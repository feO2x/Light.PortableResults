using System;
using Light.PortableResults.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Documents an MVC validation problem response by applying the generated OpenAPI contract of
/// <typeparamref name="TValidator" />.
/// </summary>
/// <typeparam name="TValidator">The validator type that exposes generated validation OpenAPI metadata.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesPortableValidationProblemForAttribute<TValidator>
    : ProducesPortableValidationProblemAttribute
    where TValidator : IPortableValidationOpenApiContract
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableValidationProblemForAttribute{TValidator}" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableValidationProblemForAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType)
    {
        TValidator.ConfigurePortableValidationOpenApi(new PortableValidationProblemOpenApiBuilder(this));
    }
}

/// <summary>
/// Documents an MVC validation problem response by applying the generated OpenAPI contract of
/// <typeparamref name="TValidator" /> and then endpoint-local OpenAPI customizations from
/// <typeparamref name="TEndpointContract" />.
/// </summary>
/// <typeparam name="TValidator">The validator type that exposes generated validation OpenAPI metadata.</typeparam>
/// <typeparam name="TEndpointContract">The endpoint-local contract that customizes the generated metadata.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ProducesPortableValidationProblemForAttribute<TValidator, TEndpointContract>
    : ProducesPortableValidationProblemAttribute
    where TValidator : IPortableValidationOpenApiContract
    where TEndpointContract : IPortableValidationOpenApiContract
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ProducesPortableValidationProblemForAttribute{TValidator, TEndpointContract}" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableValidationProblemForAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(statusCode, contentType)
    {
        var builder = new PortableValidationProblemOpenApiBuilder(this);
        TValidator.ConfigurePortableValidationOpenApi(builder);
        TEndpointContract.ConfigurePortableValidationOpenApi(builder);
    }
}
