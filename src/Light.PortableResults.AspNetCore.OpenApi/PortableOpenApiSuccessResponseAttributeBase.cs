using System;
using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Base class for Light.PortableResults success-response OpenAPI metadata.
/// </summary>
/// <remarks>
/// The metadata serialization mode override is represented by the combination of
/// <see cref="MetadataSerializationMode" /> and <see cref="HasMetadataSerializationModeOverride" />.
/// This type intentionally does not use a nullable enum property because MVC attribute named arguments
/// must use attribute-compatible property types; changing the property to
/// <see cref="Nullable{T}" /> would trigger compiler error CS0655 for usages such as
/// <c>[ProducesPortableSuccessResponse(MetadataSerializationMode = MetadataSerializationMode.Always)]</c>.
/// </remarks>
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
    /// Gets or sets the documentation-only override for the metadata serialization mode.
    /// </summary>
    /// <remarks>
    /// Read this property together with <see cref="HasMetadataSerializationModeOverride" />.
    /// When <see cref="HasMetadataSerializationModeOverride" /> is <see langword="false" />,
    /// the value returned by this property is only the enum's default value and does not indicate
    /// that an explicit override was configured.
    /// </remarks>
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
    /// Indicates whether <see cref="MetadataSerializationMode" /> was explicitly overridden.
    /// </summary>
    /// <remarks>
    /// This mirror property exists because attribute properties cannot use a nullable enum type without
    /// breaking MVC attribute named arguments with compiler error CS0655.
    /// </remarks>
    public bool HasMetadataSerializationModeOverride { get; private set; }
}