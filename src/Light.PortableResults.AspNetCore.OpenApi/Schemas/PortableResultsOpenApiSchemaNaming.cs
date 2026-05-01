using System;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.Schemas;

/// <summary>
/// Provides helpers for creating stable OpenAPI component schema ids used by Light.PortableResults.
/// </summary>
public static class PortableResultsOpenApiSchemaNaming
{
    /// <summary>
    /// Creates a schema id for an endpoint-specific response envelope derived from a canonical base schema.
    /// </summary>
    /// <param name="canonicalName">The canonical base schema name.</param>
    /// <param name="operation">The OpenAPI operation that owns the derived schema.</param>
    /// <param name="apiDescription">The ASP.NET API description for the operation.</param>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    /// <returns>The derived schema id.</returns>
    public static string CreateDerivedEnvelopeSchemaId(
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

    /// <summary>
    /// Creates a schema id for an endpoint-specific error-item variant declared inline on an endpoint.
    /// </summary>
    /// <param name="baseSchemaName">The canonical base schema name for the error item.</param>
    /// <param name="operation">The OpenAPI operation that owns the derived schema.</param>
    /// <param name="apiDescription">The ASP.NET API description for the operation.</param>
    /// <param name="statusCode">The documented HTTP status code.</param>
    /// <param name="contentType">The documented content type.</param>
    /// <param name="errorCode">The error code represented by the schema.</param>
    /// <returns>The inline error schema id.</returns>
    public static string CreateInlineErrorSchemaId(
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

    /// <summary>
    /// Creates a schema id for a globally registered error-code-specific schema.
    /// </summary>
    /// <param name="baseSchemaName">The canonical base schema name for the error item.</param>
    /// <param name="errorCode">The registered error code.</param>
    /// <returns>The global error schema id.</returns>
    public static string CreateGlobalErrorSchemaId(string baseSchemaName, string errorCode)
    {
        return $"{baseSchemaName}__{SanitizeErrorCode(errorCode)}";
    }

    /// <summary>
    /// Creates a schema id for a metadata schema owned by another schema component.
    /// </summary>
    /// <param name="ownerSchemaId">The schema id of the owning component.</param>
    /// <returns>The metadata schema id.</returns>
    public static string CreateMetadataSchemaId(string ownerSchemaId)
    {
        return $"{ownerSchemaId}__Metadata";
    }

    /// <summary>
    /// Sanitizes an error code so it can be embedded safely into an OpenAPI component schema id.
    /// </summary>
    /// <param name="value">The raw error code.</param>
    /// <returns>The sanitized error code.</returns>
    public static string SanitizeErrorCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return SanitizeSegment(value);
    }

    /// <summary>
    /// Replaces characters that are unsuitable for component schema ids with underscores.
    /// </summary>
    /// <param name="value">The raw segment value.</param>
    /// <returns>The sanitized segment.</returns>
    private static string SanitizeSegment(string value)
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

    /// <summary>
    /// Sanitizes a route pattern into a compact token suitable for schema ids.
    /// </summary>
    /// <param name="routePattern">The raw route pattern.</param>
    /// <returns>The sanitized route token.</returns>
    public static string SanitizeRoutePattern(string routePattern)
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
