using System;
using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Base class for Light.PortableResults success-response OpenAPI metadata.
/// </summary>
public abstract class PortableOpenApiSuccessResponseAttributeBase : PortableOpenApiResponseAttributeBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableOpenApiSuccessResponseAttributeBase" />.
    /// </summary>
    /// <param name="statusCode">The associated HTTP status code.</param>
    /// <param name="contentType">The associated content type.</param>
    /// <param name="valueType">The response value type.</param>
    protected PortableOpenApiSuccessResponseAttributeBase(int statusCode, string contentType, Type valueType)
        : base(PortableOpenApiResponseKind.SuccessResponse, statusCode, contentType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        ValueType = valueType;
    }

    /// <summary>
    /// Gets the response value type.
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// Gets or sets the optional documentation-only override for the metadata serialization mode.
    /// </summary>
    public MetadataSerializationMode MetadataSerializationMode
    {
        get;
        set
        {
            field = value;
            HasMetadataSerializationModeOverride = true;
        }
    }

    /// <summary>
    /// Indicates whether the metadata serialization mode has been explicitly overridden.
    /// </summary>
    public bool HasMetadataSerializationModeOverride { get; private set; }
}