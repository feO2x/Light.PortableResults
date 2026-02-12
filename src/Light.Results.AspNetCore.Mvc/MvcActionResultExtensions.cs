using System.Net;
using System.Text.Json;
using Light.Results.Http.Writing;

namespace Light.Results.AspNetCore.Mvc;

/// <summary>
/// Extension methods for converting Light.Results to MVC action results.
/// </summary>
public static class MvcActionResultExtensions
{
    /// <summary>
    /// Converts a <see cref="Result" /> to a <see cref="LightActionResult" />.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The MVC action result.</returns>
    public static LightActionResult ToMvcActionResult(
        this Result result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result{T}" /> to a <see cref="LightActionResult{T}" />.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="successStatusCode">Optional success status code override.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The MVC action result.</returns>
    public static LightActionResult<T> ToMvcActionResult<T>(
        this Result<T> result,
        HttpStatusCode? successStatusCode = null,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, successStatusCode, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result" /> to a <see cref="LightActionResult" /> using HTTP 201 Created.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The MVC action result.</returns>
    public static LightActionResult ToHttp201CreatedMvcActionResult(
        this Result result,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);

    /// <summary>
    /// Converts a <see cref="Result{T}" /> to a <see cref="LightActionResult{T}" /> using HTTP 201 Created.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">Optional Location header value.</param>
    /// <param name="overrideOptions">Optional Light.Results options override.</param>
    /// <param name="serializerOptions">Optional JSON serializer options override.</param>
    /// <returns>The MVC action result.</returns>
    public static LightActionResult<T> ToHttp201CreatedMvcActionResult<T>(
        this Result<T> result,
        string? location = null,
        LightResultsHttpWriteOptions? overrideOptions = null,
        JsonSerializerOptions? serializerOptions = null
    ) =>
        new (result, HttpStatusCode.Created, location, overrideOptions, serializerOptions);
}
