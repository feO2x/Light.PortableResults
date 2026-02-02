using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Light.Results.AspNetCore.Shared;
using Light.Results.AspNetCore.Shared.Enrichment;
using Light.Results.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Results.AspNetCore.MinimalApis;

public abstract class BaseLightResult<TResult> : IResult
    where TResult : struct, IResultObject
{
    protected BaseLightResult(
        TResult result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    )
    {
        ReceivedResult = result;
        SuccessStatusCode = successStatusCode;
        Location = location;
        OverrideOptions = overrideOptions;
        SerializerOptions = serializerOptions;
    }

    public TResult ReceivedResult { get; }
    public HttpStatusCode? SuccessStatusCode { get; }
    public string? Location { get; }
    public JsonSerializerOptions? SerializerOptions { get; }
    public LightResultOptions? OverrideOptions { get; }

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

    protected abstract Task WriteBodyAsync(TResult enrichedResult, HttpContext httpContext);
}
