using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi;

internal static class PortableResultsOpenApiSchemaNaming
{
    internal static string CreateDerivedEnvelopeSchemaId(
        string canonicalName,
        OpenApiOperation operation,
        ApiDescription apiDescription,
        int statusCode,
        string contentType
    )
    {
        var operationToken = CreateOperationToken(operation, apiDescription);
        return $"{canonicalName}__{operationToken}__{statusCode}__{SanitizeSegment(contentType)}";
    }

    internal static string CreateInlineErrorSchemaId(
        string baseSchemaName,
        OpenApiOperation operation,
        ApiDescription apiDescription,
        int statusCode,
        string contentType,
        string errorCode
    )
    {
        var operationToken = CreateOperationToken(operation, apiDescription);
        return
            $"{baseSchemaName}__{operationToken}__{statusCode}__{SanitizeSegment(contentType)}__{SanitizeErrorCode(errorCode)}";
    }

    internal static string CreateGlobalErrorSchemaId(string baseSchemaName, string errorCode)
    {
        return $"{baseSchemaName}__{SanitizeErrorCode(errorCode)}";
    }

    internal static string CreateMetadataSchemaId(string ownerSchemaId)
    {
        return $"{ownerSchemaId}__Metadata";
    }

    internal static string EscapeJsonPointer(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace("~", "~0", StringComparison.Ordinal)
                    .Replace("/", "~1", StringComparison.Ordinal);
    }

    internal static string SanitizeErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return SanitizeSegment(value);
    }

    internal static string SanitizeSegment(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Span<char> buffer = stackalloc char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            buffer[i] = char.IsAsciiLetterOrDigit(character) || character == '_' ? character : '_';
        }

        return new string(buffer);
    }

    internal static string SanitizeRoutePattern(string routePattern)
    {
        ArgumentNullException.ThrowIfNull(routePattern);

        Span<char> buffer = stackalloc char[routePattern.Length];
        var outputIndex = 0;
        var lastCharacterWasReplacement = false;
        foreach (var character in routePattern)
        {
            var isAllowed = char.IsAsciiLetterOrDigit(character) || character == '_';
            if (isAllowed)
            {
                buffer[outputIndex++] = character;
                lastCharacterWasReplacement = false;
            }
            else if (!lastCharacterWasReplacement)
            {
                buffer[outputIndex++] = '_';
                lastCharacterWasReplacement = true;
            }
        }

        if (outputIndex == 0)
        {
            return "root";
        }

        var sanitized = new string(buffer[..outputIndex]).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "root" : sanitized;
    }

    private static string CreateOperationToken(OpenApiOperation operation, ApiDescription apiDescription)
    {
        if (!string.IsNullOrWhiteSpace(operation.OperationId))
        {
            return SanitizeSegment(operation.OperationId);
        }

        var httpMethod = apiDescription.HttpMethod ?? "Unknown";
        var routePattern = apiDescription.RelativePath ?? string.Empty;
        return $"{SanitizeSegment(httpMethod)}__{SanitizeRoutePattern(routePattern)}";
    }
}
