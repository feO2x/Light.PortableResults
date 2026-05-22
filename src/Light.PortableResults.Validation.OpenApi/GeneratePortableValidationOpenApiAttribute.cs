using System;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Marks a synchronous validator for generated PortableResults validation OpenAPI metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratePortableValidationOpenApiAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the generated endpoint metadata should allow undocumented error codes.
    /// </summary>
    public bool AllowUnknownErrorCodes { get; set; }
}
