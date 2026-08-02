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
    public static FrozenDictionary<string, ErrorMetadataContract> Contracts { get; } = CreateContracts();

    private static FrozenDictionary<string, ErrorMetadataContract> CreateContracts()
    {
        return new Dictionary<string, ErrorMetadataContract>(StringComparer.Ordinal)
        {
            [ValidationErrorCodes.Count] = Schema(
                ValidationErrorCodes.Count,
                ObjectWithInteger(ValidationErrorMetadataKeys.ExpectedCount)
            ),
            [ValidationErrorCodes.MinCount] = Schema(
                ValidationErrorCodes.MinCount,
                ObjectWithInteger(ValidationErrorMetadataKeys.MinCount)
            ),
            [ValidationErrorCodes.MaxCount] = Schema(
                ValidationErrorCodes.MaxCount,
                ObjectWithInteger(ValidationErrorMetadataKeys.MaxCount)
            ),
            [ValidationErrorCodes.MinLength] = Schema(
                ValidationErrorCodes.MinLength,
                ObjectWithInteger(ValidationErrorMetadataKeys.MinLength)
            ),
            [ValidationErrorCodes.MaxLength] = Schema(
                ValidationErrorCodes.MaxLength,
                ObjectWithInteger(ValidationErrorMetadataKeys.MaxLength)
            ),
            [ValidationErrorCodes.LengthInRange] = Schema(
                ValidationErrorCodes.LengthInRange,
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.MinLength] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.MaxLength] = IntegerSchema()
                    }
                )
            ),
            [ValidationErrorCodes.EqualTo] = Schema(
                ValidationErrorCodes.EqualTo,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.NotEqualTo] = Schema(
                ValidationErrorCodes.NotEqualTo,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.GreaterThan] = Schema(
                ValidationErrorCodes.GreaterThan,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.GreaterThanOrEqualTo] = Schema(
                ValidationErrorCodes.GreaterThanOrEqualTo,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.LessThan] = Schema(
                ValidationErrorCodes.LessThan,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.LessThanOrEqualTo] = Schema(
                ValidationErrorCodes.LessThanOrEqualTo,
                ObjectWithPrimitiveValue(ValidationErrorMetadataKeys.ComparativeValue)
            ),
            [ValidationErrorCodes.InRange] = Schema(ValidationErrorCodes.InRange, ObjectWithPrimitiveRange()),
            [ValidationErrorCodes.NotInRange] = Schema(
                ValidationErrorCodes.NotInRange,
                ObjectWithPrimitiveRange()
            ),
            [ValidationErrorCodes.ExclusiveRange] = Schema(
                ValidationErrorCodes.ExclusiveRange,
                ObjectWithPrimitiveRange()
            ),
            [ValidationErrorCodes.Pattern] = Schema(
                ValidationErrorCodes.Pattern,
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.Pattern] = StringSchema(),
                        [ValidationErrorMetadataKeys.RegexOptions] = IntegerSchema()
                    }
                )
            ),
            [ValidationErrorCodes.Enum] = Schema(
                ValidationErrorCodes.Enum,
                ObjectWithString(ValidationErrorMetadataKeys.EnumType)
            ),
            [ValidationErrorCodes.EnumName] = Schema(
                ValidationErrorCodes.EnumName,
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.EnumType] = StringSchema(),
                        [ValidationErrorMetadataKeys.IgnoreCase] = BooleanSchema()
                    }
                )
            ),
            [ValidationErrorCodes.PrecisionScale] = Schema(
                ValidationErrorCodes.PrecisionScale,
                _ => CreateObjectSchema(
                    new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        [ValidationErrorMetadataKeys.ExpectedPrecision] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.ExpectedScale] = IntegerSchema(),
                        [ValidationErrorMetadataKeys.IgnoreTrailingZeros] = BooleanSchema()
                    }
                )
            ),
            [ValidationErrorCodes.NotNull] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Null] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.NotEmpty] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Empty] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.NotNullOrWhiteSpace] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Email] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.DigitsOnly] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.LettersAndDigitsOnly] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.UuidV7] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Utc] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Local] = ErrorMetadataContract.NoMetadata,
            [ValidationErrorCodes.Unspecified] = ErrorMetadataContract.NoMetadata
        }.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static ErrorMetadataContract Schema(
        string code,
        Func<OpenApiSpecVersion, OpenApiSchema> schemaFactory
    ) => ErrorMetadataContract.FromSchema(schemaFactory, "built-in validation schema for " + code);

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
