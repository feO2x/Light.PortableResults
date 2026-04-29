using System;
using System.Collections.Generic;
using Light.PortableResults.AspNetCore.OpenApi.Generation;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Builds the global map of documented error-code metadata contracts.
/// </summary>
public sealed class PortableErrorMetadataContractsBuilder
{
    private readonly Dictionary<string, PortableErrorMetadataContract> _contracts = new (StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sanitizedCodes = new (StringComparer.Ordinal);

    internal IReadOnlyDictionary<string, PortableErrorMetadataContract> Contracts => _contracts;

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
        return ForCode(code, PortableErrorMetadataContract.FromType(metadataType));
    }

    /// <summary>
    /// Registers the specified OpenAPI metadata schema factory for the specified code.
    /// </summary>
    public PortableErrorMetadataContractsBuilder ForCode(
        string code,
        Func<OpenApiSpecVersion, OpenApiSchema> metadataSchemaFactory
    )
    {
        return ForCode(code, PortableErrorMetadataContract.FromSchema(metadataSchemaFactory));
    }

    /// <summary>
    /// Registers the specified code as a code that emits no metadata.
    /// </summary>
    public PortableErrorMetadataContractsBuilder ForCode(string code)
    {
        return ForCode(code, PortableErrorMetadataContract.NoMetadata);
    }

    internal PortableErrorMetadataContractsBuilder ForCode(string code, PortableErrorMetadataContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(contract);

        if (_contracts.TryGetValue(code, out var existingContract))
        {
            if (PortableErrorMetadataContractEqualityComparer.Instance.Equals(existingContract, contract))
            {
                return this;
            }

            throw new InvalidOperationException(
                PortableResultsOpenApiMessages.CreateDuplicateErrorMetadataContractMessage(
                    code,
                    existingContract,
                    contract
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

        _contracts.Add(code, contract);
        _sanitizedCodes.Add(sanitizedCode, code);
        return this;
    }
}
