using System;
using System.Collections.Generic;
using Light.PortableResults.AspNetCore.OpenApi.Generation;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Builds the global map of documented error-code metadata contracts.
/// </summary>
public sealed class ErrorMetadataContractsBuilder
{
    private readonly Dictionary<string, ErrorMetadataContract> _contracts = new (StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sanitizedCodes = new (StringComparer.Ordinal);

    /// <summary>
    /// Gets the immutable map of documented error codes to their metadata contracts.
    /// </summary>
    public IReadOnlyDictionary<string, ErrorMetadataContract> Contracts => _contracts;

    /// <summary>
    /// Registers <typeparamref name="TMetadata" /> as the metadata contract for the specified code.
    /// </summary>
    public ErrorMetadataContractsBuilder ForCode<TMetadata>(string code)
    {
        return ForCode(code, typeof(TMetadata));
    }

    /// <summary>
    /// Registers the specified CLR metadata type for the specified code.
    /// </summary>
    public ErrorMetadataContractsBuilder ForCode(string code, Type metadataType)
    {
        return ForCode(code, ErrorMetadataContract.FromType(metadataType));
    }

    /// <summary>
    /// Registers the specified OpenAPI metadata schema factory for the specified code.
    /// </summary>
    /// <param name="code">The error code to register.</param>
    /// <param name="metadataSchemaFactory">The factory that creates a fresh metadata schema for the requested OpenAPI version.</param>
    /// <param name="schemaId">The schema ID that uniquely identifies this contract. When null, the ID is derived from the factory's method metadata.</param>
    public ErrorMetadataContractsBuilder ForCode(
        string code,
        Func<OpenApiSpecVersion, OpenApiSchema> metadataSchemaFactory,
        string? schemaId = null
    )
    {
        return ForCode(code, ErrorMetadataContract.FromSchema(metadataSchemaFactory, schemaId));
    }

    /// <summary>
    /// Registers the specified code as a code that emits no metadata.
    /// </summary>
    public ErrorMetadataContractsBuilder ForCode(string code)
    {
        return ForCode(code, ErrorMetadataContract.NoMetadata);
    }

    private ErrorMetadataContractsBuilder ForCode(string code, ErrorMetadataContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(contract);

        if (_contracts.TryGetValue(code, out var existingContract))
        {
            if (existingContract.Equals(contract))
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
