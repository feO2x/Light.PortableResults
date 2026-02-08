using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Light.Results.AspNetCore.Shared;
using Light.Results.AspNetCore.Shared.Enrichment;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Headers;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Base type for Minimal API results that wrap Light.Results values.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public abstract class BaseLightResult<TResult> : IResult
    where TResult : struct, IResultObject, ICanReplaceMetadata<TResult>
{
    /// <summary>
    /// Initializes a new instance of <see cref="BaseLightResult{TResult}" />.
    /// </summary>
    /// <param name="result">The result to execute.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    protected BaseLightResult(
        TResult result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    )
    {
        ReceivedResult = result;
        SuccessStatusCode = successStatusCode;
        Location = location;
        OverrideOptions = overrideOptions;
        SerializerOptions = serializerOptions;
    }

    /// <summary>
    /// Gets the received result instance.
    /// </summary>
    public TResult ReceivedResult { get; }

    /// <summary>
    /// Gets the optional success status code override.
    /// </summary>
    public HttpStatusCode? SuccessStatusCode { get; }

    /// <summary>
    /// Gets the optional Location header value.
    /// </summary>
    public string? Location { get; }

    /// <summary>
    /// Gets the optional JSON serializer options override.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; }

    /// <summary>
    /// Gets the optional Light.Results options override.
    /// </summary>
    public LightResultsHttpWriteOptions? OverrideOptions { get; }

    /// <summary>
    /// Executes the result against the supplied HTTP context.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services or options are not available.
    /// </exception>
    public virtual Task ExecuteAsync(HttpContext httpContext)
    {
        var result = ReceivedResult;
        var enricher = httpContext.RequestServices.GetService<IHttpResultEnricher>();
        if (enricher is not null)
        {
            result = enricher.Enrich(result, httpContext);
        }

        SetHeaders(result, httpContext);
        return WriteBodyAsync(result, httpContext);
    }

    /// <summary>
    /// Sets response headers based on the enriched result.
    /// </summary>
    /// <param name="enrichedResult">The enriched result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services or options are not available.
    /// </exception>
    protected virtual void SetHeaders(TResult enrichedResult, HttpContext httpContext)
    {
        var options = httpContext.ResolveLightResultOptions(OverrideOptions);
        var conversionService = httpContext.RequestServices.GetRequiredService<IHttpHeaderConversionService>();
        httpContext.Response.SetStatusCodeFromResult(
            enrichedResult,
            SuccessStatusCode,
            options.FirstErrorCategoryIsLeadingCategory
        );
        httpContext.Response.SetContentTypeFromResult(enrichedResult, options.MetadataSerializationMode);
        httpContext.Response.SetMetadataValuesAsHeadersIfNecessary(enrichedResult, options, conversionService);
        if (!string.IsNullOrWhiteSpace(Location))
        {
            httpContext.Response.Headers.Location = Location;
        }
    }

    /// <summary>
    /// Writes the response body for the enriched result.
    /// </summary>
    /// <param name="enrichedResult">The enriched result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task WriteBodyAsync(TResult enrichedResult, HttpContext httpContext);
}
