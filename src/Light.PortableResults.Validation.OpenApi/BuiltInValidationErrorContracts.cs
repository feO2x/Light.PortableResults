using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Validation.Definitions;
using Microsoft.OpenApi;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Provides OpenAPI metadata contracts for built-in validation error codes.
/// </summary>
public static class BuiltInValidationErrorContracts
{
    /// <summary>
    /// Gets the built-in validation error metadata contracts.
    /// </summary>
    public static FrozenDictionary<string, PortableErrorMetadataContract> Contracts { get; } = CreateContracts();

    private static FrozenDictionary<string, PortableErrorMetadataContract> CreateContracts()
    {
        return new Dictionary<string, PortableErrorMetadataContract>(StringComparer.Ordinal)
        {
            [ValidationErrorCodes.Count] = Schema(ObjectWithInteger(ValidationErrorMetadataKeys.ExpectedCount)),
            [ValidationErrorCodes.MinCount] = Schema(ObjectWithInteger(ValidationErrorMetadataKeys.MinCount)),
            [ValidationErrorCodes.MaxCount] = Schema(ObjectWithInteger(ValidationErrorMetadataKeys.MaxCount)),
            [ValidationErrorCodes.MinLength] = Schema(ObjectWithInteger(ValidationErrorMetadataKeys.MinLength)),
            [ValidationErrorCodes.MaxLength] = Schema(ObjectWithInteger(ValidationErrorMetadataKeys.MaxLength)),
            [ValidationErrorCodes.LengthInRange] = Schema(
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.MinLength] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.MaxLength] = IntegerSchema()
                    }
                )
            ),
            [ValidationErrorCodes.EqualTo] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.NotEqualTo] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.GreaterThan] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.GreaterThanOrEqualTo] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.LessThan] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.LessThanOrEqualTo] =
                Schema(ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)),
            [ValidationErrorCodes.InRange] = Schema(ObjectWithPrimitiveRange()),
            [ValidationErrorCodes.NotInRange] = Schema(ObjectWithPrimitiveRange()),
            [ValidationErrorCodes.ExclusiveRange] = Schema(ObjectWithPrimitiveRange()),
            [ValidationErrorCodes.Pattern] = Schema(
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.Pattern] = StringSchema(),
                        [ValidationErrorMetadataKeys.RegexOptions] = IntegerSchema()
                    }
                )
            ),
            [ValidationErrorCodes.Enum] = Schema(ObjectWithString(ValidationErrorMetadataKeys.EnumType)),
            [ValidationErrorCodes.EnumName] = Schema(
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.EnumType] = StringSchema(),
                        [ValidationErrorMetadataKeys.IgnoreCase] = BooleanSchema()
                    }
                )
            ),
            [ValidationErrorCodes.PrecisionScale] = Schema(
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.ExpectedPrecision] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.ExpectedScale] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.IgnoreTrailingZeros] = BooleanSchema()
                    }
                )
            ),
            [ValidationErrorCodes.NotNull] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Null] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.NotEmpty] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Empty] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.NotNullOrWhiteSpace] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Email] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.DigitsOnly] = PortableErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.LettersAndDigitsOnly] = PortableErrorMetadataContract.NoMetadata
        }.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static PortableErrorMetadataContract Schema(Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory) =>
        PortableErrorMetadataContract.FromSchema(schemaFactory);

    private static Func<OpenApiSpecVersion, OpenApiSchema> ObjectWithInteger(string propertyName) =>
        _ => CreateObjectSchema(
            new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [propertyName] = IntegerSchema()
            }
        );

    private static Func<OpenApiSpecVersion, OpenApiSchema> ObjectWithString(string propertyName) =>
        _ => CreateObjectSchema(
            new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [propertyName] = StringSchema()
            }
        );

    private static Func<OpenApiSpecVersion, OpenApiSchema> ObjectWithPrimitiveValue(string propertyName) =>
        version => CreateObjectSchema(
            new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [propertyName] = PrimitiveValueSchema(version)
            }
        );

    private static Func<OpenApiSpecVersion, OpenApiSchema> ObjectWithPrimitiveRange() =>
        version => CreateObjectSchema(
            new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [ValidationErrorMetadataKeys.LowerBoundary] = PrimitiveValueSchema(version),
                [ValidationErrorMetadataKeys.UpperBoundary] = PrimitiveValueSchema(version)
            }
        );

    private static OpenApiSchema CreateObjectSchema(Dictionary<string, IOpenApiSchema> properties)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = properties,
            Required = new HashSet<string>(properties.Keys, StringComparer.Ordinal)
        };
    }

    private static OpenApiSchema PrimitiveValueSchema(OpenApiSpecVersion version)
    {
        var oneOf = new List<IOpenApiSchema>
        {
            StringSchema(),
            NumberSchema(),
            IntegerSchema(),
            BooleanSchema()
        };

        if (version >= OpenApiSpecVersion.OpenApi3_1)
        {
            oneOf.Add(NullSchema());
            return new OpenApiSchema { OneOf = oneOf };
        }

        return new OpenApiSchema
        {
            Type = JsonSchemaType.Null,
            OneOf = oneOf
        };
    }

    private static OpenApiSchema StringSchema() => new () { Type = JsonSchemaType.String };

    private static OpenApiSchema NumberSchema() => new () { Type = JsonSchemaType.Number };

    private static OpenApiSchema IntegerSchema() => new () { Type = JsonSchemaType.Integer };

    private static OpenApiSchema BooleanSchema() => new () { Type = JsonSchemaType.Boolean };

    private static OpenApiSchema NullSchema() => new () { Type = JsonSchemaType.Null };
}
