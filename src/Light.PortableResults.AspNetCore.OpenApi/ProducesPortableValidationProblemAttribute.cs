using System;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Documents a Light.PortableResults validation problem response.
/// </summary>
/// <remarks>
/// For a given HTTP status code and content type, PortableResults OpenAPI metadata is authoritative.
/// If another OpenAPI contributor already documented the same response slot, this attribute replaces that
/// media-type schema instead of merging it.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ProducesPortableValidationProblemAttribute : PortableOpenApiErrorResponseAttributeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableValidationProblemAttribute" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableValidationProblemAttribute(
        int statusCode = StatusCodes.Status400BadRequest,
        string contentType = "application/problem+json"
    ) : base(PortableOpenApiResponseKind.ValidationProblem, statusCode, contentType) { }

    /// <summary>
    /// Gets or sets the optional documentation-only override for the validation serialization format.
    /// </summary>
    public ValidationProblemSerializationFormat Format
    {
        get;
        set
        {
            field = value;
            HasFormatOverride = true;
        }
    }

    /// <summary>
    /// Indicates whether the serialization format for the validation problem response has been explicitly overridden.
    /// </summary>
    public bool HasFormatOverride { get; private set; }
}
