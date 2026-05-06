using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.Schemas;

/// <summary>
/// Installs the canonical Light.PortableResults OpenAPI schemas into a document.
/// </summary>
public static class PortableResultsOpenApiSchemas
{
    /// <summary>
    /// The component schema id for the canonical portable error item shape.
    /// </summary>
    public const string PortableErrorSchemaId = "PortableError";

    /// <summary>
    /// The component schema id for the canonical portable validation error detail shape.
    /// </summary>
    public const string PortableValidationErrorDetailSchemaId = "PortableValidationErrorDetail";

    /// <summary>
    /// The component schema id for the canonical portable problem details envelope.
    /// </summary>
    public const string PortableProblemDetailsSchemaId = "PortableProblemDetails";

    /// <summary>
    /// The component schema id for the rich validation-problem envelope.
    /// </summary>
    public const string PortableRichValidationProblemDetailsSchemaId = "PortableRichValidationProblemDetails";


    /// <summary>
    /// The component schema id for the ASP.NET Core-compatible validation-problem envelope.
    /// </summary>
    public const string PortableAspNetCoreValidationProblemDetailsSchemaId =
        "PortableAspNetCoreValidationProblemDetails";

    /// <summary>
    /// The component schema id for the <see cref="ErrorCategory" /> enum values.
    /// </summary>
    public const string ErrorCategorySchemaId = "ErrorCategory";

    /// <summary>
    /// Installs the canonical Light.PortableResults schema catalog into the specified document.
    /// </summary>
    public static void InstallInto(OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var schemas = EnsureSchemaStore(document);
        AddIfMissing(schemas, ErrorCategorySchemaId, CreateErrorCategorySchema());
        AddIfMissing(schemas, PortableErrorSchemaId, CreatePortableErrorSchema(document));
        AddIfMissing(
            schemas,
            PortableValidationErrorDetailSchemaId,
            CreatePortableValidationErrorDetailSchema(document)
        );
        AddIfMissing(schemas, PortableProblemDetailsSchemaId, CreatePortableProblemDetailsSchema(document));
        AddIfMissing(
            schemas,
            PortableRichValidationProblemDetailsSchemaId,
            CreatePortableRichValidationProblemDetailsSchema(document)
        );
        AddIfMissing(
            schemas,
            PortableAspNetCoreValidationProblemDetailsSchemaId,
            CreatePortableAspNetCoreValidationProblemDetailsSchema(document)
        );
    }

    /// <summary>
    /// Creates an open-ended metadata schema that allows arbitrary object properties.
    /// </summary>
    /// <returns>The metadata schema.</returns>
    public static OpenApiSchema CreateOpenMetadataSchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object | JsonSchemaType.Null,
            AdditionalPropertiesAllowed = true
        };
    }

    /// <summary>
    /// Creates a reference to a schema component in the specified OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document that owns the schema component.</param>
    /// <param name="schemaId">The component schema id to reference.</param>
    /// <returns>The schema reference.</returns>
    public static OpenApiSchemaReference CreateSchemaReference(OpenApiDocument document, string schemaId)
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
        IDictionary<string, IOpenApiSchema> schemas,
        string schemaId,
        OpenApiSchema schema
    )
    {
        schemas.TryAdd(schemaId, schema);
    }

    private static OpenApiSchema CreateErrorCategorySchema()
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = Enum.GetNames(typeof(ErrorCategory))
               .Select(static name => (JsonNode) JsonValue.Create(name))
               .ToList(),
            Examples = [JsonValue.Create(ErrorCategory.Validation.ToString())]
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
            Properties = CreatePortableProblemDetailsProperties(document),
            Required = CreatePortableProblemDetailsRequired()
        };
    }

    private static OpenApiSchema CreatePortableRichValidationProblemDetailsSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = CreatePortableProblemDetailsProperties(document),
            Required = CreatePortableProblemDetailsRequired()
        };
    }

    private static OpenApiSchema CreatePortableAspNetCoreValidationProblemDetailsSchema(OpenApiDocument document)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = CreatePortableAspNetCoreValidationProblemDetailsProperties(document),
            Required = CreatePortableAspNetCoreValidationProblemDetailsRequired()
        };
    }

    /// <summary>
    /// Creates a fresh property dictionary for the portable problem-details envelopes.
    /// </summary>
    public static Dictionary<string, IOpenApiSchema> CreatePortableProblemDetailsProperties(OpenApiDocument document)
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

    /// <summary>
    /// Creates a fresh required-set for the portable problem-details envelopes.
    /// </summary>
    public static HashSet<string> CreatePortableProblemDetailsRequired()
    {
        return new HashSet<string>(StringComparer.Ordinal) { "type", "title", "status", "errors" };
    }

    /// <summary>
    /// Creates a fresh property dictionary for the ASP.NET Core-compatible validation problem envelope.
    /// </summary>
    public static Dictionary<string, IOpenApiSchema> CreatePortableAspNetCoreValidationProblemDetailsProperties(
        OpenApiDocument document
    )
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
        };
    }

    /// <summary>
    /// Creates a fresh required-set for the ASP.NET Core-compatible validation problem envelope.
    /// </summary>
    public static HashSet<string> CreatePortableAspNetCoreValidationProblemDetailsRequired()
    {
        return new HashSet<string>(StringComparer.Ordinal) { "type", "title", "status", "errors" };
    }
}
