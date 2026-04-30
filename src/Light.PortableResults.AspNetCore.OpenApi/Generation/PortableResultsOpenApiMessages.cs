using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;

namespace Light.PortableResults.AspNetCore.OpenApi.Generation;

internal static class PortableResultsOpenApiMessages
{
    internal static string CreateDuplicateErrorMetadataContractMessage(
        string code,
        ErrorMetadataContract existingContract,
        ErrorMetadataContract newContract
    ) =>
        $"The error code '{code}' is already registered with metadata contract '{DescribeContract(existingContract)}'. It cannot also be registered with '{DescribeContract(newContract)}'.";

    internal static string CreateSanitizedErrorCodeCollisionMessage(
        string firstCode,
        string secondCode,
        string sanitizedCode
    ) =>
        $"The error codes '{firstCode}' and '{secondCode}' both sanitize to '{sanitizedCode}'. Error-code schema ids must be unique.";

    internal static string CreateUnknownErrorCodeMessage(string code) =>
        $"The error code '{code}' is not registered in AddPortableResultsOpenApi. Register it globally or use WithErrorMetadata as an inline escape hatch.";

    internal static string CreateIncompleteInlineErrorMetadataMessage() =>
        "Inline error metadata must configure both InlineErrorMetadataCodes and InlineErrorMetadataContracts together.";

    private static string DescribeContract(ErrorMetadataContract contract)
    {
        return contract switch
        {
            ErrorMetadataTypeContract typeContract => typeContract.MetadataType.FullName ??
                                                             typeContract.MetadataType.Name,
            ErrorMetadataSchemaContract schemaContract => DescribeSchemaFactory(schemaContract),
            NoMetadataContract => "no metadata",
            _ => contract.GetType().FullName ?? contract.GetType().Name
        };
    }

    private static string DescribeSchemaFactory(ErrorMetadataSchemaContract schemaContract)
        => "schema factory " + schemaContract.DiagnosticName;
}
