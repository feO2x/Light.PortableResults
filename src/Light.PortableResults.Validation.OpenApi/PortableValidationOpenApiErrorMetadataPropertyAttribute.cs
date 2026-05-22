using System;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Adds one inline metadata schema property to a matching <see cref="PortableValidationOpenApiErrorHintAttribute" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PortableValidationOpenApiErrorMetadataPropertyAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiErrorMetadataPropertyAttribute" />.
    /// </summary>
    public PortableValidationOpenApiErrorMetadataPropertyAttribute(string code, string key, Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(type);

        Code = code;
        Key = key;
        Type = type;
    }

    /// <summary>
    /// Gets the parent error hint code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the metadata value type.
    /// </summary>
    public Type Type { get; }
}
