using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation.Definitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class PortableValidationProblemOpenApiBuilderTests
{
    [Fact]
    public async Task Transformer_ShouldEmitCodeOnlyExtensionForNoMetadataCodes()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
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

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var component = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(document, "PortableError__NotNull");
        var extension = (OpenApiSchema) component.AllOf![1];

        extension.Properties!.Keys.Should().BeEquivalentTo("code");
        extension.Properties.Should().NotContainKey("metadata");
        extension.Required.Should().BeEquivalentTo("code");
    }

    [Fact]
    public async Task Transformer_ShouldEmitMetadataAndNoMetadataBuiltInCodesTogether()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
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

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var responseItems = ValidationOpenApiDocumentTestUtilities.GetProblemItems(document, "/count-and-not-null");

        responseItems.AnyOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .Contain(["PortableError__Count", "PortableError__NotNull", "PortableError"]);
        var countMetadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            "PortableError__Count__Metadata"
        );
        countMetadata.Properties!.Keys.Should().BeEquivalentTo(ValidationErrorMetadataKeys.ExpectedCount);
        ((OpenApiSchema) countMetadata.Properties[ValidationErrorMetadataKeys.ExpectedCount]).Type.Should()
           .Be(JsonSchemaType.Integer);
    }

    [Fact]
    public async Task Transformer_ShouldProduceValidOpenApi30DocumentWithBuiltInCatalog()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
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

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var validationErrors = document.Validate(ValidationRuleSet.GetDefaultRuleSet());
        await using var stream = new MemoryStream();
        await document.SerializeAsJsonAsync(
            stream,
            OpenApiSpecVersion.OpenApi3_0,
            TestContext.Current.CancellationToken
        );
        var json = Encoding.UTF8.GetString(stream.ToArray());

        validationErrors.Should().BeEmpty();
        json.Should().NotContain("\"type\":\"null\"");
    }

    [Theory]
    [MemberData(
        nameof(ValidationTypedHelperTestData.TypedHelperCases),
        MemberType = typeof(ValidationTypedHelperTestData)
    )]
    public async Task TypedHelpers_ShouldEmitEndpointScopedIntegerMetadata(
        string operationName,
        string code,
        string[] properties
    )
    {
        await using var app = CreateTypedHelperApp<int>(operationName);

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var metadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            $"PortableError__{operationName}__400__application_problem_json__{code}__Metadata"
        );

        metadata.Properties!.Keys.Should().BeEquivalentTo(properties);
        metadata.Required.Should().BeEquivalentTo(properties);
        properties.Should().OnlyContain(
            property =>
                ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(
                    (OpenApiSchema) metadata.Properties[property],
                    JsonSchemaType.Integer
                )
        );
    }

    [Fact]
    public async Task TypedHelpers_ShouldEmitEndpointScopedDateTimeMetadata()
    {
        await using var app = CreateTypedHelperApp<DateTime>("InRangeDateTimeValidation");

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var metadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            "PortableError__InRangeDateTimeValidation__400__application_problem_json__InRange__Metadata"
        );

        foreach (var property in new[]
                     { ValidationErrorMetadataKeys.LowerBoundary, ValidationErrorMetadataKeys.UpperBoundary })
        {
            var schema = (OpenApiSchema) metadata.Properties![property];
            ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(schema, JsonSchemaType.String).Should().BeTrue();
            schema.Format.Should().Be("date-time");
        }
    }

    [Fact]
    public async Task ProducesPortableValidationProblem_ShouldMixGlobalAndEndpointScopedBuiltInContracts()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/mixed-validation", static () => Results.Problem())
                   .WithName("MixedValidation")
                   .ProducesPortableValidationProblem(
                        configure: x => x
                           .WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange)
                           .UseFormat(ValidationProblemSerializationFormat.Rich)
                           .WithInRangeError<int>()
                    );
            }
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var responseItems = ValidationOpenApiDocumentTestUtilities.GetProblemItems(document, "/mixed-validation");

        responseItems.AnyOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .Contain(
                [
                    "PortableError__NotEmpty",
                    "PortableError__LengthInRange",
                    "PortableError__MixedValidation__400__application_problem_json__InRange",
                    "PortableError"
                ]
            );
    }

    private static WebApplication CreateTypedHelperApp<T>(string operationName)
    {
        return ValidationOpenApiDocumentTestUtilities.CreateApp(
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
                            ValidationTypedHelperTestData.AddTypedHelper<T>(operationName, builder);
                        }
                    );
            }
        );
    }
}
