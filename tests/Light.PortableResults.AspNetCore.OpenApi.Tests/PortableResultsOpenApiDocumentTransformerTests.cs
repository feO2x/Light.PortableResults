using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.Mvc;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableResultsOpenApiDocumentTransformerTests
{
    [Fact]
    public async Task MinimalApiDocument_ShouldEmitConfiguredSchemas()
    {
        await using var app = CreateMinimalApiApp();

        var document = await GetOpenApiDocumentAsync(app);

        var defaultSuccessSchema = GetResponseSchema(
            document,
            "/minimal/success/default",
            HttpMethod.Get,
            StatusCodes.Status200OK,
            "application/json"
        );
        defaultSuccessSchema.Should().BeOfType<OpenApiSchema>();
        ((OpenApiSchema) defaultSuccessSchema).Properties.Should().ContainKey("title");

        var wrappedSuccessSchema = GetResponseSchema(
            document,
            "/minimal/success/wrapped",
            HttpMethod.Get,
            StatusCodes.Status200OK,
            "application/json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var wrappedSuccessComponent = GetSchemaComponent(document, GetSchemaReferenceId(wrappedSuccessSchema));
        wrappedSuccessComponent.Properties.Should().ContainKeys("value", "metadata");
        wrappedSuccessComponent.Required.Should().Contain("value");
        wrappedSuccessComponent.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>();
        var successMetadataReference = (OpenApiSchemaReference) wrappedSuccessComponent.Properties["metadata"];
        GetSchemaComponent(document, GetSchemaReferenceId(successMetadataReference))
           .Properties.Should().ContainKey("traceId");

        var globalProblemSchema = GetResponseSchema(
            document,
            "/minimal/problems/global",
            HttpMethod.Get,
            StatusCodes.Status409Conflict,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var globalProblemComponent = GetSchemaComponent(document, GetSchemaReferenceId(globalProblemSchema));
        var globalProblemExtension = (OpenApiSchema) globalProblemComponent.AllOf![1];
        var globalProblemErrors = (OpenApiSchema) globalProblemExtension.Properties!["errors"];
        var globalProblemItems = (OpenApiSchema) globalProblemErrors.Items!;
        globalProblemItems.AnyOf.Should().HaveCount(3);
        GetSchemaReferenceId((OpenApiSchemaReference) globalProblemItems.AnyOf![0]).Should()
           .Be("PortableError__VersionMismatch");
        GetSchemaReferenceId((OpenApiSchemaReference) globalProblemItems.AnyOf[1]).Should()
           .Be("PortableError__Insufficient_Funds");
        GetSchemaReferenceId((OpenApiSchemaReference) globalProblemItems.AnyOf[2]).Should().Be("PortableError");
        globalProblemItems.Discriminator.Should().NotBeNull();
        globalProblemItems.Discriminator!.PropertyName.Should().Be("code");
        globalProblemItems.Discriminator.Mapping.Should().NotBeNull();
        globalProblemItems.Discriminator.Mapping!.Keys.Should().BeEquivalentTo("VersionMismatch", "Insufficient/Funds");
        GetSchemaReferenceId(globalProblemItems.Discriminator.Mapping["Insufficient/Funds"])
           .Should().Be("PortableError__Insufficient_Funds");

        var inlineProblemSchema = GetResponseSchema(
            document,
            "/minimal/problems/inline",
            HttpMethod.Get,
            StatusCodes.Status404NotFound,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var inlineProblemComponent = GetSchemaComponent(document, GetSchemaReferenceId(inlineProblemSchema));
        var inlineProblemExtension = (OpenApiSchema) inlineProblemComponent.AllOf![1];
        var inlineProblemItems = (OpenApiSchema) ((OpenApiSchema) inlineProblemExtension.Properties!["errors"]).Items!;
        GetSchemaReferenceId((OpenApiSchemaReference) inlineProblemItems.AnyOf![0]).Should().Contain("PortableError__");
        GetSchemaReferenceId((OpenApiSchemaReference) inlineProblemItems.AnyOf[0]).Should().Contain("Movie_Gone");
        GetSchemaReferenceId((OpenApiSchemaReference) inlineProblemItems.AnyOf[1]).Should().Be("PortableError");

        var defaultValidationSchema = GetResponseSchema(
            document,
            "/minimal/validation/default",
            HttpMethod.Get,
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        GetSchemaReferenceId(defaultValidationSchema).Should().Be("PortableAspNetCoreValidationProblemDetails");

        var richValidationSchema = GetResponseSchema(
            document,
            "/minimal/validation/rich",
            HttpMethod.Get,
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        GetSchemaReferenceId(richValidationSchema).Should().StartWith("PortableRichValidationProblemDetails__");

        var unionSchema = GetResponseSchema(
            document,
            "/minimal/problems/union",
            HttpMethod.Get,
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchema>().Subject;
        unionSchema.AnyOf.Should().HaveCount(2);
    }

    [Fact]
    public async Task MvcDocument_ShouldHonorPortableOpenApiAttributes()
    {
        await using var app = CreateMvcApp();

        var document = await GetOpenApiDocumentAsync(app);

        var successSchema = GetResponseSchema(
            document,
            "/mvc/openapi/success",
            HttpMethod.Get,
            StatusCodes.Status200OK,
            "application/json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        GetSchemaComponent(document, GetSchemaReferenceId(successSchema)).Properties.Should()
           .ContainKeys("value", "metadata");

        var problemSchema = GetResponseSchema(
            document,
            "/mvc/openapi/problem",
            HttpMethod.Get,
            StatusCodes.Status404NotFound,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var problemComponent = GetSchemaComponent(document, GetSchemaReferenceId(problemSchema));
        ((OpenApiSchema) problemComponent.AllOf![1]).Properties.Should().ContainKey("errors");

        var validationSchema = GetResponseSchema(
            document,
            "/mvc/openapi/validation",
            HttpMethod.Get,
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        GetSchemaReferenceId(validationSchema).Should().StartWith("PortableRichValidationProblemDetails__");

        var customSuccessSchema = GetResponseSchema(
            document,
            "/mvc/openapi/custom-success",
            HttpMethod.Get,
            StatusCodes.Status202Accepted,
            "application/json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var customSuccessComponent = GetSchemaComponent(document, GetSchemaReferenceId(customSuccessSchema));
        customSuccessComponent.Properties.Should().ContainKeys("value", "metadata");
    }

    [Fact]
    public async Task Transformer_ShouldUseEnumNarrowingForOpenApi30AndConstForOpenApi31()
    {
        await using var openApi30App =
            CreateMinimalApiApp(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0);
        var openApi30Document = await GetOpenApiDocumentAsync(openApi30App);

        var openApi30Variant = GetSchemaComponent(openApi30Document, "PortableError__VersionMismatch");
        var openApi30CodeSchema = (OpenApiSchema) ((OpenApiSchema) openApi30Variant.AllOf![1]).Properties!["code"];
        openApi30CodeSchema.Const.Should().BeNull();
        openApi30CodeSchema.Enum.Should().ContainSingle();
        openApi30CodeSchema.Enum![0].ToJsonString().Should().Be("\"VersionMismatch\"");

        await using var openApi31App =
            CreateMinimalApiApp(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        var openApi31Document = await GetOpenApiDocumentAsync(openApi31App);

        var openApi31Variant = GetSchemaComponent(openApi31Document, "PortableError__VersionMismatch");
        var openApi31CodeSchema = (OpenApiSchema) ((OpenApiSchema) openApi31Variant.AllOf![1]).Properties!["code"];
        openApi31CodeSchema.Const.Should().Be("VersionMismatch");
    }

    [Fact]
    public async Task Transformer_ShouldThrowWhenAnEndpointUsesAnUnknownGlobalErrorCode()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/unknown", static () => TypedResults.Problem())
                   .ProducesPortableProblem(configure: x => x.WithErrorCodes("UnknownCode"));
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*UnknownCode*AddPortableResultsOpenApi*WithErrorMetadata*");
    }

    [Fact]
    public async Task Transformer_ShouldThrowWhenSuccessMetadataIsDocumentedForErrorsOnlyMode()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/success/invalid", static () => TypedResults.Ok(new MovieDto()))
                   .ProducesPortableSuccessResponse<MovieDto>(configure: x => x.WithMetadata<SuccessMetadata>());
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act.Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*MetadataSerializationMode is ErrorsOnly*");
    }

    [Fact]
    public async Task Transformer_ShouldThrowWhenDuplicateKindsShareTheSameResponseKey()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/ambiguous", static () => TypedResults.Problem())
                   .ProducesPortableProblem(StatusCodes.Status400BadRequest)
                   .ProducesPortableProblem(StatusCodes.Status400BadRequest);
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act.Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*status code 400*kind 'Problem'*");
    }

    [Fact]
    public async Task Transformer_ShouldTreatDuplicateKindsWithDifferentContentTypeCasingAsAmbiguous()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/ambiguous-casing", static () => TypedResults.Problem())
                   .WithMetadata(
                        new ProducesPortableProblemAttribute(
                            StatusCodes.Status400BadRequest,
                            // ReSharper disable once RedundantArgumentDefaultValue
                            "application/problem+json"
                        )
                    )
                   .WithMetadata(
                        new ProducesPortableProblemAttribute(
                            StatusCodes.Status400BadRequest,
                            "application/PROBLEM+JSON"
                        )
                    );
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act.Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*status code 400*application/problem+json*kind 'Problem'*");
    }

    [Fact]
    public async Task Transformer_ShouldMergeResponseSchemas_WhenContentTypeOnlyDiffersByCasing()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/union-casing", static () => TypedResults.Problem())
                   .WithMetadata(
                        new ProducesPortableProblemAttribute(
                            StatusCodes.Status400BadRequest,
                            // ReSharper disable once RedundantArgumentDefaultValue
                            "application/problem+json"
                        )
                    )
                   .WithMetadata(
                        new ProducesPortableValidationProblemAttribute(
                            StatusCodes.Status400BadRequest,
                            "application/PROBLEM+JSON"
                        )
                    );
            }
        );

        var response = GetResponse(
            await GetOpenApiDocumentAsync(app),
            "/minimal/problems/union-casing",
            HttpMethod.Get,
            StatusCodes.Status400BadRequest
        );

        response.Content.Should().ContainSingle();
        response.Content!.Keys.Should().ContainSingle(
            key => string.Equals(key, "application/problem+json", StringComparison.OrdinalIgnoreCase)
        );
        var mediaType = response.Content.Values.Should().ContainSingle().Subject;
        mediaType.Schema.Should().BeOfType<OpenApiSchema>();
        ((OpenApiSchema) mediaType.Schema!).AnyOf.Should().HaveCount(2);
    }

    [Fact]
    public async Task Transformer_ShouldMaterializeReferencedResponsesBeforeWritingContent()
    {
        await using var app = CreateMinimalApiApp(
            configureOpenApi: options =>
            {
                options.AddOperationTransformer(
                    (operation, _, _) =>
                    {
                        operation.Responses ??= new OpenApiResponses();
                        operation.Responses[StatusCodes.Status409Conflict.ToString(CultureInfo.InvariantCulture)] =
                            new OpenApiResponseReference(
                                "UpstreamConflictResponse",
                                new OpenApiDocument(),
                                externalResource: null
                            );
                        return Task.CompletedTask;
                    }
                );
            },
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/upstream-reference", static () => TypedResults.Problem())
                   .ProducesPortableProblem(StatusCodes.Status409Conflict);
            }
        );

        var document = await GetOpenApiDocumentAsync(app);
        var responseEntry = document
           .Paths["/minimal/problems/upstream-reference"]
           .Operations![HttpMethod.Get]
           .Responses![StatusCodes.Status409Conflict.ToString(CultureInfo.InvariantCulture)];

        responseEntry.Should().BeOfType<OpenApiResponse>();
        var response = (OpenApiResponse) responseEntry;
        response.Content.Should().ContainKey("application/problem+json");
        response.Content["application/problem+json"].Schema.Should().BeOfType<OpenApiSchemaReference>();
    }

    [Fact]
    public async Task Transformer_ShouldReplaceExistingSchemaForTheSameResponseSlot()
    {
        await using var app = CreateMinimalApiApp(
            configureOpenApi: options =>
            {
                options.AddOperationTransformer(
                    (operation, context, _) =>
                    {
                        if (!string.Equals(
                                context.Description.RelativePath,
                                "minimal/success/replaces-existing",
                                StringComparison.Ordinal
                            ))
                        {
                            return Task.CompletedTask;
                        }

                        operation.Responses ??= new OpenApiResponses();
                        operation.Responses[StatusCodes.Status200OK.ToString(CultureInfo.InvariantCulture)] =
                            new OpenApiResponse
                            {
                                Description = "HTTP 200",
                                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.OrdinalIgnoreCase)
                                {
                                    ["application/json"] = new ()
                                    {
                                        Schema = new OpenApiSchema
                                        {
                                            Type = JsonSchemaType.String
                                        }
                                    }
                                }
                            };
                        return Task.CompletedTask;
                    }
                );
            },
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/success/replaces-existing", static () => TypedResults.Ok(new MovieDto()))
                   .ProducesPortableSuccessResponse<MovieDto>();
            }
        );

        var schema = GetResponseSchema(
            await GetOpenApiDocumentAsync(app),
            "/minimal/success/replaces-existing",
            HttpMethod.Get,
            StatusCodes.Status200OK,
            "application/json"
        );

        schema.Should().BeOfType<OpenApiSchema>();
        var concreteSchema = (OpenApiSchema) schema;
        concreteSchema.Type.Should().NotBe(JsonSchemaType.String);
        concreteSchema.Properties.Should().ContainKey("title");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Transformer_ShouldThrowWhenInlineErrorMetadataArraysAreOnlyPartiallyConfigured(
        bool configureCodes
    )
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                var attribute = new ProducesPortableProblemAttribute(StatusCodes.Status404NotFound);
                if (configureCodes)
                {
                    attribute.InlineErrorMetadataCodes = new[] { "Movie/Gone" };
                }
                else
                {
                    attribute.InlineErrorMetadataContracts = [PortableErrorMetadataContract.FromType(typeof(InlineProblemMetadata))];
                }

                webApplication
                   .MapGet($"/minimal/problems/invalid-inline-{configureCodes}", static () => TypedResults.Problem())
                   .WithMetadata(attribute);
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act.Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*InlineErrorMetadataCodes*InlineErrorMetadataContracts*");
    }

    [Fact]
    public void ErrorMetadataContractsBuilder_ShouldRejectSanitizedCodeCollisions()
    {
        var builder = new PortableErrorMetadataContractsBuilder();

        builder.ForCode<VersionMismatchMetadata>("Code/One");
        var act = () => builder.ForCode<FundsMetadata>("Code_One");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Code/One*Code_One*");
    }

    [Fact]
    public async Task Transformer_ShouldOmitMetadataProperty_WhenGlobalCodeHasNoMetadata()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddPortableResultsOpenApi(contracts => contracts.ForCode("SimpleError"));
        builder.Services.AddOpenApi();

        await using var app = builder.Build();
        app.MapGet("/simple-error", static () => TypedResults.Problem())
           .ProducesPortableProblem(StatusCodes.Status400BadRequest, configure: x => x.WithErrorCodes("SimpleError"));

        var document = await GetOpenApiDocumentAsync(app);
        var schemaComponent = GetSchemaComponent(document, "PortableError__SimpleError");
        var extension = (OpenApiSchema) schemaComponent.AllOf![1];

        extension.Properties!.Keys.Should().BeEquivalentTo("code");
        extension.Properties.Should().NotContainKey("metadata");
    }

    [Fact]
    public async Task Transformer_ShouldAcceptDuplicateInlineMetadataCode_WhenTypeIsIdentical()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/inline-duplicate", static () => TypedResults.Problem())
                   .ProducesPortableProblem(
                        StatusCodes.Status404NotFound,
                        configure: x => x
                           .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                           .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                    );
            }
        );

        var document = await GetOpenApiDocumentAsync(app);
        var responseSchemaRef = GetResponseSchema(
            document,
            "/minimal/problems/inline-duplicate",
            HttpMethod.Get,
            StatusCodes.Status404NotFound,
            "application/problem+json"
        ).Should().BeOfType<OpenApiSchemaReference>().Subject;
        var component = GetSchemaComponent(document, GetSchemaReferenceId(responseSchemaRef));
        var items =
            (OpenApiSchema) ((OpenApiSchema) ((OpenApiSchema) component.AllOf![1]).Properties!["errors"]).Items!;

        // Duplicate inline code with identical type must be deduplicated: only one documented variant + fallback.
        items.AnyOf.Should().HaveCount(2);
    }

    [Fact]
    public async Task Transformer_ShouldThrow_WhenInlineMetadataCodesConflict()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/inline-conflict", static () => TypedResults.Problem())
                   .ProducesPortableProblem(
                        StatusCodes.Status404NotFound,
                        configure: x => x
                           .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                           .WithErrorMetadata<ProblemMetadata>("Movie/Gone")
                    );
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act
           .Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*Movie/Gone*InlineProblemMetadata*ProblemMetadata*");
    }

    [Fact]
    public async Task Transformer_ShouldThrow_WhenInlineCodesSanitizeToSameName()
    {
        await using var app = CreateMinimalApiApp(
            configureEndpoints: webApplication =>
            {
                webApplication
                   .MapGet("/minimal/problems/inline-sanitize-collision", static () => TypedResults.Problem())
                   .ProducesPortableProblem(
                        StatusCodes.Status404NotFound,
                        configure: x => x
                           .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                           .WithErrorMetadata<ProblemMetadata>("Movie_Gone")
                    );
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await GetOpenApiDocumentAsync(app);

        await act
           .Should()
           .ThrowAsync<InvalidOperationException>()
           .WithMessage("*Movie/Gone*Movie_Gone*");
    }

    [Fact]
    public void OpenApiModule_ShouldRegisterErrorMetadataRegistryOnlyOnce_WhenConfiguredViaAddPortableResultsOpenApi()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        services.AddPortableResultsOpenApi(
            contracts => contracts.ForCode<VersionMismatchMetadata>("VersionMismatch")
        );
        services.AddPortableResultsOpenApi(
            contracts => contracts.ForCode<FundsMetadata>("Insufficient/Funds")
        );

        services.Where(static descriptor => descriptor.ServiceType == typeof(IPortableErrorMetadataContractRegistry))
           .Should()
           .ContainSingle();

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IPortableErrorMetadataContractRegistry>();
        registry.Contracts.Should().ContainKey("VersionMismatch");
        registry.Contracts.Should().ContainKey("Insufficient/Funds");
    }

    private static async Task<OpenApiDocument> GetOpenApiDocumentAsync(WebApplication app)
    {
        await app.StartAsync(TestContext.Current.CancellationToken);
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        return await provider.GetOpenApiDocumentAsync(TestContext.Current.CancellationToken);
    }

    private static WebApplication CreateMinimalApiApp(
        Action<OpenApiOptions>? configureOpenApi = null,
        Action<WebApplication>? configureEndpoints = null
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddPortableResultsOpenApi(
            contracts =>
            {
                contracts.ForCode<VersionMismatchMetadata>("VersionMismatch");
                contracts.ForCode<FundsMetadata>("Insufficient/Funds");
            }
        );
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options =>
            {
                options.MetadataSerializationMode = MetadataSerializationMode.ErrorsOnly;
                options.ValidationProblemSerializationFormat =
                    ValidationProblemSerializationFormat.AspNetCoreCompatible;
            }
        );
        builder.Services.AddOpenApi(options => configureOpenApi?.Invoke(options));

        var app = builder.Build();
        app.MapGet("/minimal/success/default", static () => TypedResults.Ok(new MovieDto()))
           .ProducesPortableSuccessResponse<MovieDto>();
        app.MapGet("/minimal/success/wrapped", static () => TypedResults.Ok(new MovieDto()))
           .ProducesPortableSuccessResponse<MovieDto>(
                configure: x =>
                    x.WithMetadata<SuccessMetadata>()
                       .UseMetadataSerializationMode(MetadataSerializationMode.Always)
            );
        app.MapGet("/minimal/problems/global", static () => TypedResults.Problem())
           .ProducesPortableProblem(
                StatusCodes.Status409Conflict,
                configure: x =>
                    x.WithMetadata<ProblemMetadata>()
                       .WithErrorCodes("VersionMismatch", "Insufficient/Funds")
            );
        app.MapGet("/minimal/problems/inline", static () => TypedResults.Problem())
           .ProducesPortableProblem(
                StatusCodes.Status404NotFound,
                configure: x =>
                    x.WithMetadata<ProblemMetadata>()
                       .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
            );
        app.MapGet("/minimal/validation/default", static () => TypedResults.Problem())
           .ProducesPortableValidationProblem();
        app.MapGet("/minimal/validation/rich", static () => TypedResults.Problem())
           .ProducesPortableValidationProblem(
                configure: x =>
                    x.UseFormat(ValidationProblemSerializationFormat.Rich)
                       .WithErrorCodes("VersionMismatch")
            );
        app.MapGet("/minimal/problems/union", static () => TypedResults.Problem())
           .ProducesPortableProblem(StatusCodes.Status400BadRequest)
           .ProducesPortableValidationProblem();

        configureEndpoints?.Invoke(app);
        return app;
    }

    private static WebApplication CreateMvcApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMvc();
        builder.Services.AddPortableResultsOpenApi(
            contracts => contracts.ForCode<VersionMismatchMetadata>("VersionMismatch")
        );
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options =>
            {
                options.MetadataSerializationMode = MetadataSerializationMode.ErrorsOnly;
                options.ValidationProblemSerializationFormat =
                    ValidationProblemSerializationFormat.AspNetCoreCompatible;
            }
        );
        builder.Services.AddControllers().AddApplicationPart(typeof(OpenApiMvcController).Assembly);
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.MapControllers();
        return app;
    }

    private static IOpenApiSchema GetResponseSchema(
        OpenApiDocument document,
        string path,
        HttpMethod httpMethod,
        int statusCode,
        string contentType
    )
    {
        var response = GetResponse(document, path, httpMethod, statusCode);
        return response.Content![contentType].Schema!;
    }

    private static OpenApiResponse GetResponse(
        OpenApiDocument document,
        string path,
        HttpMethod httpMethod,
        int statusCode
    )
    {
        var pathItem = document.Paths[path];
        var operation = pathItem.Operations![httpMethod];
        return (OpenApiResponse) operation.Responses![statusCode.ToString(CultureInfo.InvariantCulture)];
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
}

