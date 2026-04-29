using System;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Documents a Light.PortableResults success response.
/// </summary>
/// <remarks>
/// For a given HTTP status code and content type, PortableResults OpenAPI metadata is authoritative.
/// If another OpenAPI contributor already documented the same response slot, this attribute replaces that
/// media-type schema instead of merging it.
/// </remarks>
/// <typeparam name="TValue">The response value type.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ProducesPortableSuccessResponseAttribute<TValue> : PortableOpenApiSuccessResponseAttributeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableSuccessResponseAttribute{TValue}" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableSuccessResponseAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) : base(statusCode, contentType, typeof(TValue)) { }
}
