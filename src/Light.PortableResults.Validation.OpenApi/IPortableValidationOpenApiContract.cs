using Light.PortableResults.AspNetCore.OpenApi;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Exposes generated validation OpenAPI configuration without reflection.
/// </summary>
public interface IPortableValidationOpenApiContract
{
    /// <summary>
    /// Applies the validator's generated validation OpenAPI metadata to the specified builder.
    /// </summary>
    static abstract void ConfigurePortableValidationOpenApi(PortableValidationProblemOpenApiBuilder builder);
}
