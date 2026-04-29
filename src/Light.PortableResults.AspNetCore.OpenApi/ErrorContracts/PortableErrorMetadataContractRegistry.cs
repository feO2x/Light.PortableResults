using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Light.PortableResults.AspNetCore.OpenApi.Generation;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;

namespace Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

/// <summary>
/// Default implementation of <see cref="IPortableErrorMetadataContractRegistry" />.
/// </summary>
public sealed class PortableErrorMetadataContractRegistry : IPortableErrorMetadataContractRegistry
{
    /// <summary>
    /// Initializes a new instance of <see cref="PortableErrorMetadataContractRegistry" />.
    /// </summary>
    /// <param name="builder">The builder that holds the configured contracts.</param>
    public PortableErrorMetadataContractRegistry(PortableErrorMetadataContractsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var contracts = new Dictionary<string, PortableErrorMetadataContract>(StringComparer.Ordinal);
        var sanitizedCodes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (code, contract) in builder.Contracts)
        {
            if (contracts.TryGetValue(code, out var existingContract))
            {
                if (PortableErrorMetadataContractEqualityComparer.Instance.Equals(existingContract, contract))
                {
                    continue;
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
            // ForCode already rejects sanitized-name collisions, but the registry is the final snapshot
            // boundary before document generation and keeps the same guard in case the builder contents
            // were composed outside that API or future option-pipeline changes bypass the builder check.
            if (sanitizedCodes.TryGetValue(sanitizedCode, out var existingCode))
            {
                throw new InvalidOperationException(
                    PortableResultsOpenApiMessages.CreateSanitizedErrorCodeCollisionMessage(
                        existingCode,
                        code,
                        sanitizedCode
                    )
                );
            }

            contracts.Add(code, contract);
            sanitizedCodes.Add(sanitizedCode, code);
        }

        Contracts = new ReadOnlyDictionary<string, PortableErrorMetadataContract>(contracts);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, PortableErrorMetadataContract> Contracts { get; }
}
