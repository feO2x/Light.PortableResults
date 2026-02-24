using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Serialization;
using Light.Results.Http.Writing;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Minimal API result for <see cref="Result" /> which either writes success HTTP response bodies or Problem Details
/// bodies, based on the given result.
/// </summary>
public sealed class LightResult : BaseLightResult<Result>
{
    /// <summary>
    /// Initializes a new instance of <see cref="LightResult" />.
    /// </summary>
    /// <param name="result">The result to execute.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    public LightResult(
        Result result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) : base(result, successStatusCode, location, overrideOptions, serializerOptions) { }

    /// <summary>
    /// Writes the response body for the result.
    /// </summary>
    /// <param name="enrichedResult">The enriched result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="resolvedOptions">The frozen options for this request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task WriteBodyAsync(
        Result enrichedResult,
        HttpContext httpContext,
        ResolvedHttpWriteOptions resolvedOptions
    )
    {
        var serializerOptions = httpContext.RequestServices.ResolveJsonSerializerOptions(SerializerOptions);
        var wrapper = enrichedResult.ToHttpResultForWriting(resolvedOptions);

        var typeInfo = serializerOptions.GetTypeInfo(typeof(HttpResultForWriting));
        if (typeInfo is not JsonTypeInfo<HttpResultForWriting> castTypeInfo)
        {
            throw new InvalidOperationException(
                "Could not resolve 'JsonTypeInfo<HttpResultForWriting>'. Please ensure that your JsonSerializerOptions are configured correctly. The AddDefaultLightResultsHttpWriteJsonConverters extension method can help you with this."
            );
        }

        return JsonSerializer.SerializeAsync(
            httpContext.Response.BodyWriter,
            wrapper,
            castTypeInfo,
            httpContext.RequestAborted
        );
    }
}

/// <summary>
/// Minimal API result for <see cref="Result{T}" /> which either writes success HTTP response bodies or Problem Details
/// bodies, based on the given result.
/// </summary>
/// <typeparam name="T">The type of the success value in the result.</typeparam>
public sealed class LightResult<T> : BaseLightResult<Result<T>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="LightResult{T}" />.
    /// </summary>
    /// <param name="result">The result to execute.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    public LightResult(
        Result<T> result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) : base(result, successStatusCode, location, overrideOptions, serializerOptions) { }

    /// <summary>
    /// Writes the response body for the result.
    /// </summary>
    /// <param name="enrichedResult">The enriched result.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="resolvedOptions">The frozen options for this request.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task WriteBodyAsync(
        Result<T> enrichedResult,
        HttpContext httpContext,
        ResolvedHttpWriteOptions resolvedOptions
    )
    {
        var serializerOptions = httpContext.RequestServices.ResolveJsonSerializerOptions(SerializerOptions);
        var wrapper = enrichedResult.ToHttpResultForWriting(resolvedOptions);

        var typeInfo = serializerOptions.GetTypeInfo(typeof(HttpResultForWriting<T>));
        if (typeInfo is not JsonTypeInfo<HttpResultForWriting<T>> castTypeInfo)
        {
            throw new InvalidOperationException(
                $"Could not resolve 'JsonTypeInfo<HttpResultForWriting<{nameof(T)}>>'. Please ensure that your JsonSerializerOptions are configured correctly. The AddDefaultLightResultsHttpWriteJsonConverters extension method can help you with this."
            );
        }

        await JsonSerializer.SerializeAsync(
            httpContext.Response.BodyWriter,
            wrapper,
            castTypeInfo,
            httpContext.RequestAborted
        );
    }
}
