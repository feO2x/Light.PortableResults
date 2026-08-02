using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class GeneratedValidationOpenApiIntegrationTests
{
    [Fact]
    public async Task ProducesPortableValidationProblemFor_ShouldApplyGeneratedSchemasAndExamples()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapPost("/generated-validation", static () => Results.BadRequest())
                   .WithName("GeneratedValidation")
                   .ProducesPortableValidationProblemFor<GeneratedRatingValidator>(
                        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich)
                    )
                   .ProducesPortableProblem();
            }
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var operation = document.Paths["/generated-validation"].Operations![HttpMethod.Post];
        var response = (OpenApiResponse) operation.Responses![StatusCodes.Status400BadRequest.ToString()];
        var mediaType = response.Content!["application/problem+json"];
        var schemaReference = (OpenApiSchemaReference) mediaType.Schema!;
        var envelope = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(schemaReference)
        );
        var errors = (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errors"]).Items!;

        errors.OneOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .BeEquivalentTo(
                "PortableError__LengthInRange",
                "PortableError__NotEmpty",
                "PortableError__GeneratedValidation__400__application_problem_json__InRange"
            );

        mediaType.Examples.Should().ContainKey("ValidationProblem");
        var example = (OpenApiExample) mediaType.Examples["ValidationProblem"];
        var body = example.Value.Should().BeOfType<JsonObject>().Subject;
        var exampleErrors = body["errors"].Should().BeOfType<JsonArray>().Subject;
        exampleErrors.ToJsonString().Should().Contain("\"lowerBoundary\":1");
        exampleErrors.ToJsonString().Should().Contain("\"upperBoundary\":5");
        exampleErrors.ToJsonString().Should().Contain("\"minLength\":10");
        exampleErrors.ToJsonString().Should().Contain("\"maxLength\":1000");
        exampleErrors.ToJsonString().Should().Contain("\"message\":\"id must not be empty\"");
        exampleErrors.ToJsonString().Should()
           .Contain("\"message\":\"comment must be between 10 and 1000 characters long\"");
        exampleErrors.ToJsonString().Should().Contain("\"message\":\"rating must be between 1 and 5\"");

        var genericProblemResponse =
            (OpenApiResponse) operation.Responses![StatusCodes.Status500InternalServerError.ToString()];
        genericProblemResponse.Content!["application/problem+json"].Examples.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ProducesPortableValidationProblemFor_ShouldDocumentUuidV7FromTheBuiltInContracts()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapPost("/generated-validation/uuid-v7", static () => Results.BadRequest())
                   .WithName("GeneratedUuidV7Validation")
                   .ProducesPortableValidationProblemFor<GeneratedClientIdentifierValidator>(
                        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich)
                    );
            }
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var operation = document.Paths["/generated-validation/uuid-v7"].Operations![HttpMethod.Post];
        var response = (OpenApiResponse) operation.Responses![StatusCodes.Status400BadRequest.ToString()];
        var mediaType = response.Content!["application/problem+json"];
        var schemaReference = (OpenApiSchemaReference) mediaType.Schema!;
        var envelope = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(schemaReference)
        );
        var errors = (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errors"]).Items!;

        errors.OneOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .BeEquivalentTo("PortableError__UuidV7");

        var example = (OpenApiExample) mediaType.Examples!["ValidationProblem"];
        var body = example.Value.Should().BeOfType<JsonObject>().Subject;
        var exampleErrors = body["errors"].Should().BeOfType<JsonArray>().Subject;
        exampleErrors.ToJsonString().Should().Contain("\"code\":\"UuidV7\"");
        exampleErrors.ToJsonString().Should().Contain("\"message\":\"id must be a version 7 UUID\"");
    }

    // A missing registry entry for one of these codes surfaces here rather than as a generator diagnostic:
    // document construction fails when a discovered rule has no registered metadata contract.
    [Fact]
    public async Task ProducesPortableValidationProblemFor_ShouldDocumentDateTimeKindsFromTheBuiltInContracts()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapPost("/generated-validation/date-time-kinds", static () => Results.BadRequest())
                   .WithName("GeneratedDateTimeKindValidation")
                   .ProducesPortableValidationProblemFor<GeneratedTimestampValidator>(
                        configure: builder => builder.UseFormat(ValidationProblemSerializationFormat.Rich)
                    );
            }
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var operation = document.Paths["/generated-validation/date-time-kinds"].Operations![HttpMethod.Post];
        var response = (OpenApiResponse) operation.Responses![StatusCodes.Status400BadRequest.ToString()];
        var mediaType = response.Content!["application/problem+json"];
        var schemaReference = (OpenApiSchemaReference) mediaType.Schema!;
        var envelope = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(schemaReference)
        );
        var errors = (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errors"]).Items!;

        errors.OneOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .BeEquivalentTo("PortableError__Utc", "PortableError__Local", "PortableError__Unspecified");

        var example = (OpenApiExample) mediaType.Examples!["ValidationProblem"];
        var body = example.Value.Should().BeOfType<JsonObject>().Subject;
        var exampleErrors = body["errors"].Should().BeOfType<JsonArray>().Subject;
        var exampleJson = exampleErrors.ToJsonString();
        exampleJson.Should().Contain("\"code\":\"Utc\"");
        exampleJson.Should().Contain("\"message\":\"recordedAt must be represented in UTC\"");
        exampleJson.Should().Contain("\"code\":\"Local\"");
        exampleJson.Should().Contain("\"message\":\"scheduledAt must be a local date and time\"");
        exampleJson.Should().Contain("\"code\":\"Unspecified\"");
        exampleJson.Should().Contain("\"message\":\"observedAt must not specify a time zone\"");
    }

    [Fact]
    public async Task MvcAttribute_ShouldApplyGeneratedSchemasExamplesAndOverrides()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateMvcApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            controllers => controllers.AddApplicationPart(typeof(GeneratedValidationOpenApiIntegrationTests).Assembly)
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var operation = document.Paths["/generated-validation/mvc"].Operations![HttpMethod.Post];
        var response = (OpenApiResponse) operation.Responses![StatusCodes.Status422UnprocessableEntity.ToString()];
        var mediaType = response.Content!["application/problem+json"];
        var schemaReference = (OpenApiSchemaReference) mediaType.Schema!;
        var envelope = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(schemaReference)
        );

        envelope.Properties.Should().ContainKey("metadata");
        var metadataReference = envelope.Properties["metadata"].Should().BeOfType<OpenApiSchemaReference>().Subject;
        var metadataSchema = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(metadataReference)
        );
        metadataSchema.Properties.Should().ContainKey("traceId");

        var errors = (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errors"]).Items!;
        errors.OneOf.Should().BeNull();
        var errorSchemaIds = errors.AnyOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .ToArray();
        errorSchemaIds.Should().Contain(["PortableError__LengthInRange", "PortableError__NotEmpty", "PortableError"]);
        errorSchemaIds.Should().ContainSingle(
            schemaId => schemaId.StartsWith("PortableError__", StringComparison.Ordinal) &&
                        schemaId.EndsWith("__InRange", StringComparison.Ordinal)
        );

        mediaType.Examples.Should().ContainKey("ValidationProblem");
        var example = (OpenApiExample) mediaType.Examples["ValidationProblem"];
        var body = example.Value.Should().BeOfType<JsonObject>().Subject;
        var exampleErrors = body["errors"].Should().BeOfType<JsonArray>().Subject;
        exampleErrors.ToJsonString().Should().Contain("\"lowerBoundary\":1");
        exampleErrors.ToJsonString().Should().Contain("\"upperBoundary\":5");
        exampleErrors.ToJsonString().Should().Contain("\"minLength\":10");
        exampleErrors.ToJsonString().Should().Contain("\"maxLength\":1000");
        exampleErrors.ToJsonString().Should().Contain("\"message\":\"id must not be empty\"");
    }

    [Fact]
    public async Task MvcTwoContractAttribute_ShouldMatchEquivalentMinimalApiCustomization()
    {
        await using var minimalApp = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapPost("/generated-validation/minimal-customized", static () => Results.BadRequest())
                   .WithName("GeneratedValidationMinimalCustomized")
                   .ProducesPortableValidationProblemFor<GeneratedRatingValidator>(
                        StatusCodes.Status422UnprocessableEntity,
                        configure: GeneratedValidationMvcEndpointContract.ConfigurePortableValidationOpenApi
                    );
            }
        );
        await using var mvcApp = ValidationOpenApiDocumentTestUtilities.CreateMvcApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            controllers => controllers.AddApplicationPart(typeof(GeneratedValidationOpenApiIntegrationTests).Assembly)
        );

        var minimalDocument = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(minimalApp);
        var mvcDocument = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(mvcApp);

        var minimalEnvelope = GetResponseEnvelope(minimalDocument, "/generated-validation/minimal-customized");
        var mvcEnvelope = GetResponseEnvelope(mvcDocument, "/generated-validation/mvc-customized");
        var minimalErrors = GetDocumentedErrorItems(minimalEnvelope);
        var mvcErrors = GetDocumentedErrorItems(mvcEnvelope);

        minimalEnvelope.Properties.Should().ContainKey("errorDetails");
        mvcEnvelope.Properties.Should().ContainKey("errorDetails");
        minimalErrors.AnyOf.Should().NotBeNull();
        minimalErrors.OneOf.Should().BeNull();
        mvcErrors.AnyOf.Should().NotBeNull();
        mvcErrors.OneOf.Should().BeNull();

        NormalizeSchemaIds(GetDocumentedErrorSchemaIds(minimalDocument, "/generated-validation/minimal-customized"))
           .Should()
           .BeEquivalentTo(
                NormalizeSchemaIds(GetDocumentedErrorSchemaIds(mvcDocument, "/generated-validation/mvc-customized"))
            );

        GetTopLevelMetadataPropertyNames(minimalDocument, "/generated-validation/minimal-customized")
           .Should()
           .BeEquivalentTo(GetTopLevelMetadataPropertyNames(mvcDocument, "/generated-validation/mvc-customized"));
        GetInlineMetadataPropertyNames(
                minimalDocument,
                "/generated-validation/minimal-customized",
                "EndpointCustom"
            )
           .Should()
           .BeEquivalentTo(
                GetInlineMetadataPropertyNames(mvcDocument, "/generated-validation/mvc-customized", "EndpointCustom")
            );

        var minimalExample = GetValidationProblemExample(minimalDocument, "/generated-validation/minimal-customized");
        var mvcExample = GetValidationProblemExample(mvcDocument, "/generated-validation/mvc-customized");

        minimalExample.ToJsonString().Should().Be(mvcExample.ToJsonString());
        minimalExample.ToJsonString().Should().Contain("\"code\":\"EndpointCustom\"");
        minimalExample.ToJsonString().Should().Contain("\"reason\":\"customization\"");
        minimalExample.ToJsonString().Should().Contain("\"code\":\"NotNull\"");
    }

    private static OpenApiSchema GetResponseEnvelope(OpenApiDocument document, string path)
    {
        var response = (OpenApiResponse) document.Paths[path]
           .Operations![HttpMethod.Post]
           .Responses![StatusCodes.Status422UnprocessableEntity.ToString()];
        var schemaReference = (OpenApiSchemaReference) response.Content!["application/problem+json"].Schema!;
        return ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(schemaReference)
        );
    }

    private static OpenApiSchema GetDocumentedErrorItems(OpenApiSchema envelope)
    {
        return (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errorDetails"]).Items!;
    }

    private static string[] GetDocumentedErrorSchemaIds(OpenApiDocument document, string path)
    {
        var errors = GetDocumentedErrorItems(GetResponseEnvelope(document, path));
        return errors.AnyOf!
           .Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .OrderBy(static schemaId => schemaId, StringComparer.Ordinal)
           .ToArray();
    }

    private static string[] NormalizeSchemaIds(IEnumerable<string> schemaIds)
    {
        return schemaIds.Select(NormalizeSchemaId).OrderBy(static schemaId => schemaId, StringComparer.Ordinal)
           .ToArray();
    }

    private static string NormalizeSchemaId(string schemaId)
    {
        if (schemaId.EndsWith("__InRange", StringComparison.Ordinal))
        {
            return "EndpointScoped__InRange";
        }

        if (schemaId.EndsWith("__EndpointCustom", StringComparison.Ordinal))
        {
            return "EndpointScoped__EndpointCustom";
        }

        return schemaId;
    }

    private static string[] GetTopLevelMetadataPropertyNames(OpenApiDocument document, string path)
    {
        var envelope = GetResponseEnvelope(document, path);
        var metadataReference = envelope.Properties!["metadata"].Should().BeOfType<OpenApiSchemaReference>().Subject;
        var metadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(metadataReference)
        );
        return metadata.Properties!.Keys.OrderBy(static propertyName => propertyName, StringComparer.Ordinal).ToArray();
    }

    private static string[] GetInlineMetadataPropertyNames(OpenApiDocument document, string path, string codeSuffix)
    {
        var schemaId = GetDocumentedErrorSchemaIds(document, path)
           .Single(id => id.EndsWith("__" + codeSuffix, StringComparison.Ordinal));
        var metadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(document, schemaId + "__Metadata");
        return metadata.Properties!.Keys.OrderBy(static propertyName => propertyName, StringComparer.Ordinal).ToArray();
    }

    private static JsonObject GetValidationProblemExample(OpenApiDocument document, string path)
    {
        var response = (OpenApiResponse) document.Paths[path]
           .Operations![HttpMethod.Post]
           .Responses![StatusCodes.Status422UnprocessableEntity.ToString()];
        var example = (OpenApiExample) response.Content!["application/problem+json"].Examples!["ValidationProblem"];
        return example.Value.Should().BeOfType<JsonObject>().Subject;
    }
}

