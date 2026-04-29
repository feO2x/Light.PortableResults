using System;
using Light.PortableResults.AspNetCore.OpenApi;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Provides typed OpenAPI metadata helpers for built-in validation error codes.
/// </summary>
public static class BuiltInValidationErrorBuilderExtensions
{
    /// <summary>Documents endpoint-specific EqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithEqualToError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<EqualToMetadata<T>>(ValidationErrorCodes.EqualTo);

    /// <summary>Documents endpoint-specific EqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<EqualToMetadata<T>>(ValidationErrorCodes.EqualTo);

    /// <summary>Documents endpoint-specific NotEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithNotEqualToError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<NotEqualToMetadata<T>>(ValidationErrorCodes.NotEqualTo);

    /// <summary>Documents endpoint-specific NotEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithNotEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<NotEqualToMetadata<T>>(ValidationErrorCodes.NotEqualTo);

    /// <summary>Documents endpoint-specific GreaterThan validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithGreaterThanError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<GreaterThanMetadata<T>>(ValidationErrorCodes.GreaterThan);

    /// <summary>Documents endpoint-specific GreaterThan validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithGreaterThanError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<GreaterThanMetadata<T>>(ValidationErrorCodes.GreaterThan);

    /// <summary>Documents endpoint-specific GreaterThanOrEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithGreaterThanOrEqualToError<T>(
        this PortableProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<GreaterThanOrEqualToMetadata<T>>(
            ValidationErrorCodes.GreaterThanOrEqualTo
        );

    /// <summary>Documents endpoint-specific GreaterThanOrEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithGreaterThanOrEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<GreaterThanOrEqualToMetadata<T>>(
            ValidationErrorCodes.GreaterThanOrEqualTo
        );

    /// <summary>Documents endpoint-specific LessThan validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithLessThanError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<LessThanMetadata<T>>(ValidationErrorCodes.LessThan);

    /// <summary>Documents endpoint-specific LessThan validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithLessThanError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<LessThanMetadata<T>>(ValidationErrorCodes.LessThan);

    /// <summary>Documents endpoint-specific LessThanOrEqualTo validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithLessThanOrEqualToError<T>(
        this PortableProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<LessThanOrEqualToMetadata<T>>(
            ValidationErrorCodes.LessThanOrEqualTo
        );

    /// <summary>Documents endpoint-specific LessThanOrEqualTo validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithLessThanOrEqualToError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<LessThanOrEqualToMetadata<T>>(
            ValidationErrorCodes.LessThanOrEqualTo
        );

    /// <summary>Documents endpoint-specific InRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithInRangeError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<InRangeMetadata<T>>(ValidationErrorCodes.InRange);

    /// <summary>Documents endpoint-specific InRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithInRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<InRangeMetadata<T>>(ValidationErrorCodes.InRange);

    /// <summary>Documents endpoint-specific NotInRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithNotInRangeError<T>(this PortableProblemOpenApiBuilder builder) =>
        EnsureBuilder(builder).WithErrorMetadata<NotInRangeMetadata<T>>(ValidationErrorCodes.NotInRange);

    /// <summary>Documents endpoint-specific NotInRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithNotInRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<NotInRangeMetadata<T>>(ValidationErrorCodes.NotInRange);

    /// <summary>Documents endpoint-specific ExclusiveRange validation error metadata.</summary>
    public static PortableProblemOpenApiBuilder WithExclusiveRangeError<T>(
        this PortableProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<ExclusiveRangeMetadata<T>>(ValidationErrorCodes.ExclusiveRange);

    /// <summary>Documents endpoint-specific ExclusiveRange validation error metadata.</summary>
    public static PortableValidationProblemOpenApiBuilder WithExclusiveRangeError<T>(
        this PortableValidationProblemOpenApiBuilder builder
    ) =>
        EnsureBuilder(builder).WithErrorMetadata<ExclusiveRangeMetadata<T>>(ValidationErrorCodes.ExclusiveRange);

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
