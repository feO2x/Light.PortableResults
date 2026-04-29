using System;
using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Configures a documented Light.PortableResults success response.
/// </summary>
public sealed class PortableSuccessResponseOpenApiBuilder
{
    private readonly PortableOpenApiSuccessResponseAttributeBase _attribute;
    private readonly Action<MetadataSerializationMode> _setMetadataSerializationMode;

    internal PortableSuccessResponseOpenApiBuilder(
        PortableOpenApiSuccessResponseAttributeBase attribute,
        Action<MetadataSerializationMode> setMetadataSerializationMode
    )
    {
        _attribute = attribute;
        _setMetadataSerializationMode = setMetadataSerializationMode;
    }

    /// <summary>
    /// Narrows the top-level metadata schema to <typeparamref name="TMetadata" />.
    /// </summary>
    public PortableSuccessResponseOpenApiBuilder WithMetadata<TMetadata>()
    {
        _attribute.TopLevelMetadataType = typeof(TMetadata);
        return this;
    }

    /// <summary>
    /// Narrows the top-level metadata schema to the specified CLR type.
    /// </summary>
    public PortableSuccessResponseOpenApiBuilder WithMetadata(Type metadataType)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        _attribute.TopLevelMetadataType = metadataType;
        return this;
    }

    /// <summary>
    /// Overrides the documented metadata serialization mode for this endpoint.
    /// </summary>
    public PortableSuccessResponseOpenApiBuilder UseMetadataSerializationMode(MetadataSerializationMode mode)
    {
        _setMetadataSerializationMode(mode);
        return this;
    }
}
