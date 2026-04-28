using System;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Http;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Documents a Light.PortableResults success response.
/// </summary>
/// <typeparam name="TValue">The response value type.</typeparam>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ProducesPortableSuccessResponseAttribute<TValue>
    : PortableOpenApiResponseAttributeBase, IPortableSuccessResponseOpenApiAttribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="ProducesPortableSuccessResponseAttribute{TValue}" />.
    /// </summary>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    public ProducesPortableSuccessResponseAttribute(
        int statusCode = StatusCodes.Status200OK,
        string contentType = "application/json"
    ) : base(PortableOpenApiResponseKind.SuccessResponse, statusCode, contentType)
    {
        ValueType = typeof(TValue);
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

    bool IPortableSuccessResponseOpenApiAttribute.HasMetadataSerializationModeOverride =>
        HasMetadataSerializationModeOverride;

    private bool HasMetadataSerializationModeOverride { get; set; }
}
