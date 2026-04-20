using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Light.PortableResults.AspNetCore.OpenApi;

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

        var contracts = new Dictionary<string, Type>(StringComparer.Ordinal);
        var sanitizedCodes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (code, metadataType) in builder.Contracts)
        {
            if (contracts.TryGetValue(code, out var existingType))
            {
                if (existingType == metadataType)
                {
                    continue;
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

            contracts.Add(code, metadataType);
            sanitizedCodes.Add(sanitizedCode, code);
        }

        Contracts = new ReadOnlyDictionary<string, Type>(contracts);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Type> Contracts { get; }
}
