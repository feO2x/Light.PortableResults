using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Definitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        var genericProblemResponse = (OpenApiResponse) operation.Responses![StatusCodes.Status500InternalServerError.ToString()];
        genericProblemResponse.Content!["application/problem+json"].Examples.Should().BeNullOrEmpty();
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
