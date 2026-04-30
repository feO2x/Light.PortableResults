namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Options backing the global error-code metadata contract registry.
/// </summary>
public sealed class ErrorMetadataContractsOptions
{
    /// <summary>
    /// Gets the mutable builder populated through the options pipeline.
    /// </summary>
    public ErrorMetadataContractsBuilder Builder { get; } = new ();
}
