using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// RFC 9457 problem details response returned for validation failures when the API is
/// configured to serialize validation errors in the rich Light.PortableResults format. The
/// <c>errors</c> property is a structured array of error items rather than the ASP.NET Core
/// default <c>Dictionary&lt;string, string[]&gt;</c>.
/// </summary>
/// <typeparam name="TErrorMetadata">The shape of the per-error metadata on each <c>errors</c> entry.</typeparam>
/// <typeparam name="TProblemMetadata">The shape of the top-level <c>metadata</c> bag.</typeparam>
/// <remarks>
/// Use this schema when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>Rich</c>. This is a schema-only type used by Light.PortableResults for OpenAPI
/// documentation; the wire format is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata> : ProblemDetails
{
    /// <summary>
    /// The validation errors that caused the request to be rejected.
    /// </summary>
    public IReadOnlyList<PortableError<TErrorMetadata>> Errors { get; init; } =
        new List<PortableError<TErrorMetadata>>();

    /// <summary>
    /// Optional structured information about the validation failure as a whole, separate from
    /// any individual error item.
    /// </summary>
    public TProblemMetadata Metadata { get; init; } = default!;
}

/// <summary>
/// RFC 9457 problem details response returned for validation failures when the API is
/// configured to serialize validation errors in the rich Light.PortableResults format. The
/// <c>errors</c> property is a structured array of error items rather than the ASP.NET Core
/// default <c>Dictionary&lt;string, string[]&gt;</c>.
/// </summary>
/// <remarks>
/// Use this schema when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>Rich</c>. This is a schema-only type used by Light.PortableResults for OpenAPI
/// documentation; the wire format is produced directly by the runtime HTTP writers.
/// </remarks>
public class PortableRichValidationProblemDetails : PortableRichValidationProblemDetails<object, object>;
