using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.AspNetCore.Shared.Enrichment;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Light.PortableResults.AspNetCore.Mvc;

/// <summary>
/// Base type for MVC action results that wrap Light.PortableResults values.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
public abstract class BaseLightActionResult<TResult> : IActionResult
    where TResult : struct, IResultObject, ICanReplaceMetadata<TResult>
{
    /// <summary>
    /// Initializes a new instance of <see cref="BaseLightActionResult{TResult}" />.
    /// </summary>
    /// <param name="result">The result to execute.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.PortableResults options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    protected BaseLightActionResult(
        TResult result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        PortableResultsHttpWriteOptions? overrideOptions = null,
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
    /// Gets the optional Light.PortableResults options override.
    /// </summary>
    public PortableResultsHttpWriteOptions? OverrideOptions { get; }

    /// <summary>
    /// Executes the result against the supplied action context.
    /// </summary>
    /// <param name="context">The action context for the current request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services or options are not available.
    /// </exception>
    public virtual Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        var result = ReceivedResult;
        var enricher = httpContext.RequestServices.GetService<IHttpResultEnricher>();
        if (enricher is not null)
        {
            result = enricher.Enrich(result, httpContext);
        }

        var options = httpContext.ResolvePortableResultsHttpWriteOptions(OverrideOptions);
        var resolvedOptions = options.ToResolvedHttpWriteOptions();
        SetHeaders(result, httpContext, resolvedOptions);
        return WriteBodyAsync(result, httpContext, resolvedOptions);
    }

    /// <summary>
    /// Sets response headers based on the enriched result.
    /// </summary>
    /// <param name="enrichedResult">The enriched result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="resolvedOptions">The frozen options for this request.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services or options are not available.
    /// </exception>
    protected virtual void SetHeaders(
        TResult enrichedResult,
        HttpContext httpContext,
        ResolvedHttpWriteOptions resolvedOptions
    )
    {
        var conversionService = httpContext.RequestServices.GetRequiredService<IHttpHeaderConversionService>();
        httpContext.Response.SetStatusCodeFromResult(
            enrichedResult,
            SuccessStatusCode,
            resolvedOptions.FirstErrorCategoryIsLeadingCategory
        );
        httpContext.Response.SetContentTypeFromResult(enrichedResult, resolvedOptions.MetadataSerializationMode);
        httpContext.Response.SetMetadataValuesAsHeadersIfNecessary(enrichedResult, conversionService);
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
    /// <param name="resolvedOptions">The frozen options for this request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task WriteBodyAsync(
        TResult enrichedResult,
        HttpContext httpContext,
        ResolvedHttpWriteOptions resolvedOptions
    );
}
