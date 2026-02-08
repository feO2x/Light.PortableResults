using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Serialization;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;
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
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when JSON type metadata cannot be resolved.
    /// </exception>
    protected override async Task WriteBodyAsync(Result enrichedResult, HttpContext httpContext)
    {
        var serializerOptions = httpContext.RequestServices.ResolveJsonSerializerOptions(SerializerOptions);
        if (!serializerOptions.TryGetTypeInfo(typeof(Result), out var foundTypeInfo))
        {
            throw new InvalidOperationException(
                "There is no JsonTypeInfo for 'Result' - please check the Microsoft.AspNetCore.Http.Json.JsonOptions of your app"
            );
        }

        await using var writer = new Utf8JsonWriter(httpContext.Response.BodyWriter);

        // Prefer the strongly typed JsonTypeInfo<T> when available (source-gen / reflection).
        if (foundTypeInfo.Converter is HttpWriteResultJsonConverter defaultResultJsonConverter)
        {
            defaultResultJsonConverter.Serialize(writer, enrichedResult, serializerOptions, OverrideOptions);
            return;
        }

        if (foundTypeInfo is JsonTypeInfo<Result> castTypeInfo)
        {
            JsonSerializer.Serialize(writer, enrichedResult, castTypeInfo);
            return;
        }

        // Fallback: still works if the resolver returned a non-generic JsonTypeInfo instance.
        JsonSerializer.Serialize(writer, enrichedResult, foundTypeInfo);
    }
}

/// <summary>
/// Minimal API result for <see cref="Result{T}" /> which either writes success HTTP response bodies or Problem Details
/// bodies, based on the given result.
/// </summary>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when JSON type metadata cannot be resolved.
    /// </exception>
    protected override async Task WriteBodyAsync(Result<T> enrichedResult, HttpContext httpContext)
    {
        var serializerOptions = httpContext.RequestServices.ResolveJsonSerializerOptions(SerializerOptions);
        if (!serializerOptions.TryGetTypeInfo(typeof(Result<T>), out var foundTypeInfo))
        {
            throw new InvalidOperationException(
                $"There is no JsonTypeInfo for '{typeof(Result<T>)}' - please check the Microsoft.AspNetCore.Http.Json.JsonOptions of your app"
            );
        }

        await using var writer = new Utf8JsonWriter(httpContext.Response.BodyWriter);

        // Prefer the strongly typed JsonTypeInfo<T> when available (source-gen / reflection).
        if (foundTypeInfo.Converter is HttpWriteResultJsonConverter<T> defaultConverter)
        {
            defaultConverter.Serialize(writer, enrichedResult, serializerOptions, OverrideOptions);
            return;
        }

        if (foundTypeInfo is JsonTypeInfo<Result<T>> castTypeInfo)
        {
            JsonSerializer.Serialize(writer, enrichedResult, castTypeInfo);
            return;
        }

        // Fallback: still works if the resolver returned a non-generic JsonTypeInfo instance.
        JsonSerializer.Serialize(writer, enrichedResult, foundTypeInfo);
    }
}
