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
                    builder.ForCode(code, typeContract.MetadataType);
                    break;
                case ErrorMetadataSchemaContract schemaContract:
                    builder.ForCode(code, schemaContract.SchemaFactory, schemaContract.SchemaId);
                    break;
                case NoMetadataContract:
                    builder.ForCode(code);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"The error metadata contract '{contract.GetType().FullName}' is not supported."
                    );
            }
        }

        return builder;
    }
}
