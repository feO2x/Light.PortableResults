using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// RFC 9457 problem details response returned for validation failures in the ASP.NET
/// Core-compatible format. The inherited <c>errors</c> property is a
/// <c>Dictionary&lt;string, string[]&gt;</c> (field name to messages) for compatibility with
/// clients that expect the ASP.NET Core default; the optional <c>errorDetails</c> array
/// supplies Light.PortableResults-specific information such as codes, categories, and
/// metadata for each message.
/// </summary>
/// <typeparam name="TErrorDetailMetadata">The shape of the per-detail metadata on each <c>errorDetails</c> entry.</typeparam>
/// <typeparam name="TProblemMetadata">The shape of the top-level <c>metadata</c> bag.</typeparam>
/// <remarks>
/// Use this schema when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>AspNetCoreCompatible</c>. This is a schema-only type used by
/// Light.PortableResults for OpenAPI documentation; the wire format is produced directly by
/// the runtime HTTP writers.
/// </remarks>
public class PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>
    : HttpValidationProblemDetails
{
    /// <summary>
    /// Optional Light.PortableResults-specific details that correlate with the inherited
    /// <c>errors</c> dictionary. Each entry points back to a message in
    /// <c>errors[target]</c> via its <c>index</c> property.
    /// </summary>
    public IReadOnlyList<PortableValidationErrorDetail<TErrorDetailMetadata>>? ErrorDetails { get; init; }

    /// <summary>
    /// Optional structured information about the validation failure as a whole, separate from
    /// any individual error item.
    /// </summary>
    public TProblemMetadata Metadata { get; init; } = default!;
}

/// <summary>
/// RFC 9457 problem details response returned for validation failures when the API is
/// configured to serialize validation errors in the ASP.NET Core-compatible format. The
/// inherited <c>errors</c> property is a <c>Dictionary&lt;string, string[]&gt;</c> for
/// compatibility with clients that expect the ASP.NET Core default; the optional
/// <c>errorDetails</c> array supplies Light.PortableResults-specific information.
/// </summary>
/// <remarks>
/// Use this schema when <c>PortableResultsHttpWriteOptions.ValidationProblemSerializationFormat</c>
/// is set to <c>AspNetCoreCompatible</c>. This is a schema-only type used by
/// Light.PortableResults for OpenAPI documentation; the wire format is produced directly by
/// the runtime HTTP writers.
/// </remarks>
public class PortableAspNetCoreValidationProblemDetails
    : PortableAspNetCoreValidationProblemDetails<object, object>;
