using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Serialization;
using Light.Results.AspNetCore.Shared;
using Light.Results.AspNetCore.Shared.Serialization;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis;

public sealed class LightResult : BaseLightResult<Result>
{
    public LightResult(
        Result result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) : base(result, successStatusCode, location, overrideOptions, serializerOptions) { }

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
        if (foundTypeInfo.Converter is DefaultResultJsonConverter defaultResultJsonConverter)
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

public sealed class LightResult<T> : BaseLightResult<Result<T>>
{
    public LightResult(
        Result<T> result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) : base(result, successStatusCode, location, overrideOptions, serializerOptions) { }

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
        if (foundTypeInfo.Converter is DefaultResultJsonConverter<T> defaultConverter)
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
