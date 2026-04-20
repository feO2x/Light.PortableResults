using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Installs the canonical Light.PortableResults OpenAPI schemas into a document.
/// </summary>
public static class PortableResultsOpenApiSchemas
{
    internal const string PortableErrorSchemaId = "PortableError";
    internal const string PortableValidationErrorDetailSchemaId = "PortableValidationErrorDetail";
    internal const string PortableProblemDetailsSchemaId = "PortableProblemDetails";
    internal const string PortableRichValidationProblemDetailsSchemaId = "PortableRichValidationProblemDetails";
    internal const string PortableAspNetCoreValidationProblemDetailsSchemaId =
        "PortableAspNetCoreValidationProblemDetails";
    internal const string ErrorCategorySchemaId = "ErrorCategory";

    /// <summary>
    /// Installs the canonical Light.PortableResults schema catalog into the specified document.
    /// </summary>
    public static void InstallInto(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var schemas = EnsureSchemaStore(document);
        AddIfMissing(document, schemas, ErrorCategorySchemaId, CreateErrorCategorySchema());
        AddIfMissing(document, schemas, PortableErrorSchemaId, CreatePortableErrorSchema(document));
        AddIfMissing(
            document,
            schemas,
            PortableValidationErrorDetailSchemaId,
            CreatePortableValidationErrorDetailSchema(document)
        );
        AddIfMissing(document, schemas, PortableProblemDetailsSchemaId, CreatePortableProblemDetailsSchema(document));
        AddIfMissing(
            document,
            schemas,
            PortableRichValidationProblemDetailsSchemaId,
            CreatePortableRichValidationProblemDetailsSchema(document)
        );
        AddIfMissing(
            document,
            schemas,
            PortableAspNetCoreValidationProblemDetailsSchemaId,
            CreatePortableAspNetCoreValidationProblemDetailsSchema(document)
        );
    }

    internal static OpenApiSchema CreateOpenMetadataSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            AdditionalPropertiesAllowed = true
        };
    }

    internal static OpenApiSchemaReference CreateSchemaReference(OpenApiDocument document, string schemaId)
    {
        return new OpenApiSchemaReference(schemaId, document, externalResource: null);
    }

    private static IDictionary<string, IOpenApiSchema> EnsureSchemaStore(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        return document.Components.Schemas;
    }

    private static void AddIfMissing(
        OpenApiDocument document,
        IDictionary<string, IOpenApiSchema> schemas,
        string schemaId,
        OpenApiSchema schema
    )
    {
        if (!schemas.ContainsKey(schemaId))
        {
            schemas.Add(schemaId, schema);
        }
    }

    private static OpenApiSchema CreateErrorCategorySchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = Enum.GetNames(typeof(ErrorCategory))
                       .Select(static name => (JsonNode) JsonValue.Create(name)!)
                       .ToList()
        };
    }

    private static OpenApiSchema CreatePortableErrorSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["message"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["code"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["target"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["category"] = CreateSchemaReference(document, ErrorCategorySchemaId),
                ["metadata"] = CreateOpenMetadataSchema()
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "message" }
        };
    }

    private static OpenApiSchema CreatePortableValidationErrorDetailSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["target"] = new OpenApiSchema { Type = JsonSchemaType.String },
                ["index"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
                ["code"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["category"] = CreateSchemaReference(document, ErrorCategorySchemaId),
                ["metadata"] = CreateOpenMetadataSchema()
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "target", "index" }
        };
    }

    private static OpenApiSchema CreatePortableProblemDetailsSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = CreateProblemDetailsProperties(document),
            Required = new HashSet<string>(StringComparer.Ordinal) { "type", "title", "status", "errors" }
        };
    }

    private static OpenApiSchema CreatePortableRichValidationProblemDetailsSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = CreateProblemDetailsProperties(document),
            Required = new HashSet<string>(StringComparer.Ordinal) { "type", "title", "status", "errors" }
        };
    }

    private static OpenApiSchema CreatePortableAspNetCoreValidationProblemDetailsSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["type"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
                ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
                ["errors"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    AdditionalPropertiesAllowed = true,
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Array,
                        Items = new OpenApiSchema { Type = JsonSchemaType.String }
                    }
                },
                ["errorDetails"] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = CreateSchemaReference(document, PortableValidationErrorDetailSchemaId)
                },
                ["metadata"] = CreateOpenMetadataSchema()
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "type", "title", "status", "errors" }
        };
    }

    private static Dictionary<string, IOpenApiSchema> CreateProblemDetailsProperties(OpenApiDocument document)
    {
        return new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
        {
            ["type"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["title"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["status"] = new OpenApiSchema { Type = JsonSchemaType.Integer },
            ["detail"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["instance"] = new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null },
            ["errors"] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = CreateSchemaReference(document, PortableErrorSchemaId)
            },
            ["metadata"] = CreateOpenMetadataSchema()
        };
    }
}
