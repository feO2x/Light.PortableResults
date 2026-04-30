using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

namespace Light.PortableResults.AspNetCore.OpenApi;

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
    /// Gets or sets the inline metadata contracts aligned by index with
    /// <see cref="InlineErrorMetadataCodes" />.
    /// </summary>
    public ErrorMetadataContract[]? InlineErrorMetadataContracts { get; set; }
}
