using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.Shared.Enrichment;

/// <summary>
/// Service for enriching results with additional metadata before conversion to LightProblemDetails.
/// Register in DI to add traceId, correlationId, or other cross-cutting metadata.
/// </summary>
public interface IHttpResultEnricher
{
    /// <summary>
    /// Enriches a result with additional metadata before conversion to LightProblemDetails.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
    TResult Enrich<TResult>(TResult result, HttpContext httpContext)
        where TResult : struct, IResultObject, ICanReplaceMetadata<TResult>;
}
