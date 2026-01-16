using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.Shared.Enrichment;

/// <summary>
/// Service for enriching results with additional metadata before conversion to LightProblemDetails.
/// Register in DI to add traceId, correlationId, or other cross-cutting metadata.
/// </summary>
public interface IHttpResultEnricher
{
    /// <summary>
    /// Enriches a result with additional metadata before conversion to LightProblemDetails.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
    Result<T> Enrich<T>(Result<T> result, HttpContext httpContext);

    /// <summary>
    /// Enriches a void result with additional metadata before conversion to LightProblemDetails.
    /// </summary>
    /// <param name="result">The original result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A new result with enriched metadata, or the original if unchanged.</returns>
    Result Enrich(Result result, HttpContext httpContext);
}
