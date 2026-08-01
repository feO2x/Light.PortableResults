using System;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

namespace Light.PortableResults.Validation.OpenApi;

/// <summary>
/// Provides registration helpers for built-in validation error metadata contracts.
/// </summary>
public static class BuiltInValidationErrorContractRegistrationExtensions
{
    /// <summary>
    /// Registers the built-in validation error metadata contracts.
    /// </summary>
    /// <param name="builder">The error metadata contract builder.</param>
    /// <returns>The configured builder.</returns>
    public static ErrorMetadataContractsBuilder RegisterBuiltInValidationErrors(
        this ErrorMetadataContractsBuilder builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (var (code, contract) in BuiltInValidationErrorContracts.Contracts)
        {
            switch (contract)
            {
                case ErrorMetadataTypeContract typeContract:
                    // The built-in registry holds schema and no-metadata contracts exclusively, so this arm is
                    // unreachable today. It stays correct for a registry that later gains a type contract.
                    // Stryker disable once Statement : unreachable - the built-in registry never holds a type contract
                    builder.ForCode(code, typeContract.MetadataType);
                    break;
                case ErrorMetadataSchemaContract schemaContract:
                    builder.ForCode(code, schemaContract.SchemaFactory, schemaContract.SchemaId);
                    break;
                case NoMetadataContract:
                    builder.ForCode(code);
                    break;
                default:
                    // ErrorMetadataContract declares a private protected constructor and has exactly three sealed
                    // subclasses, so no fourth contract kind can exist and this arm cannot be reached.
                    // Stryker disable once String : unreachable - ErrorMetadataContract permits no fourth subclass
                    throw new InvalidOperationException(
                        $"The error metadata contract '{contract.GetType().FullName}' is not supported."
                    );
            }
        }

        return builder;
    }
}
