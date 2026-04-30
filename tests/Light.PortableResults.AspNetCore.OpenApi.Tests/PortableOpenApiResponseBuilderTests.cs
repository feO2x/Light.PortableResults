using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableOpenApiResponseBuilderTests
{
    [Fact]
    public void ProducesPortableProblem_ShouldAccumulateRouteMetadata()
    {
        var attribute = GetMetadata<ProducesPortableProblemAttribute>(
            static builder =>
                builder.ProducesPortableProblem(
                    StatusCodes.Status404NotFound,
                    configure: builder => builder
                       .WithErrorCodes("First")
                       .WithErrorCodes("Second", "Third")
                       .WithMetadata<InlineProblemMetadata>()
                       .WithMetadata(typeof(ProblemMetadata))
                       .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                       .WithErrorMetadata("Movie/Archived", typeof(ProblemMetadata))
                ),
            static () => TypedResults.Problem()
        );

        attribute.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        attribute.TopLevelMetadataType.Should().Be(typeof(ProblemMetadata));
        attribute.ErrorCodes.Should().Equal("First", "Second", "Third");
        attribute.InlineErrorMetadataCodes.Should().Equal("Movie/Gone", "Movie/Archived");
        attribute.InlineErrorMetadataContracts.Should().Equal(
            PortableErrorMetadataContract.FromType(typeof(InlineProblemMetadata)),
            PortableErrorMetadataContract.FromType(typeof(ProblemMetadata))
        );
    }

    [Fact]
    public void ProducesPortableValidationProblem_ShouldAccumulateRouteMetadata()
    {
        var attribute = GetMetadata<ProducesPortableValidationProblemAttribute>(
            static builder =>
                builder.ProducesPortableValidationProblem(
                    configure: x => x.WithErrorCodes("First")
                       .WithErrorCodes()
                       .WithErrorCodes("Second", "Third")
                       .WithMetadata<InlineProblemMetadata>()
                       .WithMetadata(typeof(ProblemMetadata))
                       .WithErrorMetadata<InlineProblemMetadata>("Movie/Gone")
                       .WithErrorMetadata("Movie/Archived", typeof(ProblemMetadata))
                       .UseFormat(ValidationProblemSerializationFormat.Rich)
                ),
            static () => TypedResults.Problem()
        );

        attribute.TopLevelMetadataType.Should().Be(typeof(ProblemMetadata));
        attribute.ErrorCodes.Should().Equal("First", "Second", "Third");
        attribute.InlineErrorMetadataCodes.Should().Equal("Movie/Gone", "Movie/Archived");
        attribute.InlineErrorMetadataContracts.Should().Equal(
            PortableErrorMetadataContract.FromType(typeof(InlineProblemMetadata)),
            PortableErrorMetadataContract.FromType(typeof(ProblemMetadata))
        );
        attribute.Format.Should().Be(ValidationProblemSerializationFormat.Rich);
        attribute.HasFormatOverride.Should().BeTrue();
    }

    [Fact]
    public void ProducesPortableSuccessResponse_ShouldAccumulateRouteMetadata()
    {
        var attribute = GetMetadata<PortableOpenApiSuccessResponseAttributeBase>(
            static builder =>
                builder.ProducesPortableSuccessResponse<MovieDto>(
                    StatusCodes.Status202Accepted,
                    configure: x => x.WithMetadata<ProblemMetadata>()
                       .WithMetadata(typeof(SuccessMetadata))
                       .UseMetadataSerializationMode(MetadataSerializationMode.Always)
                ),
            static () => TypedResults.Ok(new MovieDto())
        );

        attribute.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        attribute.ValueType.Should().Be(typeof(MovieDto));
        attribute.TopLevelMetadataType.Should().Be(typeof(SuccessMetadata));
        attribute.MetadataSerializationMode.Should().Be(MetadataSerializationMode.Always);
        attribute.HasMetadataSerializationModeOverride.Should().BeTrue();
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

        var typeExtension = GetCodeSpecificExtension(
            await GetOpenApiDocumentAsync(typeApp),
            "PortableError__Equivalent"
        );
        var schemaExtension =
            GetCodeSpecificExtension(await GetOpenApiDocumentAsync(schemaApp), "PortableError__Equivalent");

        typeExtension.Properties!.Keys.Should().BeEquivalentTo(schemaExtension.Properties!.Keys);
        typeExtension.Required.Should().BeEquivalentTo(schemaExtension.Required);
        typeExtension.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>();
        schemaExtension.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>();
    }

    private static TAttribute GetMetadata<TAttribute>(
        Action<RouteHandlerBuilder> configure,
        Delegate handler
    )
        where TAttribute : class
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        configure(app.MapGet("/metadata", handler));

        return ((IEndpointRouteBuilder) app).DataSources.SelectMany(static dataSource => dataSource.Endpoints)
              .OfType<RouteEndpoint>()
              .Single()
              .Metadata.GetMetadata<TAttribute>() ??
               throw new InvalidOperationException($"No metadata of type {typeof(TAttribute).FullName} was found.");
    }

    private static WebApplication CreateApp(
        Action<PortableErrorMetadataContractsBuilder> configureContracts,
        Action<WebApplication> configureEndpoints
    )
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddPortableResultsOpenApi(configureContracts);
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options =>
            {
                options.MetadataSerializationMode = MetadataSerializationMode.ErrorsOnly;
                options.ValidationProblemSerializationFormat =
                    ValidationProblemSerializationFormat.AspNetCoreCompatible;
            }
        );
        builder.Services.AddOpenApi();

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

    private static OpenApiSchema GetCodeSpecificExtension(OpenApiDocument document, string schemaId)
    {
        return (OpenApiSchema) GetSchemaComponent(document, schemaId).AllOf![1];
    }

    private static OpenApiSchema GetSchemaComponent(OpenApiDocument document, string schemaId)
    {
        return (OpenApiSchema) document.Components!.Schemas![schemaId];
    }

    // ReSharper disable once ClassNeverInstantiated.Local -- required for testing
    private sealed class EquivalentMetadata
    {
        // ReSharper disable once UnusedMember.Local -- required for testing
        public int Value { get; init; }
    }
}
