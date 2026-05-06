using System;
using System.Collections.Generic;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation.Definitions;
using Microsoft.OpenApi;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Provides typed OpenAPI metadata helpers for built-in validation error codes.
/// </summary>
public static class BuiltInValidationErrorBuilderExtensions
{
    /// <summary>Documents endpoint-specific EqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithEqualToError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.EqualTo,
            _ => CreateComparisonSchema<T>(),
            $"EqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.EqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific EqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.EqualTo,
            _ => CreateComparisonSchema<T>(),
            $"EqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.EqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific NotEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithNotEqualToError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.NotEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"NotEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.NotEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific NotEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithNotEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.NotEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"NotEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.NotEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific GreaterThan validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithGreaterThanError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.GreaterThan,
            _ => CreateComparisonSchema<T>(),
            $"GreaterThanMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.GreaterThan, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific GreaterThan validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithGreaterThanError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.GreaterThan,
            _ => CreateComparisonSchema<T>(),
            $"GreaterThanMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.GreaterThan, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific GreaterThanOrEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithGreaterThanOrEqualToError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.GreaterThanOrEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"GreaterThanOrEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.GreaterThanOrEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific GreaterThanOrEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithGreaterThanOrEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.GreaterThanOrEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"GreaterThanOrEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.GreaterThanOrEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific LessThan validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithLessThanError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.LessThan,
            _ => CreateComparisonSchema<T>(),
            $"LessThanMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.LessThan, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific LessThan validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithLessThanError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.LessThan,
            _ => CreateComparisonSchema<T>(),
            $"LessThanMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.LessThan, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific LessThanOrEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithLessThanOrEqualToError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.LessThanOrEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"LessThanOrEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.LessThanOrEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific LessThanOrEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithLessThanOrEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? comparativeValue = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.LessThanOrEqualTo,
            _ => CreateComparisonSchema<T>(),
            $"LessThanOrEqualToMetadata<{typeof(T).Name}>"
        );
        return AddComparisonExample(configuredBuilder, ValidationErrorCodes.LessThanOrEqualTo, target, comparativeValue);
    }

    /// <summary>Documents endpoint-specific InRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithInRangeError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.InRange,
            _ => CreateRangeSchema<T>(),
            $"InRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.InRange, target, lowerBoundary, upperBoundary);
    }

    /// <summary>Documents endpoint-specific InRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithInRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.InRange,
            _ => CreateRangeSchema<T>(),
            $"InRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.InRange, target, lowerBoundary, upperBoundary);
    }

    /// <summary>Documents endpoint-specific NotInRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithNotInRangeError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.NotInRange,
            _ => CreateRangeSchema<T>(),
            $"NotInRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.NotInRange, target, lowerBoundary, upperBoundary);
    }

    /// <summary>Documents endpoint-specific NotInRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithNotInRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.NotInRange,
            _ => CreateRangeSchema<T>(),
            $"NotInRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.NotInRange, target, lowerBoundary, upperBoundary);
    }

    /// <summary>Documents endpoint-specific ExclusiveRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithExclusiveRangeError<T>(
        this PortableProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.ExclusiveRange,
            _ => CreateRangeSchema<T>(),
            $"ExclusiveRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.ExclusiveRange, target, lowerBoundary, upperBoundary);
    }

    /// <summary>Documents endpoint-specific ExclusiveRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithExclusiveRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder,
        string? target = null,
        T? lowerBoundary = default,
        T? upperBoundary = default
    )
    {
        var configuredBuilder = EnsureBuilder(builder).WithErrorMetadata(
            ValidationErrorCodes.ExclusiveRange,
            _ => CreateRangeSchema<T>(),
            $"ExclusiveRangeMetadata<{typeof(T).Name}>"
        );
        return AddRangeExample(configuredBuilder, ValidationErrorCodes.ExclusiveRange, target, lowerBoundary, upperBoundary);
    }

    private static OpenApiSchema CreateComparisonSchema<T>() =>
        new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [ValidationErrorMetadataKeys.ComparativeValue] = PortableOpenApiSchemaTypeMapper.Map<T>()
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { ValidationErrorMetadataKeys.ComparativeValue }
        };

    private static OpenApiSchema CreateRangeSchema<T>() =>
        new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                [ValidationErrorMetadataKeys.LowerBoundary] = PortableOpenApiSchemaTypeMapper.Map<T>(),
                [ValidationErrorMetadataKeys.UpperBoundary] = PortableOpenApiSchemaTypeMapper.Map<T>()
            },
            Required = new HashSet<string>(StringComparer.Ordinal)
            {
                ValidationErrorMetadataKeys.LowerBoundary,
                ValidationErrorMetadataKeys.UpperBoundary
            }
        };

    private static PortableProblemOpenApiBuilder AddComparisonExample<T>(
        PortableProblemOpenApiBuilder builder,
        string code,
        string? target,
        T? comparativeValue
    ) =>
        target is null ?
            builder :
            builder.WithErrorExample(code, target, CreateComparisonMetadata(comparativeValue));

    private static PortableValidationProblemOpenApiBuilder AddComparisonExample<T>(
        PortableValidationProblemOpenApiBuilder builder,
        string code,
        string? target,
        T? comparativeValue
    ) =>
        target is null ?
            builder :
            builder.WithErrorExample(code, target, CreateComparisonMetadata(comparativeValue));

    private static PortableProblemOpenApiBuilder AddRangeExample<T>(
        PortableProblemOpenApiBuilder builder,
        string code,
        string? target,
        T? lowerBoundary,
        T? upperBoundary
    ) =>
        target is null ?
            builder :
            builder.WithErrorExample(code, target, CreateRangeMetadata(lowerBoundary, upperBoundary));

    private static PortableValidationProblemOpenApiBuilder AddRangeExample<T>(
        PortableValidationProblemOpenApiBuilder builder,
        string code,
        string? target,
        T? lowerBoundary,
        T? upperBoundary
    ) =>
        target is null ?
            builder :
            builder.WithErrorExample(code, target, CreateRangeMetadata(lowerBoundary, upperBoundary));

    private static IReadOnlyDictionary<string, object?> CreateComparisonMetadata<T>(T? comparativeValue) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [ValidationErrorMetadataKeys.ComparativeValue] = comparativeValue
        };

    private static IReadOnlyDictionary<string, object?> CreateRangeMetadata<T>(T? lowerBoundary, T? upperBoundary) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [ValidationErrorMetadataKeys.LowerBoundary] = lowerBoundary,
            [ValidationErrorMetadataKeys.UpperBoundary] = upperBoundary
        };

    private static PortableProblemOpenApiBuilder EnsureBuilder(PortableProblemOpenApiBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    private static PortableValidationProblemOpenApiBuilder EnsureBuilder(PortableValidationProblemOpenApiBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }
}