public sealed class GeneratedRatingDto
{
    public Guid Id { get; init; }
    public string Comment { get; set; } = "";
    public int Rating { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class GeneratedRatingValidator : Validator<GeneratedRatingDto>
{
    public GeneratedRatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<GeneratedRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        GeneratedRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsInRange(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class GeneratedClientIdentifierDto
{
    public Guid Id { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class GeneratedClientIdentifierValidator : Validator<GeneratedClientIdentifierDto>
{
    public GeneratedClientIdentifierValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<GeneratedClientIdentifierDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        GeneratedClientIdentifierDto dto
    )
    {
        context.Check(dto.Id).IsUuidV7();
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class GeneratedTimestampDto
{
    public DateTime RecordedAt { get; init; }
    public DateTime ScheduledAt { get; init; }
    public DateTime ObservedAt { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class GeneratedTimestampValidator : Validator<GeneratedTimestampDto>
{
    public GeneratedTimestampValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<GeneratedTimestampDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        GeneratedTimestampDto dto
    )
    {
        context.Check(dto.RecordedAt).IsUtc();
        context.Check(dto.ScheduledAt).IsLocal();
        context.Check(dto.ObservedAt).IsUnspecified();
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class GeneratedValidationMvcMetadata
{
    public string TraceId { get; init; } = string.Empty;
}

public sealed class GeneratedValidationMvcEndpointMetadata
{
    public string TraceId { get; init; } = string.Empty;
}

public sealed class GeneratedValidationMvcEndpointInlineMetadata
{
    public string Reason { get; init; } = string.Empty;
}

public sealed class GeneratedValidationMvcEndpointContract : IPortableValidationOpenApiContract
{
    public static void ConfigurePortableValidationOpenApi(PortableValidationProblemOpenApiBuilder builder)
    {
        builder
           .UseFormat(ValidationProblemSerializationFormat.AspNetCoreCompatible)
           .WithMetadata<GeneratedValidationMvcEndpointMetadata>()
           .WithErrorCodes(ValidationErrorCodes.NotNull)
           .WithErrorMetadata<GeneratedValidationMvcEndpointInlineMetadata>("EndpointCustom")
           .WithErrorExample(ValidationErrorCodes.NotNull, "comment", "comment must not be null")
           .WithErrorExample(
                "EndpointCustom",
                "comment",
                "comment has endpoint-local metadata",
                new Dictionary<string, object?> { ["reason"] = "customization" }
            )
           .AllowUnknownErrorCodes();
    }
}

[ApiController]
[Route("generated-validation")]
public sealed class GeneratedValidationOpenApiController : ControllerBase
{
    [HttpPost("mvc")]
    [ProducesPortableValidationProblemFor<GeneratedRatingValidator>(
        StatusCodes.Status422UnprocessableEntity,
        Format = ValidationProblemSerializationFormat.Rich,
        TopLevelMetadataType = typeof(GeneratedValidationMvcMetadata),
        AllowUnknownErrorCodes = true
    )]
    public IActionResult PostRating()
    {
        return Problem();
    }

    [HttpPost("mvc-customized")]
    [ProducesPortableValidationProblemFor<GeneratedRatingValidator, GeneratedValidationMvcEndpointContract>(
        StatusCodes.Status422UnprocessableEntity
    )]
    public IActionResult PostCustomizedRating()
    {
        return Problem();
    }
}
