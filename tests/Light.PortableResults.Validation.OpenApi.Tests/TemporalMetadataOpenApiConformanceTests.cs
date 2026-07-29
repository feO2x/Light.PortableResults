using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation.Definitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class TemporalMetadataOpenApiConformanceTests
{
    [Fact]
    public async Task DateTimeValidationProblemBodyShouldConformToGeneratedOpenApiDocument()
    {
        await using var app = CreateApp();
        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        using var httpClient = app.GetTestClient();

        using var response = await httpClient.PostAsync(
            "/temporal-validation",
            content: null,
            TestContext.Current.CancellationToken
        );
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var jsonDocument = JsonDocument.Parse(body);
        var error = jsonDocument.RootElement
           .GetProperty("errors")
           .EnumerateArray()
           .Single(element => element.GetProperty("code").GetString() == ValidationErrorCodes.InRange);
        var metadata = error.GetProperty("metadata");

        metadata.GetProperty("lowerBoundary").ValueKind.Should().Be(JsonValueKind.String);
        metadata.GetProperty("lowerBoundary").GetString().Should().Be("2026-07-26T13:45:30Z");
        metadata.GetProperty("upperBoundary").GetString().Should().Be("2026-07-27T13:45:30Z");

        var schema = GetMetadataPropertySchema(
            document,
            ValidationErrorCodes.InRange,
            ValidationErrorMetadataKeys.LowerBoundary
        );
        ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(schema, JsonSchemaType.String).Should().BeTrue();
        schema.Format.Should().Be("date-time");
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddValidationForPortableResults();
        builder.Services.AddSingleton<TemporalBoundaryValidator>();
        builder.Services.AddPortableResultsOpenApi(contracts => contracts.RegisterBuiltInValidationErrors());
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options => options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
        );
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.MapPost("/temporal-validation", ValidateTimestamp)
           .WithName("TemporalValidation")
           .ProducesPortableValidationProblemFor<TemporalBoundaryValidator>(
                configure: static openApi => openApi.UseFormat(ValidationProblemSerializationFormat.Rich)
            );
        return app;
    }

    private static IResult ValidateTimestamp(TemporalBoundaryValidator validator)
    {
        var dto = new TemporalDto
        {
            Timestamp = new DateTime(2026, 7, 25, 13, 45, 30, DateTimeKind.Utc)
        };
        var validationContext = validator.ValidationContextFactory.CreateValidationContext();
        return validator.CheckForErrors(dto, validationContext, out var errorResult) ?
            Result<TemporalDto>.Fail(errorResult.Errors).ToMinimalApiResult() :
            Result<TemporalDto>.Ok(dto).ToMinimalApiResult();
    }

    private static OpenApiSchema GetMetadataPropertySchema(
        OpenApiDocument document,
        string errorCode,
        string metadataKey
    )
    {
        var response = (OpenApiResponse) document.Paths["/temporal-validation"]
           .Operations![HttpMethod.Post]
           .Responses![StatusCodes.Status400BadRequest.ToString()];
        var envelopeReference = (OpenApiSchemaReference) response.Content!["application/problem+json"].Schema!;
        var envelope = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId(envelopeReference)
        );
        var errorItems = (OpenApiSchema) ((OpenApiSchema) envelope.Properties!["errors"]).Items!;
        var errorSchemaId = errorItems.OneOf!
           .Select(
                static item =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) item)
            )
           .Single(schemaId => schemaId.EndsWith("__" + errorCode, StringComparison.Ordinal));
        var metadataSchema = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            errorSchemaId + "__Metadata"
        );
        return (OpenApiSchema) metadataSchema.Properties![metadataKey];
    }
}

public sealed class TemporalDto
{
    public DateTime Timestamp { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class TemporalBoundaryValidator : Validator<TemporalDto>
{
    private static readonly DateTime LowerBoundary =
        new (2026, 7, 26, 13, 45, 30, DateTimeKind.Utc);

    private static readonly DateTime UpperBoundary =
        new (2026, 7, 27, 13, 45, 30, DateTimeKind.Utc);

    public TemporalBoundaryValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<TemporalDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        TemporalDto dto
    )
    {
        context.Check(dto.Timestamp).IsInRange(LowerBoundary, UpperBoundary);
        return checkpoint.ToValidatedValue(dto);
    }
}
