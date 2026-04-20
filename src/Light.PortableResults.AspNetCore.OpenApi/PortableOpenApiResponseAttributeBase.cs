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

/// <summary>
/// Base class for Light.PortableResults error-response OpenAPI metadata.
/// </summary>
public abstract class PortableOpenApiErrorResponseAttributeBase : PortableOpenApiResponseAttributeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableOpenApiErrorResponseAttributeBase" />.
    /// </summary>
    /// <param name="kind">The response kind documented by the attribute.</param>
    /// <param name="statusCode">The associated HTTP status code.</param>
    /// <param name="contentType">The associated content type.</param>
    protected PortableOpenApiErrorResponseAttributeBase(
        PortableOpenApiResponseKind kind,
        int statusCode,
        string contentType
    ) : base(kind, statusCode, contentType) { }

    /// <summary>
    /// Gets or sets the globally registered error codes that should be narrowed on the response.
    /// </summary>
    public string[]? ErrorCodes { get; set; }

    /// <summary>
    /// Gets or sets the inline error codes whose metadata schema is defined directly on the endpoint.
    /// </summary>
    public string[]? InlineErrorMetadataCodes { get; set; }

    /// <summary>
    /// Gets or sets the inline metadata CLR types aligned by index with
    /// <see cref="InlineErrorMetadataCodes" />.
    /// </summary>
    public Type[]? InlineErrorMetadataTypes { get; set; }
}
