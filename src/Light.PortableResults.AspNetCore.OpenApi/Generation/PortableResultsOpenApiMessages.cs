using System;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

namespace Light.PortableResults.AspNetCore.OpenApi.Generation;

internal static class PortableResultsOpenApiMessages
{
    internal static string CreateDuplicateErrorMetadataContractMessage(
        string code,
        Type existingType,
        Type newType
    ) =>
        $"The error code '{code}' is already registered with metadata type '{existingType.FullName}'. It cannot also be registered with '{newType.FullName}'.";

    internal static string CreateDuplicateErrorMetadataContractMessage(
        string code,
        PortableErrorMetadataContract existingContract,
        PortableErrorMetadataContract newContract
    ) =>
        $"The error code '{code}' is already registered with metadata contract '{DescribeContract(existingContract)}'. It cannot also be registered with '{DescribeContract(newContract)}'.";

    internal static string CreateSanitizedErrorCodeCollisionMessage(
        string firstCode,
        string secondCode,
        string sanitizedCode
    ) =>
        $"The error codes '{firstCode}' and '{secondCode}' both sanitize to '{sanitizedCode}'. Error-code schema ids must be unique.";

    internal static string CreateUnknownErrorCodeMessage(string code) =>
        $"The error code '{code}' is not registered in ConfigureErrorMetadataContracts. Register it globally or use WithErrorMetadata as an inline escape hatch.";

    internal static string CreateIncompleteInlineErrorMetadataMessage() =>
        "Inline error metadata must configure both InlineErrorMetadataCodes and InlineErrorMetadataTypes together.";

    private static string DescribeContract(PortableErrorMetadataContract contract)
    {
        return contract switch
        {
            PortableErrorMetadataTypeContract typeContract => typeContract.MetadataType.FullName ??
                                                             typeContract.MetadataType.Name,
            PortableErrorMetadataSchemaContract => "schema factory",
            PortableErrorMetadataNoMetadataContract => "no metadata",
            _ => contract.GetType().FullName ?? contract.GetType().Name
        };
    }
}
