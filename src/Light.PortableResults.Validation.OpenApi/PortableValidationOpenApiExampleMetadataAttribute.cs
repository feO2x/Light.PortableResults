using System;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Adds one constant metadata value to a matching <see cref="PortableValidationOpenApiExampleHintAttribute" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PortableValidationOpenApiExampleMetadataAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, string? value)
        : this(code, key) =>
        StringValue = value;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, int value)
        : this(code, key) =>
        Int64Value = value;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, long value)
        : this(code, key) =>
        Int64Value = value;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, bool value)
        : this(code, key) =>
        BooleanValue = value;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, Type value)
        : this(code, key)
    {
        ArgumentNullException.ThrowIfNull(value);
        TypeValue = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleMetadataAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleMetadataAttribute(string code, string key, double value)
        : this(code, key) =>
        DoubleValue = value;

    private PortableValidationOpenApiExampleMetadataAttribute(string code, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Code = code;
        Key = key;
    }

    /// <summary>
    /// Gets the parent example error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the metadata key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the optional string value.
    /// </summary>
    public string? StringValue { get; }

    /// <summary>
    /// Gets the optional integer value.
    /// </summary>
    public long? Int64Value { get; }

    /// <summary>
    /// Gets the optional Boolean value.
    /// </summary>
    public bool? BooleanValue { get; }

    /// <summary>
    /// Gets the optional type value.
    /// </summary>
    public Type? TypeValue { get; }

    /// <summary>
    /// Gets the optional floating-point value.
    /// </summary>
    public double? DoubleValue { get; }
}
