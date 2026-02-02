using System.Net;
using System.Text.Json;
using Light.Results.AspNetCore.Shared;

namespace Light.Results.AspNetCore.MinimalApis;

public static class MinimalApiResultExtensions
{
    public static LightResult ToMinimalApiResult(
        this Result result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    public static LightResult<T> ToMinimalApiResult<T>(
        this Result<T> result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    public static LightResult ToHttp201CreatedMinimalApiResult(
        this Result result,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);

    public static LightResult<T> ToHttp201CreatedMinimalApiResult<T>(
        this Result<T> result,
        string? location = null,
        LightResultOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);
}
