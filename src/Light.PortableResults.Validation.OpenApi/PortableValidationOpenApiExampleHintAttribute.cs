using System;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Adds an illustrative validation error entry to the generated OpenAPI response example.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PortableValidationOpenApiExampleHintAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableValidationOpenApiExampleHintAttribute" />.
    /// </summary>
    public PortableValidationOpenApiExampleHintAttribute(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    /// <summary>
    /// Gets the documented example error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets or sets the optional validation target used in the generated example entry.
    /// </summary>
    public string? Target { get; set; }
}
