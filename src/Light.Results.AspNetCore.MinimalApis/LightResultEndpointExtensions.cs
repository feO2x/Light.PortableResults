using Light.Results.AspNetCore.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Extension methods for configuring OpenAPI metadata for Light.Results endpoints.
/// </summary>
public static class LightResultEndpointExtensions
{
    /// <summary>
    /// Adds OpenAPI response metadata for LightSuccessResult with typed metadata schema.
    /// Use this when you want full schema documentation for your metadata type.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata (for OpenAPI schema generation).</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesLightResult<TValue, TMetadata>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    )
    {
        return builder.Produces<WrappedResponse<TValue, TMetadata>>(statusCode, contentType);
    }

    /// <summary>
    /// Adds OpenAPI response metadata for LightSuccessResult with untyped metadata.
    /// The metadata will be documented as an object with additionalProperties.
    /// </summary>
    /// <typeparam name="TValue">The type of the result value.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="statusCode">The HTTP status code (default 200).</param>
    /// <param name="contentType">The content type (default "application/json").</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder ProducesLightResult<TValue>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    )
    {
        return builder.Produces<WrappedResponse<TValue, object>>(statusCode, contentType);
    }
}
