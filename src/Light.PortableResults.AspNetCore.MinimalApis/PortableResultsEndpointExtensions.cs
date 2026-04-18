using Light.PortableResults.AspNetCore.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.MinimalApis;

/// <summary>
/// Extension methods that add OpenAPI response metadata for Light.PortableResults endpoints.
/// These helpers are documentation-only: they register schema-only CLR types with the endpoint so
/// OpenAPI generators can emit accurate response schemas. The runtime HTTP serialization behavior is
/// unaffected.
/// </summary>
public static class PortableResultsEndpointExtensions
{
    /// <summary>
    /// Documents a successful response whose body contains both a <typeparamref name="TValue" /> and
    /// a <typeparamref name="TMetadata" /> metadata object. The response type is documented as
    /// <see cref="PortableSuccessResponse{TValue,TMetadata}" />.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TMetadata">The type of the success metadata.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableSuccessResponse<TValue, TMetadata>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) =>
        builder.Produces<PortableSuccessResponse<TValue, TMetadata>>(statusCode, contentType);

    /// <summary>
    /// Documents a Light.PortableResults problem details failure response with untyped metadata.
    /// Use the relevant <paramref name="statusCode" /> (e.g. 401, 403, 404, 409, 500) to document
    /// the expected non-validation failure response for the endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 500 Internal Server Error).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableProblem(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableProblemDetails>(statusCode, contentType);

    /// <summary>
    /// Documents a Light.PortableResults problem details failure response with strongly typed metadata.
    /// Use the relevant <paramref name="statusCode" /> to document the expected non-validation failure
    /// response for the endpoint.
    /// </summary>
    /// <typeparam name="TErrorMetadata">The type of the metadata on each error.</typeparam>
    /// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 500 Internal Server Error).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableProblem<TErrorMetadata, TProblemMetadata>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableProblemDetails<TErrorMetadata, TProblemMetadata>>(statusCode, contentType);

    /// <summary>
    /// Documents a rich Light.PortableResults validation problem details response with untyped metadata.
    /// Use this helper when <c>ValidationProblemSerializationFormat</c> is set to
    /// <c>Rich</c>.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableRichValidationProblem(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableRichValidationProblemDetails>(statusCode, contentType);

    /// <summary>
    /// Documents a rich Light.PortableResults validation problem details response with strongly typed
    /// metadata. Use this helper when <c>ValidationProblemSerializationFormat</c> is set to
    /// <c>Rich</c>.
    /// </summary>
    /// <typeparam name="TErrorMetadata">The type of the metadata on each validation error.</typeparam>
    /// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableRichValidationProblem<TErrorMetadata, TProblemMetadata>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableRichValidationProblemDetails<TErrorMetadata, TProblemMetadata>>(
            statusCode,
            contentType
        );

    /// <summary>
    /// Documents an ASP.NET Core-compatible Light.PortableResults validation problem details response with
    /// untyped metadata. Use this helper when <c>ValidationProblemSerializationFormat</c> is set to
    /// <c>AspNetCoreCompatible</c>.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableAspNetCoreValidationProblem(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableAspNetCoreValidationProblemDetails>(statusCode, contentType);

    /// <summary>
    /// Documents an ASP.NET Core-compatible Light.PortableResults validation problem details response with
    /// strongly typed metadata. Use this helper when <c>ValidationProblemSerializationFormat</c> is set to
    /// <c>AspNetCoreCompatible</c>.
    /// </summary>
    /// <typeparam name="TErrorDetailMetadata">The type of the metadata on each error details entry.</typeparam>
    /// <typeparam name="TProblemMetadata">The type of the top-level problem metadata.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 400 Bad Request).</param>
    /// <param name="contentType">The content type (default "application/problem+json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesPortableAspNetCoreValidationProblem<
        TErrorDetailMetadata,
        TProblemMetadata
    >(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) =>
        builder.Produces<PortableAspNetCoreValidationProblemDetails<TErrorDetailMetadata, TProblemMetadata>>(
            statusCode,
            contentType
        );
}
