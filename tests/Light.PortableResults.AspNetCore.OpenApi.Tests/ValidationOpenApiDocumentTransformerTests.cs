using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.AspNetCore.OpenApi.Schemas;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class ValidationOpenApiDocumentTransformerTests
{
    public static TheoryData<string, string, string[]> TypedHelperCases =>
        new ()
        {
            { "EqualTo", ValidationErrorCodes.EqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { "NotEqualTo", ValidationErrorCodes.NotEqualTo, [ValidationErrorMetadataKeys.ComparativeValue] },
            { "GreaterThan", ValidationErrorCodes.GreaterThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                "GreaterThanOrEqualTo",
                ValidationErrorCodes.GreaterThanOrEqualTo,
                [ValidationErrorMetadataKeys.ComparativeValue]
            },
            { "LessThan", ValidationErrorCodes.LessThan, [ValidationErrorMetadataKeys.ComparativeValue] },
            {
                "LessThanOrEqualTo",
                ValidationErrorCodes.LessThanOrEqualTo,
                [ValidationErrorMetadataKeys.ComparativeValue]
            },
            {
                "InRange",
                ValidationErrorCodes.InRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                "NotInRange",
                ValidationErrorCodes.NotInRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            },
            {
                "ExclusiveRange",
                ValidationErrorCodes.ExclusiveRange,
                [ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary]
            }
        };

    [Fact]
    public async Task Transformer_ShouldEmitCodeOnlyExtensionForNoMetadataCodes()
    {
        await using var app = CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/no-metadata", static () => Results.Problem())
                   .WithName("NoMetadata")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x.WithErrorCodes(ValidationErrorCodes.NotNull)
                    );
            }
        );

        var document = await GetOpenApiDocumentAsync(app);
        var component = GetSchemaComponent(document, "PortableError__NotNull");
        var extension = (OpenApiSchema) component.AllOf![1];

        extension.Properties!.Keys.Should().BeEquivalentTo("code");
        extension.Properties.Should().NotContainKey("metadata");
        extension.Required.Should().BeEquivalentTo("code");
    }

    [Fact]
    public async Task Transformer_ShouldEmitMetadataAndNoMetadataBuiltInCodesTogether()
    {
        await using var app = CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/count-and-not-null", static () => Results.Problem())
                   .WithName("CountAndNotNull")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x.WithErrorCodes(ValidationErrorCodes.Count, ValidationErrorCodes.NotNull)
                    );
            }
        );

        var document = await GetOpenApiDocumentAsync(app);
        var responseItems = GetProblemItems(document, "/count-and-not-null");

        responseItems.AnyOf!.Select(static schema => GetSchemaReferenceId((OpenApiSchemaReference) schema))
           .Should()
           .Contain(["PortableError__Count", "PortableError__NotNull", "PortableError"]);
        var countMetadata = GetSchemaComponent(document, "PortableError__Count__Metadata");
        countMetadata.Properties!.Keys.Should().BeEquivalentTo(ValidationErrorMetadataKeys.ExpectedCount);
        ((OpenApiSchema) countMetadata.Properties[ValidationErrorMetadataKeys.ExpectedCount]).Type.Should()
           .Be(JsonSchemaType.Integer);
    }

    [Fact]
    public async Task Transformer_ShouldProduceValidOpenApi30DocumentWithBuiltInCatalog()
    {
        await using var app = CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/catalog", static () => Results.Problem())
                   .WithName("Catalog")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x.WithErrorCodes(BuiltInValidationErrorContracts.Contracts.Keys.ToArray())
                    );
            },
            options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0
        );

        var document = await GetOpenApiDocumentAsync(app);
        var validationErrors = document.Validate(ValidationRuleSet.GetDefaultRuleSet());
        await using var stream = new MemoryStream();
        await document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_0, TestContext.Current.CancellationToken);
        var json = Encoding.UTF8.GetString(stream.ToArray());

        validationErrors.Should().BeEmpty();
        json.Should().NotContain("\"type\":\"null\"");
    }

    [Fact]
    public async Task Transformer_ShouldKeepTypeAndSchemaContractEnvelopeShapeEquivalent()
    {
        await using var typeApp = CreateApp(
            contracts => contracts.ForCode<EquivalentMetadata>("Equivalent"),
            endpoints =>
            {
                endpoints
                   .MapGet("/equivalent", static () => Results.Problem())
                   .WithName("EquivalentType")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x.WithErrorCodes("Equivalent")
                    );
            }
        );
        await using var schemaApp = CreateApp(
            contracts => contracts.ForCode(
                "Equivalent",
                _ => new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal)
                    {
                        ["value"] = new OpenApiSchema { Type = JsonSchemaType.Integer }
                    },
                    Required = new HashSet<string>(StringComparer.Ordinal) { "value" }
                }
            ),
            endpoints =>
            {
                endpoints
                   .MapGet("/equivalent", static () => Results.Problem())
                   .WithName("EquivalentSchema")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x.WithErrorCodes("Equivalent")
                    );
            }
        );

        var typeExtension = GetCodeSpecificExtension(await GetOpenApiDocumentAsync(typeApp), "PortableError__Equivalent");
        var schemaExtension =
            GetCodeSpecificExtension(await GetOpenApiDocumentAsync(schemaApp), "PortableError__Equivalent");

        typeExtension.Properties!.Keys.Should().BeEquivalentTo(schemaExtension.Properties!.Keys);
        typeExtension.Required.Should().BeEquivalentTo(schemaExtension.Required);
        typeExtension.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>();
        schemaExtension.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>();
    }

    [Theory]
    [MemberData(nameof(TypedHelperCases))]
    public async Task TypedHelpers_ShouldEmitEndpointScopedIntegerMetadata(
        string operationName,
        string code,
        string[] properties
    )
    {
        await using var app = CreateTypedHelperApp<int>(operationName);

        var document = await GetOpenApiDocumentAsync(app);
        var metadata = GetSchemaComponent(
            document,
            $"PortableError__{operationName}__400__application_problem_json__{code}__Metadata"
        );

        metadata.Properties!.Keys.Should().BeEquivalentTo(properties);
        metadata.Required.Should().BeEquivalentTo(properties);
        properties.Should().OnlyContain(
            property => SchemaIncludesType((OpenApiSchema) metadata.Properties[property], JsonSchemaType.Integer)
        );
    }

    [Fact]
    public async Task TypedHelpers_ShouldEmitEndpointScopedDateTimeMetadata()
    {
        await using var app = CreateTypedHelperApp<DateTime>("InRangeDateTime");

        var document = await GetOpenApiDocumentAsync(app);
        var metadata = GetSchemaComponent(
            document,
            "PortableError__InRangeDateTime__400__application_problem_json__InRange__Metadata"
        );

        foreach (var property in new[] { ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary })
        {
            var schema = (OpenApiSchema) metadata.Properties![property];
            SchemaIncludesType(schema, JsonSchemaType.String).Should().BeTrue();
            schema.Format.Should().Be("date-time");
        }
    }

    [Fact]
    public async Task Transformer_ShouldMixGlobalAndEndpointScopedBuiltInContracts()
    {
        await using var app = CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/mixed", static () => Results.Problem())
                   .WithName("Mixed")
                   .ProducesPortableValidationProblem(
                        configure: x => x
                           .UseFormat(ValidationProblemSerializationFormat.Rich)
                           .WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange)
                           .WithInRangeError<int>()
                    );
            }
        );

        var document = await GetOpenApiDocumentAsync(app);
        var responseItems = GetProblemItems(document, "/mixed");

        responseItems.AnyOf!.Select(static schema => GetSchemaReferenceId((OpenApiSchemaReference) schema))
           .Should()
           .Contain(
                [
                    "PortableError__NotEmpty",
                    "PortableError__LengthInRange",
                    "PortableError__Mixed__400__application_problem_json__InRange",
                    "PortableError"
                ]
            );
    }

    private static WebApplication CreateTypedHelperApp<T>(string operationName)
    {
        return CreateApp(
            _ => { },
            endpoints =>
            {
                endpoints
                   .MapGet("/" + operationName.ToLowerInvariant(), static () => Results.Problem())
                   .WithName(operationName)
                   .ProducesPortableValidationProblem(
                        configure: builder =>
                        {
                            builder.UseFormat(ValidationProblemSerializationFormat.Rich);
                            AddTypedHelper<T>(operationName, builder);
                        }
                    );
            }
        );
    }

    private static void AddTypedHelper<T>(string operationName, PortableValidationProblemOpenApiBuilder builder)
    {
        switch (operationName)
        {
            case "EqualTo":
                builder.WithEqualToError<T>();
                break;
            case "NotEqualTo":
                builder.WithNotEqualToError<T>();
                break;
            case "GreaterThan":
                builder.WithGreaterThanError<T>();
                break;
            case "GreaterThanOrEqualTo":
                builder.WithGreaterThanOrEqualToError<T>();
                break;
            case "LessThan":
                builder.WithLessThanError<T>();
                break;
            case "LessThanOrEqualTo":
                builder.WithLessThanOrEqualToError<T>();
                break;
            case "InRange":
            case "InRangeDateTime":
                builder.WithInRangeError<T>();
                break;
            case "NotInRange":
                builder.WithNotInRangeError<T>();
                break;
            case "ExclusiveRange":
                builder.WithExclusiveRangeError<T>();
                break;
            default:
                throw new InvalidOperationException("Unknown helper: " + operationName);
        }
    }

    private static WebApplication CreateApp(
        Action<PortableErrorMetadataContractsBuilder> configureContracts,
        Action<WebApplication> configureEndpoints,
        Action<OpenApiOptions>? configureOpenApi = null
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddPortableResultsOpenApi();
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options => options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
        );
        builder.Services.ConfigureErrorMetadataContracts(configureContracts);
        builder.Services.AddOpenApi(options => configureOpenApi?.Invoke(options));

        var app = builder.Build();
        configureEndpoints(app);
        return app;
    }

    private static async Task<OpenApiDocument> GetOpenApiDocumentAsync(WebApplication app)
    {
        await app.StartAsync(TestContext.Current.CancellationToken);
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        return await provider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
    }

    private static OpenApiSchema GetProblemItems(OpenApiDocument document, string path)
    {
        var response = (OpenApiResponse) document.Paths[path]
           .Operations![HttpMethod.Get]
           .Responses![StatusCodes.Status400BadRequest.ToString(CultureInfo.InvariantCulture)];
        var schema = (OpenApiSchemaReference) response.Content!["application/problem+json"].Schema!;
        var component = GetSchemaComponent(document, GetSchemaReferenceId(schema));
        var extension = (OpenApiSchema) component.AllOf![1];
        var propertyName = extension.Properties!.ContainsKey("errorDetails") ? "errorDetails" : "errors";
        return (OpenApiSchema) ((OpenApiSchema) extension.Properties[propertyName]).Items!;
    }

    private static OpenApiSchema GetCodeSpecificExtension(OpenApiDocument document, string schemaId)
    {
        return (OpenApiSchema) GetSchemaComponent(document, schemaId).AllOf![1];
    }

    private static OpenApiSchema GetSchemaComponent(OpenApiDocument document, string schemaId)
    {
        return (OpenApiSchema) document.Components!.Schemas![schemaId];
    }

    private static string GetSchemaReferenceId(OpenApiSchemaReference schemaReference)
    {
        var referenceId = schemaReference.Reference.Id ?? schemaReference.Id;
        referenceId.Should().NotBeNull();
        return referenceId;
    }

    private static bool SchemaIncludesType(OpenApiSchema schema, JsonSchemaType type)
    {
        return schema.Type.HasValue && (schema.Type.Value & type) == type;
    }

    private sealed class EquivalentMetadata
    {
        public int Value { get; init; }
    }
}
