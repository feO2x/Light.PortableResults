using System;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Minimal API helpers for source-generated validation OpenAPI metadata.
/// </summary>
public static class PortableValidationOpenApiRouteHandlerBuilderExtensions
{
    /// <summary>
    /// Documents a validation problem response by applying the generated OpenAPI contract of
    /// <typeparamref name="TValidator" />.
    /// </summary>
    public static RouteHandlerBuilder ProducesPortableValidationProblemFor<TValidator>(
        this RouteHandlerBuilder builder,
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json",
        Action<PortableValidationProblemOpenApiBuilder>? configure = null
    )
        where TValidator : IPortableValidationOpenApiContract
    {
        // The delegated ProducesPortableValidationProblem guards the same parameter, so the observable contract is
        // identical with or without this guard.
        // Stryker disable once Statement : equivalent - ProducesPortableValidationProblem guards the same parameter
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ProducesPortableValidationProblem(
            statusCode,
            contentType,
            openApiBuilder =>
            {
                TValidator.ConfigurePortableValidationOpenApi(openApiBuilder);
                configure?.Invoke(openApiBuilder);
            }
        );
    }
}
