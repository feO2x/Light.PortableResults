using System;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Base class for Light.PortableResults OpenAPI response metadata.
/// </summary>
public abstract class PortableOpenApiResponseAttributeBase : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableOpenApiResponseAttributeBase" />.
    /// </summary>
    /// <param name="kind">The response kind documented by the attribute.</param>
    /// <param name="statusCode">The associated HTTP status code.</param>
    /// <param name="contentType">The associated content type.</param>
    protected PortableOpenApiResponseAttributeBase(
        PortableOpenApiResponseKind kind,
        int statusCode,
        string contentType
    )
    {
        ArgumentNullException.ThrowIfNull(contentType);
        Kind = kind;
        StatusCode = statusCode;
        ContentType = contentType;
    }

    /// <summary>
    /// Gets the documented response kind.
    /// </summary>
    public PortableOpenApiResponseKind Kind { get; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the documented content type.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the optional CLR type used to narrow the top-level <c>metadata</c> schema.
    /// </summary>
    public Type? TopLevelMetadataType { get; set; }
}
