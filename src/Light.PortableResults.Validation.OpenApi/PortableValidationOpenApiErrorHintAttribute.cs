using System;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Explicitly documents an error code emitted by validation logic that the source generator cannot infer.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PortableValidationOpenApiErrorHintAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiErrorHintAttribute" />.
    /// </summary>
    public PortableValidationOpenApiErrorHintAttribute(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiErrorHintAttribute" />.
    /// </summary>
    public PortableValidationOpenApiErrorHintAttribute(string code, Type metadataType)
        : this(code)
    {
        ArgumentNullException.ThrowIfNull(metadataType);
        MetadataType = metadataType;
    }

    /// <summary>
    /// Gets the documented error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the optional endpoint-inline metadata type for the error code.
    /// </summary>
    public Type? MetadataType { get; }
}
