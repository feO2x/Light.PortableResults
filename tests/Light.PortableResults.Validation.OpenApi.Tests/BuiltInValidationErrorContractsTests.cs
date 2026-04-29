using System;
using System.Collections.Frozen;
using System.Linq;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Validation.Definitions;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class BuiltInValidationErrorContractsTests
{
    private static readonly string[] MetadataBearingCodes =
    [
        ValidationErrorCodes.Count,
        ValidationErrorCodes.MinCount,
        ValidationErrorCodes.MaxCount,
        ValidationErrorCodes.MinLength,
        ValidationErrorCodes.MaxLength,
        ValidationErrorCodes.LengthInRange,
        ValidationErrorCodes.EqualTo,
        ValidationErrorCodes.NotEqualTo,
        ValidationErrorCodes.GreaterThan,
        ValidationErrorCodes.GreaterThanOrEqualTo,
        ValidationErrorCodes.LessThan,
        ValidationErrorCodes.LessThanOrEqualTo,
        ValidationErrorCodes.InRange,
        ValidationErrorCodes.NotInRange,
        ValidationErrorCodes.ExclusiveRange,
        ValidationErrorCodes.Pattern,
        ValidationErrorCodes.Enum,
        ValidationErrorCodes.EnumName,
        ValidationErrorCodes.PrecisionScale
    ];

    public static TheoryData<string, string[]> MetadataCodeProperties =>
        new ()
        {
            { ValidationErrorCodes.Count, [ValidationErrorMetadataKeys.ExpectedCount] },
            { ValidationErrorCodes.MinCount, [ValidationErrorMetadataKeys.MinCount] },
            { ValidationErrorCodes.MaxCount, [ValidationErrorMetadataKeys.MaxCount] },
            { ValidationErrorCodes.MinLength, [ValidationErrorMetadataKeys.MinLength] },
            { ValidationErrorCodes.MaxLength, [ValidationErrorMetadataKeys.MaxLength] },
            {
                ValidationErrorCodes.LengthInRange,
                [ValidationErrorMetadataKeys.MinLength, ValidationErrorMetadataKeys.MaxLength]
            },
            { ValidationErrorCodes.EqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { ValidationErrorCodes.NotEqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { ValidationErrorCodes.GreaterThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            { ValidationErrorCodes.GreaterThanOrEqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { ValidationErrorCodes.LessThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            { ValidationErrorCodes.LessThanOrEqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                ValidationErrorCodes.InRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                ValidationErrorCodes.NotInRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                ValidationErrorCodes.ExclusiveRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                ValidationErrorCodes.Pattern,
                [ValidationErrorMetadataKeys.Pattern, ValidationErrorMetadataKeys.RegexOptions]
            },
            { ValidationErrorCodes.Enum, [ValidationErrorMetadataKeys.EnumType] },
            {
                ValidationErrorCodes.EnumName,
                [ValidationErrorMetadataKeys.EnumType, ValidationErrorMetadataKeys.IgnoreCase]
            },
            {
                ValidationErrorCodes.PrecisionScale,
                [
                    ValidationErrorMetadataKeys.ExpectedPrecision,
                    ValidationErrorMetadataKeys.ExpectedScale,
                    ValidationErrorMetadataKeys.IgnoreTrailingZeros
                ]
            }
        };

    public static TheoryData<string> PrimitiveValueCodes =>
    [
        ValidationErrorCodes.EqualTo,
        ValidationErrorCodes.NotEqualTo,
        ValidationErrorCodes.GreaterThan,
        ValidationErrorCodes.GreaterThanOrEqualTo,
        ValidationErrorCodes.LessThan,
        ValidationErrorCodes.LessThanOrEqualTo,
        ValidationErrorCodes.InRange,
        ValidationErrorCodes.NotInRange,
        ValidationErrorCodes.ExclusiveRange
    ];

    [Fact]
    public void Contracts_ShouldContainExpectedBuiltInCodes()
    {
        string[] expectedNoMetadataCodes =
        [
            ValidationErrorCodes.NotNull,
            ValidationErrorCodes.Null,
            ValidationErrorCodes.NotEmpty,
            ValidationErrorCodes.Empty,
            ValidationErrorCodes.NotNullOrWhiteSpace,
            ValidationErrorCodes.Email,
            ValidationErrorCodes.DigitsOnly,
            ValidationErrorCodes.LettersAndDigitsOnly
        ];

        BuiltInValidationErrorContracts.Contracts.Keys.Should()
           .BeEquivalentTo(MetadataBearingCodes.Concat(expectedNoMetadataCodes));
        BuiltInValidationErrorContracts.Contracts.Should().NotContainKey(ValidationErrorCodes.Predicate);
        foreach (var code in expectedNoMetadataCodes)
        {
            BuiltInValidationErrorContracts.Contracts[code].Should().BeSameAs(PortableErrorMetadataContract.NoMetadata);
        }
    }

    [Fact]
    public void Contracts_ShouldBeBackedByFrozenDictionary() =>
        BuiltInValidationErrorContracts.Contracts.Should()
           .BeAssignableTo<FrozenDictionary<string, PortableErrorMetadataContract>>();

    [Theory]
    [MemberData(nameof(MetadataCodeProperties))]
    public void MetadataContracts_ShouldEmitExpectedObjectProperties(string code, string[] expectedProperties)
    {
        var contract = BuiltInValidationErrorContracts.Contracts[code]
           .Should()
           .BeOfType<PortableErrorMetadataSchemaContract>()
           .Subject;

        var firstSchema = contract.SchemaFactory(OpenApiSpecVersion.OpenApi3_1);
        var secondSchema = contract.SchemaFactory(OpenApiSpecVersion.OpenApi3_1);

        firstSchema.Should().NotBeSameAs(secondSchema);
        firstSchema.Type.Should().Be(JsonSchemaType.Object);
        firstSchema.Properties!.Keys.Should().BeEquivalentTo(expectedProperties);
        firstSchema.Required.Should().BeEquivalentTo(expectedProperties);
    }

    [Theory]
    [MemberData(nameof(PrimitiveValueCodes))]
    public void PrimitiveValueSchemas_ShouldUseOpenApi31NullBranch(string code)
    {
        var primitiveSchema = GetFirstPrimitiveValueSchema(code, OpenApiSpecVersion.OpenApi3_1);

        primitiveSchema.Type.Should().BeNull();
        primitiveSchema.OneOf.Should().NotBeNull();
        primitiveSchema.OneOf!.Select(static schema => ((OpenApiSchema) schema).Type).Should()
           .BeEquivalentTo(
                new JsonSchemaType?[]
                {
                    JsonSchemaType.String,
                    JsonSchemaType.Number,
                    JsonSchemaType.Integer,
                    JsonSchemaType.Boolean,
                    JsonSchemaType.Null
                }
            );
    }

    [Theory]
    [MemberData(nameof(PrimitiveValueCodes))]
    public void PrimitiveValueSchemas_ShouldUseOpenApi30NullableParentWithoutNullBranch(string code)
    {
        var primitiveSchema = GetFirstPrimitiveValueSchema(code, OpenApiSpecVersion.OpenApi3_0);

        primitiveSchema.Type.Should().Be(JsonSchemaType.Null);
        primitiveSchema.OneOf.Should().NotBeNull();
        primitiveSchema.OneOf!.Select(static schema => ((OpenApiSchema) schema).Type).Should()
           .BeEquivalentTo(
                new JsonSchemaType?[]
                {
                    JsonSchemaType.String,
                    JsonSchemaType.Number,
                    JsonSchemaType.Integer,
                    JsonSchemaType.Boolean
                }
            );
    }

    [Fact]
    public void RegisterBuiltInValidationErrors_ShouldRegisterExpectedCodes()
    {
        var builder = new PortableErrorMetadataContractsBuilder();

        builder.RegisterBuiltInValidationErrors();
        var registry = new PortableErrorMetadataContractRegistry(builder);

        registry.Contracts.Keys.Should().BeEquivalentTo(BuiltInValidationErrorContracts.Contracts.Keys);
        registry.Contracts.Should().NotContainKey(ValidationErrorCodes.Predicate);
    }

    [Fact]
    public void RegisterBuiltInValidationErrors_ShouldBeIdempotent()
    {
        var builder = new PortableErrorMetadataContractsBuilder();

        builder.RegisterBuiltInValidationErrors();
        builder.RegisterBuiltInValidationErrors();
        var registry = new PortableErrorMetadataContractRegistry(builder);

        registry.Contracts.Keys.Should().BeEquivalentTo(BuiltInValidationErrorContracts.Contracts.Keys);
    }

    [Fact]
    public void RegisterBuiltInValidationErrors_ShouldRejectConflictingPreRegisteredContracts()
    {
        var builder = new PortableErrorMetadataContractsBuilder()
           .ForCode<ConflictingMetadata>(ValidationErrorCodes.Count);

        var act = builder.RegisterBuiltInValidationErrors;

        act.Should().Throw<InvalidOperationException>().WithMessage("*Count*ConflictingMetadata*");
    }

    private static OpenApiSchema GetFirstPrimitiveValueSchema(string code, OpenApiSpecVersion version)
    {
        var contract = (PortableErrorMetadataSchemaContract) BuiltInValidationErrorContracts.Contracts[code];
        var schema = contract.SchemaFactory(version);
        var propertyName = schema.Properties!.ContainsKey(ValidationErrorMetadataKeys.ComparativeValue) ?
            ValidationErrorMetadataKeys.ComparativeValue :
            ValidationErrorMetadataKeys.LowerBoundary;
        return (OpenApiSchema) schema.Properties[propertyName];
    }

    // ReSharper disable once ClassNeverInstantiated.Local -- required for testing
    private sealed class ConflictingMetadata
    {
        public int Value { get; init; }
    }
}
