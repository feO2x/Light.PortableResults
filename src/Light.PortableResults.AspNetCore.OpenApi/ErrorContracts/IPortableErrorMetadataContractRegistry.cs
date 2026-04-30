using System.Collections.Frozen;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Provides the global map of documented error-code metadata contracts.
/// </summary>
public interface IPortableErrorMetadataContractRegistry
{
    /// <summary>
    /// Gets the immutable map of documented error codes to their metadata contracts.
    /// </summary>
    FrozenDictionary<string, PortableErrorMetadataContract> Contracts { get; }
}
