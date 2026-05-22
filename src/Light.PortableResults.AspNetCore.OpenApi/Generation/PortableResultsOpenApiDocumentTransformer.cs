using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;
using Light.PortableResults.Http;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Light.PortableResults.AspNetCore.OpenApi.Generation;

/// <summary>
/// OpenAPI document transformer for Light.PortableResults.
/// </summary>
public sealed class PortableResultsOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    private const string DefaultValidationExampleMessage = "Validation failed.";

    private readonly IErrorMetadataContractRegistry _errorMetadataContractRegistry;
    private readonly PortableResultsHttpWriteOptions _writeOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="PortableResultsOpenApiDocumentTransformer" />.
    /// </summary>
    public PortableResultsOpenApiDocumentTransformer(
        PortableResultsHttpWriteOptions writeOptions,
        IErrorMetadataContractRegistry errorMetadataContractRegistry
    )
    {
        _writeOptions = writeOptions ?? throw new ArgumentNullException(nameof(writeOptions));
        _errorMetadataContractRegistry = errorMetadataContractRegistry ??
                                         throw new ArgumentNullException(nameof(errorMetadataContractRegistry));
    }

    /// <inheritdoc />
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        // Install the library's fixed base schema catalog first, then add code-specific variants for
        // globally registered error contracts. InstallInto only adds the canonical shared shapes;
        // it does not know about application-defined error codes or their metadata CLR types.
        PortableResultsOpenApiSchemas.InstallInto(document);
        var openApiOptionsMonitor = context.ApplicationServices.GetRequiredService<IOptionsMonitor<OpenApiOptions>>();
        var openApiVersion = openApiOptionsMonitor.Get(context.DocumentName).OpenApiVersion;
        await EnsureGlobalErrorContractSchemasAsync(document, context, openApiVersion, cancellationToken);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        // document.Paths can be null if no paths are defined in the OpenAPI document
        if (document.Paths is null)
        {
            return;
        }

        foreach (var apiDescription in context.DescriptionGroups.SelectMany(static group => group.Items))
        {
            var attributes = apiDescription
               .ActionDescriptor
               .EndpointMetadata
               .OfType<PortableOpenApiResponseAttributeBase>()
               .ToArray();

            if (attributes.Length > 0 && TryGetOperation(document, apiDescription, out var operation))
            {
                await ApplyResponseMetadataAsync(
                    document,
                    context,
                    openApiVersion,
                    apiDescription,
                    operation,
                    attributes,
                    cancellationToken
                );
            }
        }
    }

    private async Task EnsureGlobalErrorContractSchemasAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        OpenApiSpecVersion openApiVersion,
        CancellationToken cancellationToken
    )
    {
        // Pre-register schema components for every globally configured error code so endpoint-level
        // documentation can later reference stable component ids instead of creating duplicates on demand.
        // Each contract produces two variants because regular problems use PortableError items while
        // ASP.NET-compatible validation problems use PortableValidationErrorDetail items.
        foreach (var (errorCode, contract) in _errorMetadataContractRegistry.Contracts)
        {
            var portableErrorSchemaId = PortableResultsOpenApiSchemaNaming.CreateGlobalErrorSchemaId(
                PortableResultsOpenApiSchemas.PortableErrorSchemaId,
                errorCode
            );
            await EnsureCodeSpecificSchemaAsync(
                document,
                context,
                PortableResultsOpenApiSchemas.PortableErrorSchemaId,
                portableErrorSchemaId,
                errorCode,
                contract,
                openApiVersion,
                cancellationToken
            );

            var validationErrorSchemaId = PortableResultsOpenApiSchemaNaming.CreateGlobalErrorSchemaId(
                PortableResultsOpenApiSchemas.PortableValidationErrorDetailSchemaId,
                errorCode
            );
            await EnsureCodeSpecificSchemaAsync(
                document,
                context,
                PortableResultsOpenApiSchemas.PortableValidationErrorDetailSchemaId,
                validationErrorSchemaId,
                errorCode,
                contract,
                openApiVersion,
                cancellationToken
            );
        }
    }

    private async Task ApplyResponseMetadataAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        OpenApiSpecVersion openApiVersion,
        ApiDescription apiDescription,
        OpenApiOperation operation,
        IReadOnlyList<PortableOpenApiResponseAttributeBase> attributes,
        CancellationToken cancellationToken
    )
    {
        var responseGroups = attributes.GroupBy(
            static attribute => new ResponseGroupKey(attribute.StatusCode, attribute.ContentType)
        );
        foreach (var responseGroup in responseGroups)
        {
            // A single HTTP response slot may combine different response kinds via anyOf, but it cannot
            // describe the same kind twice for the same status code and content type because the resulting
            // schema would be ambiguous and we would not know which marker should win.
            foreach (var duplicateGroup in responseGroup.GroupBy(static attribute => attribute.Kind))
            {
                if (duplicateGroup.Count() <= 1)
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"The OpenAPI response metadata for status code {responseGroup.Key.StatusCode} and content type '{responseGroup.Key.ContentType}' contains multiple markers of kind '{duplicateGroup.Key}'."
                );
            }

            // "Contributing" is local terminology here: each response attribute yields one candidate
            // schema for this response slot, and the final response schema is either that single schema
            // or an anyOf composed from all contributed candidates.
            var contributingSchemas = new List<IOpenApiSchema>(responseGroup.Count());
            foreach (var attribute in responseGroup)
            {
                var schema = await CreateContributingSchemaAsync(
                    document,
                    context,
                    openApiVersion,
                    apiDescription,
                    operation,
                    attribute,
                    cancellationToken
                );
                contributingSchemas.Add(schema);
            }

            var response = GetOrCreateResponse(operation, responseGroup.Key.StatusCode);
            var mediaType = new OpenApiMediaType
            {
                Schema = contributingSchemas.Count == 1 ?
                    contributingSchemas[0] :
                    new OpenApiSchema { AnyOf = contributingSchemas }
            };
            ApplyResponseExample(mediaType, responseGroup.ToArray(), responseGroup.Key.StatusCode);
            SetResponseContent(
                response,
                responseGroup.Key.ContentType,
                mediaType
            );
        }
    }

    private void ApplyResponseExample(
        OpenApiMediaType mediaType,
        IReadOnlyList<PortableOpenApiResponseAttributeBase> attributes,
        int statusCode
    )
    {
        // Every error marker that contributes to this slot may carry its own examples. The slot already
        // forbids two markers of the same kind (see ApplyResponseMetadataAsync), so each contributing kind
        // gets a distinct, stably named example instead of letting attribute ordering pick a single winner.
        Dictionary<string, IOpenApiExample>? examples = null;
        foreach (var attribute in attributes)
        {
            if (attribute is not PortableOpenApiErrorResponseAttributeBase { ErrorExamples.Length: > 0 } errorAttribute)
            {
                continue;
            }

            examples ??= new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal);
            var (exampleKey, summary) = ResolveExampleIdentity(errorAttribute);
            examples[exampleKey] = new OpenApiExample
            {
                Summary = summary,
                Value = CreateResponseExample(errorAttribute, statusCode)
            };
        }

        if (examples is not null)
        {
            mediaType.Examples = examples;
        }
    }

    private static (string Key, string Summary) ResolveExampleIdentity(
        PortableOpenApiErrorResponseAttributeBase attribute
    )
    {
        return attribute.Kind == PortableOpenApiResponseKind.ValidationProblem ?
            ("ValidationProblem", "Validation problem") :
            ("Problem", "Problem");
    }

    private JsonObject CreateResponseExample(
        PortableOpenApiErrorResponseAttributeBase attribute,
        int statusCode
    )
    {
        var category = ResolveExampleCategory(attribute, statusCode);
        var example = new JsonObject
        {
            ["type"] = category.GetTypeUri(),
            ["title"] = category.GetTitle(),
            ["status"] = statusCode,
            ["detail"] = category.GetDetail()
        };

        if (attribute is ProducesPortableValidationProblemAttribute validationAttribute &&
            ResolveValidationProblemFormat(validationAttribute) == ValidationProblemSerializationFormat.AspNetCoreCompatible)
        {
            AddAspNetCoreCompatibleValidationExample(example, validationAttribute.ErrorExamples!);
        }
        else
        {
            AddRichErrorExample(example, attribute.ErrorExamples!, category);
        }

        return example;
    }

    private static ErrorCategory ResolveExampleCategory(
        PortableOpenApiErrorResponseAttributeBase attribute,
        int statusCode
    )
    {
        // Validation problems are always documented with Bad Request semantics, while generic problems
        // derive their category from the documented status code so the example envelope is not mislabeled.
        if (attribute.Kind == PortableOpenApiResponseKind.ValidationProblem)
        {
            return ErrorCategory.Validation;
        }

        return Enum.IsDefined(typeof(ErrorCategory), statusCode) ?
            (ErrorCategory) statusCode :
            ErrorCategory.InternalError;
    }

    private ValidationProblemSerializationFormat ResolveValidationProblemFormat(
        ProducesPortableValidationProblemAttribute attribute
    )
    {
        return attribute.HasFormatOverride ?
            attribute.Format :
            _writeOptions.ValidationProblemSerializationFormat;
    }

    private static void AddRichErrorExample(
        JsonObject example,
        IReadOnlyList<PortableOpenApiErrorExampleEntry> entries,
        ErrorCategory category
    )
    {
        var errors = new JsonArray();
        foreach (var entry in entries)
        {
            var error = new JsonObject
            {
                ["message"] = entry.Message ?? DefaultValidationExampleMessage,
                ["code"] = entry.Code,
                ["target"] = entry.Target,
                ["category"] = category.ToString()
            };
            AddMetadata(error, entry.Metadata);
            errors.Add((JsonNode) error);
        }

        example["errors"] = errors;
    }

    private static void AddAspNetCoreCompatibleValidationExample(
        JsonObject example,
        IReadOnlyList<PortableOpenApiErrorExampleEntry> entries
    )
    {
        var groupedErrors = new Dictionary<string, JsonArray>(StringComparer.Ordinal);
        var nextIndexByTarget = new Dictionary<string, int>(StringComparer.Ordinal);
        var errorDetails = new JsonArray();

        foreach (var entry in entries)
        {
            var target = entry.Target ?? "";
            if (!groupedErrors.TryGetValue(target, out var messages))
            {
                messages = [];
                groupedErrors.Add(target, messages);
                nextIndexByTarget.Add(target, 0);
            }

            var index = nextIndexByTarget[target];
            nextIndexByTarget[target] = index + 1;
            messages.Add((JsonNode?) JsonValue.Create(entry.Message ?? DefaultValidationExampleMessage));

            var detail = new JsonObject
            {
                ["target"] = target,
                ["index"] = index,
                ["code"] = entry.Code,
                ["category"] = ErrorCategory.Validation.ToString()
            };
            AddMetadata(detail, entry.Metadata);
            errorDetails.Add((JsonNode) detail);
        }

        var errors = new JsonObject();
        foreach (var (target, messages) in groupedErrors)
        {
            errors[target] = messages;
        }

        example["errors"] = errors;
        example["errorDetails"] = errorDetails;
    }

    private static void AddMetadata(
        JsonObject error,
        IReadOnlyDictionary<string, object?>? metadata
    )
    {
        if (metadata is null || metadata.Count == 0)
        {
            return;
        }

        var metadataObject = new JsonObject();
        foreach (var (key, value) in metadata)
        {
            metadataObject[key] = CreateJsonValue(value);
        }

        error["metadata"] = metadataObject;
    }

    private static JsonNode? CreateJsonValue(object? value)
    {
        return value switch
        {
            null => null,
            string stringValue => JsonValue.Create(stringValue),
            bool boolValue => JsonValue.Create(boolValue),
            byte byteValue => JsonValue.Create(byteValue),
            sbyte sbyteValue => JsonValue.Create(sbyteValue),
            short shortValue => JsonValue.Create(shortValue),
            ushort ushortValue => JsonValue.Create(ushortValue),
            int intValue => JsonValue.Create(intValue),
            uint uintValue => JsonValue.Create(uintValue),
            long longValue => JsonValue.Create(longValue),
            ulong ulongValue => JsonValue.Create(ulongValue),
            float floatValue => JsonValue.Create(floatValue),
            double doubleValue => JsonValue.Create(doubleValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            DateTime dateTimeValue => JsonValue.Create(dateTimeValue),
            DateTimeOffset dateTimeOffsetValue => JsonValue.Create(dateTimeOffsetValue),
            Guid guidValue => JsonValue.Create(guidValue),
            Enum enumValue => JsonValue.Create(Convert.ToInt64(enumValue, CultureInfo.InvariantCulture)),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private async Task<IOpenApiSchema> CreateContributingSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        OpenApiSpecVersion openApiVersion,
        ApiDescription apiDescription,
        OpenApiOperation operation,
        PortableOpenApiResponseAttributeBase attribute,
        CancellationToken cancellationToken
    )
    {
        // By this point the transformer only cares about the discovered response metadata kind. The
        // attribute may have originated from an MVC action attribute or from a Minimal API helper that
        // attached the same attribute via EndpointMetadata, but both flow through the same schema builder.
        return attribute switch
        {
            PortableOpenApiSuccessResponseAttributeBase successAttribute =>
                await CreateSuccessResponseSchemaAsync(
                    document,
                    context,
                    apiDescription,
                    operation,
                    successAttribute,
                    cancellationToken
                ),
            PortableOpenApiErrorResponseAttributeBase errorAttribute =>
                await CreateErrorResponseSchemaAsync(
                    document,
                    context,
                    openApiVersion,
                    apiDescription,
                    operation,
                    errorAttribute,
                    cancellationToken
                ),
            _ => throw new InvalidOperationException(
                $"The response attribute '{attribute.GetType().FullName}' does not expose supported OpenAPI response metadata."
            )
        };
    }

    private async Task<IOpenApiSchema> CreateSuccessResponseSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        ApiDescription apiDescription,
        OpenApiOperation operation,
        PortableOpenApiSuccessResponseAttributeBase attribute,
        CancellationToken cancellationToken
    )
    {
        var metadataSerializationMode = attribute.HasMetadataSerializationModeOverride ?
            attribute.MetadataSerializationMode :
            _writeOptions.MetadataSerializationMode;
        if (attribute.TopLevelMetadataType is not null &&
            metadataSerializationMode == MetadataSerializationMode.ErrorsOnly)
        {
            throw new InvalidOperationException(
                "Top-level success metadata cannot be documented when MetadataSerializationMode is ErrorsOnly because metadata is not part of the wire format."
            );
        }

        var valueSchema = await context.GetOrCreateSchemaAsync(
            attribute.ValueType,
            parameterDescription: null,
            cancellationToken
        );

        if (metadataSerializationMode == MetadataSerializationMode.ErrorsOnly &&
            attribute.TopLevelMetadataType is null)
        {
            return valueSchema;
        }

        var envelopeSchemaId = PortableResultsOpenApiSchemaNaming.CreateDerivedEnvelopeSchemaId(
            "PortableSuccessResponse",
            operation,
            apiDescription,
            attribute.StatusCode,
            attribute.ContentType
        );
        IOpenApiSchema metadataSchema = attribute.TopLevelMetadataType is null ?
            PortableResultsOpenApiSchemas.CreateOpenMetadataSchema() :
            await GetStableSchemaReferenceAsync(
                document,
                context,
                attribute.TopLevelMetadataType,
                PortableResultsOpenApiSchemaNaming.CreateMetadataSchemaId(envelopeSchemaId),
                cancellationToken
            );

        var envelopeSchema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
            {
                ["value"] = valueSchema,
                ["metadata"] = metadataSchema
            },
            Required = new HashSet<string>(StringComparer.Ordinal) { "value" }
        };
        return AddComponentAndCreateReference(document, envelopeSchemaId, envelopeSchema);
    }

    private async Task<IOpenApiSchema> CreateErrorResponseSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        OpenApiSpecVersion openApiVersion,
        ApiDescription apiDescription,
        OpenApiOperation operation,
        PortableOpenApiErrorResponseAttributeBase attribute,
        CancellationToken cancellationToken
    )
    {
        ValidateInlineMetadataArrays(attribute);

        var canonicalSchemaId = ResolveCanonicalErrorEnvelopeSchemaId(attribute);
        var itemBaseSchemaId = ResolveErrorItemSchemaId(canonicalSchemaId);
        var documentedErrorSchema = await CreateDocumentedErrorItemSchemaAsync(
            document,
            context,
            openApiVersion,
            apiDescription,
            operation,
            attribute,
            itemBaseSchemaId,
            cancellationToken
        );

        if (attribute.TopLevelMetadataType is null && documentedErrorSchema is null)
        {
            return PortableResultsOpenApiSchemas.CreateSchemaReference(document, canonicalSchemaId);
        }

        var derivedEnvelopeSchemaId = PortableResultsOpenApiSchemaNaming.CreateDerivedEnvelopeSchemaId(
            canonicalSchemaId,
            operation,
            apiDescription,
            attribute.StatusCode,
            attribute.ContentType
        );

        var properties = CreateCanonicalErrorEnvelopeProperties(document, canonicalSchemaId);

        if (attribute.TopLevelMetadataType is not null)
        {
            properties["metadata"] = await GetStableSchemaReferenceAsync(
                document,
                context,
                attribute.TopLevelMetadataType,
                PortableResultsOpenApiSchemaNaming.CreateMetadataSchemaId(derivedEnvelopeSchemaId),
                cancellationToken
            );
        }

        if (documentedErrorSchema is not null)
        {
            properties[ResolveDocumentedErrorPropertyName(canonicalSchemaId)] = new OpenApiSchema
            {
                Type = JsonSchemaType.Array,
                Items = documentedErrorSchema
            };
        }

        var derivedSchema = new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Properties = properties,
            Required = CreateCanonicalErrorEnvelopeRequired(canonicalSchemaId)
        };
        return AddComponentAndCreateReference(document, derivedEnvelopeSchemaId, derivedSchema);
    }

    private async Task<IOpenApiSchema?> CreateDocumentedErrorItemSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        OpenApiSpecVersion openApiVersion,
        ApiDescription apiDescription,
        OpenApiOperation operation,
        PortableOpenApiErrorResponseAttributeBase attribute,
        string itemBaseSchemaId,
        CancellationToken cancellationToken
    )
    {
        // Collect the narrowed variants we can document for this error item schema, while tracking the
        // raw error codes we have already seen so we can reject conflicting metadata contracts.
        var documentedVariants = new List<DocumentedErrorVariant>();
        var rawCodeContracts = new Dictionary<string, ErrorMetadataContract>(StringComparer.Ordinal);
        var inlineSanitizedCodes = new Dictionary<string, string>(StringComparer.Ordinal);

        // Global error codes reuse pre-registered component schemas created from the application's
        // error-contract registry, so here we only validate the code and reference the stable component id.
        foreach (var code in attribute.ErrorCodes ?? [])
        {
            if (!_errorMetadataContractRegistry.Contracts.TryGetValue(code, out var contract))
            {
                throw new InvalidOperationException(PortableResultsOpenApiMessages.CreateUnknownErrorCodeMessage(code));
            }

            AddDocumentedCode(rawCodeContracts, code, contract);
            var schemaId = PortableResultsOpenApiSchemaNaming.CreateGlobalErrorSchemaId(itemBaseSchemaId, code);
            documentedVariants.Add(
                new DocumentedErrorVariant(
                    code,
                    PortableResultsOpenApiSchemas.CreateSchemaReference(document, schemaId)
                )
            );
        }

        var inlineCodes = attribute.InlineErrorMetadataCodes;
        var inlineContracts = attribute.InlineErrorMetadataContracts;
        if (inlineCodes is not null && inlineContracts is not null)
        {
            // Inline contracts are declared directly on the endpoint, so this branch has to create
            // endpoint-specific schemas and guard against both raw-code duplicates and sanitized-name
            // collisions that would otherwise map different codes onto the same component schema id.
            for (var i = 0; i < inlineCodes.Length; i++)
            {
                var code = inlineCodes[i];
                var contract = inlineContracts[i];
                var sanitizedCode = PortableResultsOpenApiSchemaNaming.SanitizeErrorCode(code);
                if (inlineSanitizedCodes.TryGetValue(sanitizedCode, out var existingCode) &&
                    !string.Equals(existingCode, code, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        PortableResultsOpenApiMessages.CreateSanitizedErrorCodeCollisionMessage(
                            existingCode,
                            code,
                            sanitizedCode
                        )
                    );
                }

                if (rawCodeContracts.TryGetValue(code, out var existingContract))
                {
                    if (!existingContract.Equals(contract))
                    {
                        throw new InvalidOperationException(
                            PortableResultsOpenApiMessages.CreateDuplicateErrorMetadataContractMessage(
                                code,
                                existingContract,
                                contract
                            )
                        );
                    }

                    continue;
                }

                rawCodeContracts.Add(code, contract);
                inlineSanitizedCodes.TryAdd(sanitizedCode, code);
                var schemaId = PortableResultsOpenApiSchemaNaming.CreateInlineErrorSchemaId(
                    itemBaseSchemaId,
                    operation,
                    apiDescription,
                    attribute.StatusCode,
                    attribute.ContentType,
                    code
                );
                var schemaReference = await EnsureCodeSpecificSchemaAsync(
                    document,
                    context,
                    itemBaseSchemaId,
                    schemaId,
                    code,
                    contract,
                    openApiVersion,
                    cancellationToken
                );
                documentedVariants.Add(new DocumentedErrorVariant(code, schemaReference));
            }
        }

        if (documentedVariants.Count == 0)
        {
            return null;
        }

        var discriminatorMapping = documentedVariants.ToDictionary(
            static variant => variant.RawCode,
            static variant => variant.SchemaReference,
            StringComparer.Ordinal
        );

        if (attribute.AllowUnknownErrorCodes)
        {
            var fallbackSchema = PortableResultsOpenApiSchemas.CreateSchemaReference(document, itemBaseSchemaId);
            var anyOfSchemas = new List<IOpenApiSchema>(documentedVariants.Count + 1);
            anyOfSchemas.AddRange(documentedVariants.Select(static variant => (IOpenApiSchema) variant.SchemaReference));
            anyOfSchemas.Add(fallbackSchema);

            return new OpenApiSchema
            {
                AnyOf = anyOfSchemas,
                Required = new HashSet<string>(StringComparer.Ordinal) { "code" },
                Discriminator = new OpenApiDiscriminator
                {
                    PropertyName = "code",
                    Mapping = discriminatorMapping
                }
            };
        }

        return new OpenApiSchema
        {
            OneOf = documentedVariants.Select(static variant => (IOpenApiSchema) variant.SchemaReference).ToList(),
            Required = new HashSet<string>(StringComparer.Ordinal) { "code" },
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "code",
                Mapping = discriminatorMapping
            }
        };
    }

    private async Task<OpenApiSchemaReference> EnsureCodeSpecificSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        string baseSchemaId,
        string schemaId,
        string errorCode,
        ErrorMetadataContract contract,
        OpenApiSpecVersion openApiVersion,
        CancellationToken cancellationToken
    )
    {
        var schemas = EnsureSchemaStore(document);
        if (!schemas.ContainsKey(schemaId))
        {
            var metadataSchema = await CreateMetadataSchemaAsync(
                document,
                context,
                schemaId,
                contract,
                openApiVersion,
                cancellationToken
            );
            var schema = CreateCodeSpecificSchema(
                document,
                baseSchemaId,
                errorCode,
                metadataSchema,
                openApiVersion
            );
            schemas.Add(schemaId, schema);
        }

        return PortableResultsOpenApiSchemas.CreateSchemaReference(document, schemaId);
    }

    private static OpenApiSchema CreateCodeSpecificSchema(
        OpenApiDocument document,
        string baseSchemaId,
        string errorCode,
        IOpenApiSchema? metadataSchema,
        OpenApiSpecVersion openApiVersion
    )
    {
        var codeSchema = openApiVersion >= OpenApiSpecVersion.OpenApi3_1 ?
            new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Const = errorCode
            } :
            new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = [JsonValue.Create(errorCode)]
            };

        var extensionProperties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
        {
            ["code"] = codeSchema
        };
        if (metadataSchema is not null)
        {
            extensionProperties["metadata"] = metadataSchema;
        }

        return new OpenApiSchema
        {
            AllOf =
            [
                PortableResultsOpenApiSchemas.CreateSchemaReference(document, baseSchemaId),
                new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = extensionProperties,
                    Required = new HashSet<string>(StringComparer.Ordinal) { "code" }
                }
            ]
        };
    }

    private async Task<IOpenApiSchema?> CreateMetadataSchemaAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        string ownerSchemaId,
        ErrorMetadataContract contract,
        OpenApiSpecVersion openApiVersion,
        CancellationToken cancellationToken
    )
    {
        var metadataSchemaId = PortableResultsOpenApiSchemaNaming.CreateMetadataSchemaId(ownerSchemaId);
        return contract switch
        {
            ErrorMetadataTypeContract typeContract => await GetStableSchemaReferenceAsync(
                document,
                context,
                typeContract.MetadataType,
                metadataSchemaId,
                cancellationToken
            ),
            ErrorMetadataSchemaContract schemaContract => AddComponentAndCreateReference(
                document,
                metadataSchemaId,
                schemaContract.SchemaFactory(openApiVersion)
            ),
            NoMetadataContract => null,
            _ => throw new InvalidOperationException(
                $"The error metadata contract '{contract.GetType().FullName}' is not supported."
            )
        };
    }

    private async Task<OpenApiSchemaReference> GetStableSchemaReferenceAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        Type metadataType,
        string schemaId,
        CancellationToken cancellationToken
    )
    {
        var schema = await context.GetOrCreateSchemaAsync(metadataType, parameterDescription: null, cancellationToken);
        return AddComponentAndCreateReference(document, schemaId, schema);
    }

    private static OpenApiSchemaReference AddComponentAndCreateReference(
        OpenApiDocument document,
        string schemaId,
        OpenApiSchema schema
    )
    {
        var schemas = EnsureSchemaStore(document);
        schemas.TryAdd(schemaId, schema);

        return PortableResultsOpenApiSchemas.CreateSchemaReference(document, schemaId);
    }

    private static IDictionary<string, IOpenApiSchema> EnsureSchemaStore(OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        return document.Components.Schemas;
    }

    private static bool TryGetOperation(
        OpenApiDocument document,
        ApiDescription apiDescription,
        [NotNullWhen(true)] out OpenApiOperation? operation
    )
    {
        operation = null;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (document.Paths is null)
        {
            return false;
        }

        var path = NormalizePath(apiDescription.RelativePath);
        if (path is null || !document.Paths.TryGetValue(path, out var pathItem))
        {
            return false;
        }

        var httpMethod = string.IsNullOrWhiteSpace(apiDescription.HttpMethod) ?
            null :
            HttpMethod.Parse(apiDescription.HttpMethod);

        if (httpMethod is null ||
            pathItem.Operations is null ||
            !pathItem.Operations.TryGetValue(httpMethod, out var resolvedOperation))
        {
            return false;
        }

        operation = resolvedOperation;
        return true;
    }

    private static OpenApiResponse GetOrCreateResponse(OpenApiOperation operation, int statusCode)
    {
        operation.Responses ??= new OpenApiResponses();
        var responseKey = statusCode.ToString(CultureInfo.InvariantCulture);
        if (!operation.Responses.TryGetValue(responseKey, out var response))
        {
            var createdResponse = new OpenApiResponse
            {
                Description = $"HTTP {statusCode}"
            };
            operation.Responses.Add(responseKey, createdResponse);
            return createdResponse;
        }

        if (response is OpenApiResponse concreteResponse)
        {
            return concreteResponse;
        }

        if (response is not OpenApiResponseReference responseReference)
        {
            throw new InvalidOperationException(
                $"The OpenAPI response entry for status code {statusCode} must be either an OpenApiResponse or an OpenApiResponseReference, but was '{response.GetType().FullName}'."
            );
        }

        var materializedResponse = new OpenApiResponse
        {
            Description = string.IsNullOrWhiteSpace(responseReference.Description) ?
                $"HTTP {statusCode}" :
                responseReference.Description,
            Content = responseReference.Content,
            Headers = responseReference.Headers,
            Links = responseReference.Links,
            Extensions = responseReference.Extensions
        };

        operation.Responses[responseKey] = materializedResponse;
        return materializedResponse;
    }

    private static void SetResponseContent(
        OpenApiResponse response,
        string contentType,
        OpenApiMediaType mediaType
    )
    {
        if (response.Content is null)
        {
            response.Content = new Dictionary<string, OpenApiMediaType>(StringComparer.OrdinalIgnoreCase)
            {
                [contentType] = mediaType
            };
            return;
        }

        var existingContentType = FindExistingContentType(response.Content, contentType);
        response.Content[existingContentType ?? contentType] = mediaType;
    }

    private static string? FindExistingContentType(
        IDictionary<string, OpenApiMediaType> content,
        string contentType
    )
    {
        foreach (var existingContentType in content.Keys)
        {
            if (string.Equals(existingContentType, contentType, StringComparison.OrdinalIgnoreCase))
            {
                return existingContentType;
            }
        }

        return null;
    }

    private string ResolveCanonicalErrorEnvelopeSchemaId(PortableOpenApiResponseAttributeBase attribute)
    {
        if (attribute.Kind == PortableOpenApiResponseKind.Problem)
        {
            return PortableResultsOpenApiSchemas.PortableProblemDetailsSchemaId;
        }

        var validationAttribute = (ProducesPortableValidationProblemAttribute) attribute;
        var format = validationAttribute.HasFormatOverride ?
            validationAttribute.Format :
            _writeOptions.ValidationProblemSerializationFormat;
        return format == ValidationProblemSerializationFormat.AspNetCoreCompatible ?
            PortableResultsOpenApiSchemas.PortableAspNetCoreValidationProblemDetailsSchemaId :
            PortableResultsOpenApiSchemas.PortableRichValidationProblemDetailsSchemaId;
    }

    private static string ResolveErrorItemSchemaId(string canonicalEnvelopeSchemaId)
    {
        return canonicalEnvelopeSchemaId ==
               PortableResultsOpenApiSchemas.PortableAspNetCoreValidationProblemDetailsSchemaId ?
            PortableResultsOpenApiSchemas.PortableValidationErrorDetailSchemaId :
            PortableResultsOpenApiSchemas.PortableErrorSchemaId;
    }

    private static void ValidateInlineMetadataArrays(PortableOpenApiErrorResponseAttributeBase attribute)
    {
        if (attribute.InlineErrorMetadataCodes is null && attribute.InlineErrorMetadataContracts is null)
        {
            return;
        }

        if (attribute.InlineErrorMetadataCodes is null || attribute.InlineErrorMetadataContracts is null)
        {
            throw new InvalidOperationException(
                PortableResultsOpenApiMessages.CreateIncompleteInlineErrorMetadataMessage()
            );
        }

        if (attribute.InlineErrorMetadataCodes.Length == attribute.InlineErrorMetadataContracts.Length)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Inline error metadata arrays must have the same length, but codes has length {attribute.InlineErrorMetadataCodes.Length} and contracts has length {attribute.InlineErrorMetadataContracts.Length}."
        );
    }

    private static void AddDocumentedCode(
        IDictionary<string, ErrorMetadataContract> rawCodeContracts,
        string code,
        ErrorMetadataContract contract
    )
    {
        if (rawCodeContracts.TryGetValue(code, out var existingContract))
        {
            if (existingContract.Equals(contract))
            {
                return;
            }

            throw new InvalidOperationException(
                PortableResultsOpenApiMessages.CreateDuplicateErrorMetadataContractMessage(
                    code,
                    existingContract,
                    contract
                )
            );
        }

        rawCodeContracts.Add(code, contract);
    }

    private static Dictionary<string, IOpenApiSchema> CreateCanonicalErrorEnvelopeProperties(
        OpenApiDocument document,
        string canonicalSchemaId
    )
    {
        return canonicalSchemaId switch
        {
            PortableResultsOpenApiSchemas.PortableProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableProblemDetailsProperties(document),
            PortableResultsOpenApiSchemas.PortableRichValidationProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableProblemDetailsProperties(document),
            PortableResultsOpenApiSchemas.PortableAspNetCoreValidationProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableAspNetCoreValidationProblemDetailsProperties(document),
            _ => throw new InvalidOperationException(
                $"The canonical error envelope schema id '{canonicalSchemaId}' is not supported."
            )
        };
    }

    private static HashSet<string> CreateCanonicalErrorEnvelopeRequired(string canonicalSchemaId)
    {
        return canonicalSchemaId switch
        {
            PortableResultsOpenApiSchemas.PortableProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableProblemDetailsRequired(),
            PortableResultsOpenApiSchemas.PortableRichValidationProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableProblemDetailsRequired(),
            PortableResultsOpenApiSchemas.PortableAspNetCoreValidationProblemDetailsSchemaId =>
                PortableResultsOpenApiSchemas.CreatePortableAspNetCoreValidationProblemDetailsRequired(),
            _ => throw new InvalidOperationException(
                $"The canonical error envelope schema id '{canonicalSchemaId}' is not supported."
            )
        };
    }

    private static string ResolveDocumentedErrorPropertyName(string canonicalSchemaId)
    {
        return canonicalSchemaId == PortableResultsOpenApiSchemas.PortableAspNetCoreValidationProblemDetailsSchemaId ?
            "errorDetails" :
            "errors";
    }

    private static string? NormalizePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = NormalizeRouteSegment(segments[i]);
        }

        return "/" + string.Join("/", segments);
    }

    private static string NormalizeRouteSegment(string segment)
    {
        if (segment.Length < 2 || segment[0] != '{' || segment[^1] != '}')
        {
            return segment;
        }

        var content = segment[1..^1];
        var constraintSeparatorIndex = content.IndexOf(':');
        if (constraintSeparatorIndex < 0)
        {
            return segment;
        }

        return "{" + content[..constraintSeparatorIndex] + "}";
    }

    private readonly record struct ResponseGroupKey(int StatusCode, string ContentType)
    {
        public bool Equals(ResponseGroupKey other)
        {
            return StatusCode == other.StatusCode &&
                   string.Equals(ContentType, other.ContentType, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StatusCode,
                ContentType is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(ContentType)
            );
        }
    }

    private readonly record struct DocumentedErrorVariant(
        string RawCode,
        OpenApiSchemaReference SchemaReference
    );
}
