namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// A single error entry describing why a request failed. Each error carries a human-readable
/// message, a stable machine-readable code, an optional target field, a category, and an
/// optional free-form metadata bag.
/// </summary>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableError
{
    /// <summary>
    /// Human-readable description of what went wrong.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Stable machine-readable identifier of this kind of error. Intended for callers that
    /// branch on error types programmatically.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// The input field, property, or resource that the error refers to, if applicable.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Classification of the error (validation, conflict, authentication, and so on). Maps to
    /// the HTTP status code that the API surfaces for the overall response.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Additional structured information about the error, for example the lower and upper
    /// boundary of a failing range check. The shape is error-specific.
    /// </summary>
    public object? Metadata { get; init; }
}

/// <summary>
/// A single error entry describing why a request failed. Each error carries a human-readable
/// message, a stable machine-readable code, an optional target field, a category, and an
/// optional structured metadata bag.
/// </summary>
/// <typeparam name="TMetadata">The shape of the per-error metadata. See <see cref="PortableError" /> for the non-generic variant.</typeparam>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableError<TMetadata>
{
    /// <summary>
    /// Human-readable description of what went wrong.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Stable machine-readable identifier of this kind of error. Intended for callers that
    /// branch on error types programmatically.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// The input field, property, or resource that the error refers to, if applicable.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Classification of the error (validation, conflict, authentication, and so on). Maps to
    /// the HTTP status code that the API surfaces for the overall response.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Additional structured information about the error, for example the lower and upper
    /// boundary of a failing range check.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
