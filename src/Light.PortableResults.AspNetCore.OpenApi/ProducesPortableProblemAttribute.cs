using System;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Documents a Light.PortableResults problem details response.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ProducesPortableProblemAttribute : PortableOpenApiErrorResponseAttributeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableProblemAttribute" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableProblemAttribute(
        int statusCode = StatusCodes.Status500InternalServerError,
        string contentType = "application/problem+json"
    ) : base(PortableOpenApiResponseKind.Problem, statusCode, contentType) { }
}
