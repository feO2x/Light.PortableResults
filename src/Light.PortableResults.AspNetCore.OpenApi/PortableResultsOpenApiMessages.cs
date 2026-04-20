using System;

namespace Light.PortableResults.AspNetCore.OpenApi;

internal static class PortableResultsOpenApiMessages
{
    internal static string CreateDuplicateErrorMetadataContractMessage(
        string code,
        Type existingType,
        Type newType
    ) =>
        $"The error code '{code}' is already registered with metadata type '{existingType.FullName}'. It cannot also be registered with '{newType.FullName}'.";

    internal static string CreateSanitizedErrorCodeCollisionMessage(
        string firstCode,
        string secondCode,
        string sanitizedCode
    ) =>
        $"The error codes '{firstCode}' and '{secondCode}' both sanitize to '{sanitizedCode}'. Error-code schema ids must be unique.";

    internal static string CreateUnknownErrorCodeMessage(string code) =>
        $"The error code '{code}' is not registered in ConfigureErrorMetadataContracts. Register it globally or use WithErrorMetadata as an inline escape hatch.";
}
