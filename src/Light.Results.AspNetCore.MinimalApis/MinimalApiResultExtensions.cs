using System.Net;
using System.Text.Json;
using Light.Results.Http.Writing;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Extension methods for converting Light.Results to Minimal API results.
/// </summary>
public static class MinimalApiResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result" /> to a <see cref="LightResult" />.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The minimal API result.</returns>
    public static LightResult ToMinimalApiResult(
        this Result result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result{T}" /> to a <see cref="LightResult{T}" />.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The minimal API result.</returns>
    public static LightResult<T> ToMinimalApiResult<T>(
        this Result<T> result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result" /> to a <see cref="LightResult" /> using HTTP 201 Created.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The minimal API result.</returns>
    public static LightResult ToHttp201CreatedMinimalApiResult(
        this Result result,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result{T}" /> to a <see cref="LightResult{T}" /> using HTTP 201 Created.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The minimal API result.</returns>
    public static LightResult<T> ToHttp201CreatedMinimalApiResult<T>(
        this Result<T> result,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);
}
