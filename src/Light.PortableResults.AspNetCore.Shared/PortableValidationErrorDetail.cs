namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// A Light.PortableResults-specific supplement for a single validation error inside an
/// ASP.NET Core-compatible problem details response. Correlates to a message in the standard
/// <c>errors</c> dictionary and adds machine-readable information such as an error code,
/// category, and metadata bag.
/// </summary>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableValidationErrorDetail
{
    /// <summary>
    /// The input field, property, or resource this error detail refers to. Matches the key
    /// in the inherited <c>errors</c> dictionary of the enclosing problem details response.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Zero-based position of the corresponding error message within <c>errors[target]</c>
    /// for the same target, so <c>errorDetails</c> entries can be correlated back to the
    /// original message.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Stable machine-readable identifier of this kind of error. Intended for callers that
    /// branch on error types programmatically.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Optional classification of the error (validation, conflict, authentication, and so on).
    /// </summary>
    public ErrorCategory? Category { get; init; }

    /// <summary>
    /// Additional structured information about the error, for example the lower and upper
    /// boundary of a failing range check.
    /// </summary>
    public object? Metadata { get; init; }
}

/// <summary>
/// A Light.PortableResults-specific supplement for a single validation error inside an
/// ASP.NET Core-compatible problem details response. Correlates to a message in the standard
/// <c>errors</c> dictionary and adds machine-readable information such as an error code,
/// category, and metadata bag.
/// </summary>
/// <typeparam name="TMetadata">The shape of the per-error-detail metadata. See <see cref="PortableValidationErrorDetail" /> for the non-generic variant.</typeparam>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableValidationErrorDetail<TMetadata>
{
    /// <summary>
    /// The input field, property, or resource this error detail refers to. Matches the key
    /// in the inherited <c>errors</c> dictionary of the enclosing problem details response.
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Zero-based position of the corresponding error message within <c>errors[target]</c>
    /// for the same target, so <c>errorDetails</c> entries can be correlated back to the
    /// original message.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Stable machine-readable identifier of this kind of error. Intended for callers that
    /// branch on error types programmatically.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Optional classification of the error (validation, conflict, authentication, and so on).
    /// </summary>
    public ErrorCategory? Category { get; init; }

    /// <summary>
    /// Additional structured information about the error, for example the lower and upper
    /// boundary of a failing range check.
    /// </summary>
    public TMetadata Metadata { get; init; } = default!;
}
