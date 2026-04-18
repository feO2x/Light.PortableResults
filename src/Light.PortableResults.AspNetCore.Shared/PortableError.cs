namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a single Light.PortableResults error item as it appears in rich
/// problem details responses with untyped metadata.
/// </summary>
public class PortableError
{
    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable machine-readable error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or sets the target (e.g. the offending input field) of the error.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Gets or sets the error category.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Gets or sets the optional metadata associated with the error.
    /// </summary>
    public object? Metadata { get; init; }
}

/// <summary>
/// Schema-only type for OpenAPI documentation. Not used at runtime.
/// Represents a single Light.PortableResults error item with strongly typed metadata.
/// </summary>
/// <typeparam name="TMetadata">The type of the per-error metadata.</typeparam>
public class PortableError<TMetadata>
{
    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the stable machine-readable error code.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Gets or sets the target (e.g. the offending input field) of the error.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Gets or sets the error category.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Gets or sets the metadata associated with the error.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
