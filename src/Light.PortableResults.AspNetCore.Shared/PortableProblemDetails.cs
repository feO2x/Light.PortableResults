using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// RFC 9457 problem details response returned for a non-validation failure (for example 401,
/// 403, 404, 409, or 500). Carries the standard problem details fields plus a list of
/// Light.PortableResults error items and optional top-level problem metadata.
/// </summary>
/// <typeparam name="TErrorMetadata">The shape of the per-error metadata on each <c>errors</c> entry.</typeparam>
/// <typeparam name="TProblemMetadata">The shape of the top-level <c>metadata</c> bag.</typeparam>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableProblemDetails<TErrorMetadata, TProblemMetadata> : ProblemDetails
{
    /// <summary>
    /// The error items that describe why the request failed. Typically contains a single entry
    /// for non-validation failures.
    /// </summary>
    public IReadOnlyList<PortableError<TErrorMetadata>> Errors { get; init; } =
        new List<PortableError<TErrorMetadata>>();

    /// <summary>
    /// Optional structured information about the failure as a whole, separate from any
    /// individual error item.
    /// </summary>
    public TProblemMetadata Metadata { get; init; } = default!;
}

/// <summary>
/// RFC 9457 problem details response returned for a non-validation failure (for example 401,
/// 403, 404, 409, or 500). Carries the standard problem details fields plus a list of
/// Light.PortableResults error items and optional top-level problem metadata.
/// </summary>
/// <remarks>
/// Schema-only type used by Light.PortableResults for OpenAPI documentation; the wire format
/// is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableProblemDetails : PortableProblemDetails<object, object>;
