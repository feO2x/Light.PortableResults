using System.ComponentModel.DataAnnotations;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>Metadata contract for endpoint-specific EqualTo validation errors.</summary>
/// <param name="ComparativeValue">The expected value.</param>
public sealed record EqualToMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific NotEqualTo validation errors.</summary>
/// <param name="ComparativeValue">The disallowed value.</param>
public sealed record NotEqualToMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific GreaterThan validation errors.</summary>
/// <param name="ComparativeValue">The lower exclusive boundary.</param>
public sealed record GreaterThanMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific GreaterThanOrEqualTo validation errors.</summary>
/// <param name="ComparativeValue">The lower inclusive boundary.</param>
public sealed record GreaterThanOrEqualToMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific LessThan validation errors.</summary>
/// <param name="ComparativeValue">The upper exclusive boundary.</param>
public sealed record LessThanMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific LessThanOrEqualTo validation errors.</summary>
/// <param name="ComparativeValue">The upper inclusive boundary.</param>
public sealed record LessThanOrEqualToMetadata<T>([property: Required] T ComparativeValue);

/// <summary>Metadata contract for endpoint-specific InRange validation errors.</summary>
/// <param name="LowerBoundary">The lower boundary.</param>
/// <param name="UpperBoundary">The upper boundary.</param>
public sealed record InRangeMetadata<T>(
    [property: Required] T LowerBoundary,
    [property: Required] T UpperBoundary
);

/// <summary>Metadata contract for endpoint-specific NotInRange validation errors.</summary>
/// <param name="LowerBoundary">The lower boundary.</param>
/// <param name="UpperBoundary">The upper boundary.</param>
public sealed record NotInRangeMetadata<T>(
    [property: Required] T LowerBoundary,
    [property: Required] T UpperBoundary
);

/// <summary>Metadata contract for endpoint-specific ExclusiveRange validation errors.</summary>
/// <param name="LowerBoundary">The lower exclusive boundary.</param>
/// <param name="UpperBoundary">The upper exclusive boundary.</param>
public sealed record ExclusiveRangeMetadata<T>(
    [property: Required] T LowerBoundary,
    [property: Required] T UpperBoundary
);
