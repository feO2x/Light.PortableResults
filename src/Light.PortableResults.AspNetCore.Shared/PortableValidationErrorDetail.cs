namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a single entry in the <c>errorDetails</c> array of an
/// ASP.NET Core-compatible validation problem details response, with untyped metadata.
/// </summary>
public class PortableValidationErrorDetail
{
    /// <summary>
    /// Gets or sets the target (e.g. the offending input field) the error detail refers to.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based position of the corresponding error message within
    /// the <c>errors[target]</c> array for the same target.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets or sets the stable machine-readable error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or sets the optional error category.
    /// </summary>
    public ErrorCategory? Category { get; init; }

    /// <summary>
    /// Gets or sets the optional metadata associated with the error detail.
    /// </summary>
    public object? Metadata { get; init; }
}

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a single entry in the <c>errorDetails</c> array of an
/// ASP.NET Core-compatible validation problem details response, with strongly typed metadata.
/// </summary>
/// <typeparam name="TMetadata">The type of the per-error-detail metadata.</typeparam>
public class PortableValidationErrorDetail<TMetadata>
{
    /// <summary>
    /// Gets or sets the target (e.g. the offending input field) the error detail refers to.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the zero-based position of the corresponding error message within
    /// the <c>errors[target]</c> array for the same target.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets or sets the stable machine-readable error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or sets the optional error category.
    /// </summary>
    public ErrorCategory? Category { get; init; }

    /// <summary>
    /// Gets or sets the metadata associated with the error detail.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