[ApiController]
[Route("mvc/openapi")]
public sealed class OpenApiMvcController : ControllerBase
{
    [HttpGet("success")]
    [ProducesPortableSuccessResponse<MovieDto>(
        TopLevelMetadataType = typeof(SuccessMetadata),
        MetadataSerializationMode = MetadataSerializationMode.Always
    )]
    public ActionResult<MovieDto> GetSuccess() => Ok(new MovieDto());

    [HttpGet("problem")]
    [ProducesPortableProblem(
        StatusCodes.Status404NotFound,
        TopLevelMetadataType = typeof(ProblemMetadata),
        ErrorCodes = ["VersionMismatch"]
    )]
    public IActionResult GetProblem() => Problem();

    [HttpGet("validation")]
    [ProducesPortableValidationProblem(
        Format = ValidationProblemSerializationFormat.Rich,
        ErrorCodes = ["VersionMismatch"]
    )]
    public IActionResult GetValidation() => Problem();

    [HttpGet("custom-success")]
    [CustomPortableSuccessResponse(typeof(SuccessMetadata), StatusCodes.Status202Accepted)]
    public ActionResult<MovieDto> GetCustomSuccess() => Accepted(new MovieDto());
}

public sealed class CustomPortableSuccessResponseAttribute : PortableOpenApiSuccessResponseAttributeBase
{
    public CustomPortableSuccessResponseAttribute(Type metadataType, int statusCode = StatusCodes.Status200OK)
        : base(statusCode, "application/json", typeof(MovieDto))
    {
        TopLevelMetadataType = metadataType;
        MetadataSerializationMode = MetadataSerializationMode.Always;
    }
}

// ReSharper disable UnusedMember.Global - required for testing
public sealed class MovieDto
{
    public string Title { get; init; } = string.Empty;
}

public sealed class SuccessMetadata
{
    public string TraceId { get; init; } = string.Empty;
}

public sealed class ProblemMetadata
{
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed class InlineProblemMetadata
{
    public string MovieId { get; init; } = string.Empty;
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class VersionMismatchMetadata
{
    public string CurrentVersion { get; init; } = string.Empty;
}

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class FundsMetadata
{
    public decimal MissingAmount { get; init; }
}
// ReSharper restore UnusedMember.Global
