using System;
using System.Collections.Generic;
using Light.PortableResults.AspNetCore.OpenApi.Generation;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Builds the global map of documented error-code metadata contracts.
/// </summary>
public sealed class PortableErrorMetadataContractsBuilder
{
    private readonly Dictionary<string, Type> _contracts = new (StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sanitizedCodes = new (StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, Type> Contracts => _contracts;

    /// <summary>
    /// Registers <typeparamref name="TMetadata" /> as the metadata contract for the specified code.
    /// </summary>
    public PortableErrorMetadataContractsBuilder ForCode<TMetadata>(string code)
    {
        return ForCode(code, typeof(TMetadata));
    }

    /// <summary>
    /// Registers the specified CLR metadata type for the specified code.
    /// </summary>
    public PortableErrorMetadataContractsBuilder ForCode(string code, Type metadataType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(metadataType);

        if (_contracts.TryGetValue(code, out var existingType))
        {
            if (existingType == metadataType)
            {
                return this;
            }

            throw new InvalidOperationException(
                PortableResultsOpenApiMessages.CreateDuplicateErrorMetadataContractMessage(
                    code,
                    existingType,
                    metadataType
                )
            );
        }

        var sanitizedCode = PortableResultsOpenApiSchemaNaming.SanitizeErrorCode(code);
        if (_sanitizedCodes.TryGetValue(sanitizedCode, out var existingRawCode))
        {
            throw new InvalidOperationException(
                PortableResultsOpenApiMessages.CreateSanitizedErrorCodeCollisionMessage(
                    existingRawCode,
                    code,
                    sanitizedCode
                )
            );
        }

        _contracts.Add(code, metadataType);
        _sanitizedCodes.Add(sanitizedCode, code);
        return this;
    }
}
