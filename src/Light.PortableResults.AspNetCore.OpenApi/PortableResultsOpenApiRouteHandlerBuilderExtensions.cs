using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// OpenAPI route-handler helpers for Light.PortableResults.
/// </summary>
public static class PortableResultsOpenApiRouteHandlerBuilderExtensions
{
    /// <summary>
    /// Documents a Light.PortableResults success response.
    /// </summary>
    /// <remarks>
    /// For a given HTTP status code and content type, PortableResults OpenAPI metadata is authoritative.
    /// If another OpenAPI contributor already documented the same response slot, this helper replaces that
    /// media-type schema instead of merging it.
    /// </remarks>
    public static RouteHandlerBuilder ProducesPortableSuccessResponse<TValue>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json",
        Action<PortableSuccessResponseOpenApiBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var attribute = new ProducesPortableSuccessResponseAttribute<TValue>(statusCode, contentType);
        configure?.Invoke(
            new PortableSuccessResponseOpenApiBuilder(
                attribute,
                mode => attribute.MetadataSerializationMode = mode
            )
        );
        return builder.WithMetadata(attribute);
    }

    /// <summary>
    /// Documents a Light.PortableResults problem details response.
    /// </summary>
    /// <remarks>
    /// For a given HTTP status code and content type, PortableResults OpenAPI metadata is authoritative.
    /// If another OpenAPI contributor already documented the same response slot, this helper replaces that
    /// media-type schema instead of merging it.
    /// </remarks>
    public static RouteHandlerBuilder ProducesPortableProblem(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json",
        Action<PortableProblemOpenApiBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var attribute = new ProducesPortableProblemAttribute(statusCode, contentType);
        configure?.Invoke(new PortableProblemOpenApiBuilder(attribute));
        return builder.WithMetadata(attribute);
    }

    /// <summary>
    /// Documents a Light.PortableResults validation problem details response.
    /// </summary>
    /// <remarks>
    /// For a given HTTP status code and content type, PortableResults OpenAPI metadata is authoritative.
    /// If another OpenAPI contributor already documented the same response slot, this helper replaces that
    /// media-type schema instead of merging it.
    /// </remarks>
    public static RouteHandlerBuilder ProducesPortableValidationProblem(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json",
        Action<PortableValidationProblemOpenApiBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var attribute = new ProducesPortableValidationProblemAttribute(statusCode, contentType);
        configure?.Invoke(new PortableValidationProblemOpenApiBuilder(attribute));
        return builder.WithMetadata(attribute);
    }
}
