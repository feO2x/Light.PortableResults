using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

// This is the defect that motivated MetadataKind.Decimal: comparison and range rules on decimal-typed values
// used to serialize their metadata as quoted strings, while the OpenAPI document generated for the very same
// endpoint documents them as numbers.
public sealed class DecimalMetadataOpenApiConformanceTests
{
    [Fact]
    public async Task DecimalValidationProblemBody_ShouldConformToGeneratedOpenApiDocument()
    {
        await using var app = CreateApp();
        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        using var httpClient = app.GetTestClient();

        using var response = await httpClient.PostAsync(
            "/decimal-validation",
            content: null,
            TestContext.Current.CancellationToken
        );
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.Should().Contain("\"comparativeValue\":19.99");
        body.Should().Contain("\"lowerBoundary\":9.99");
        body.Should().Contain("\"upperBoundary\":99.99");

        using var jsonDocument = JsonDocument.Parse(body);
        var errors = jsonDocument.RootElement.GetProperty("errors").EnumerateArray().ToArray();

        var greaterThanMetadata = GetMetadata(errors, ValidationErrorCodes.GreaterThan);
        var inRangeMetadata = GetMetadata(errors, ValidationErrorCodes.InRange);

        greaterThanMetadata.GetProperty("comparativeValue").ValueKind.Should().Be(JsonValueKind.Number);
        greaterThanMetadata.GetProperty("comparativeValue").GetDecimal().Should().Be(19.99m);
        inRangeMetadata.GetProperty("lowerBoundary").ValueKind.Should().Be(JsonValueKind.Number);
        inRangeMetadata.GetProperty("upperBoundary").ValueKind.Should().Be(JsonValueKind.Number);

        GetMetadataPropertySchema(document, ValidationErrorCodes.GreaterThan, "comparativeValue")
           .Should()
           .Match<OpenApiSchema>(
                schema => ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(schema, JsonSchemaType.Number)
            );
        GetMetadataPropertySchema(document, ValidationErrorCodes.InRange, "lowerBoundary")
           .Should()
           .Match<OpenApiSchema>(
                schema => ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(schema, JsonSchemaType.Number)
            );
        GetMetadataPropertySchema(document, ValidationErrorCodes.InRange, "upperBoundary")
           .Should()
           .Match<OpenApiSchema>(
                schema => ValidationOpenApiDocumentTestUtilities.SchemaIncludesType(schema, JsonSchemaType.Number)
            );
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();
        builder.Services.AddValidationForPortableResults();
        builder.Services.AddSingleton<DecimalPriceValidator>();
        builder.Services.AddPortableResultsOpenApi(contracts => contracts.RegisterBuiltInValidationErrors());
        builder.Services.Configure<PortableResultsHttpWriteOptions>(
            options => options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
        );
        builder.Services.AddOpenApi();

        var app = builder.Build();
        app.MapPost("/decimal-validation", ValidateFixedPrice)
           .WithName("DecimalValidation")
           .ProducesPortableValidationProblemFor<DecimalPriceValidator>(
                configure: static openApi => openApi.UseFormat(ValidationProblemSerializationFormat.Rich)
            );
        return app;
    }

    private static IResult ValidateFixedPrice(DecimalPriceValidator validator)
    {
        var dto = new DecimalPriceDto { Price = 5.00m, Discount = 199.00m };
        var validationContext = validator.ValidationContextFactory.CreateValidationContext();
        return validator.CheckForErrors(dto, validationContext, out var errorResult) ?
            Result<DecimalPriceDto>.Fail(errorResult.Errors).ToMinimalApiResult() :
            Result<DecimalPriceDto>.Ok(dto).ToMinimalApiResult();
    }

    private static JsonElement GetMetadata(JsonElement[] errors, string errorCode)
    {
        var error = errors.Single(element => element.GetProperty("code").GetString() == errorCode);
        return error.GetProperty("metadata");
    }

    private static OpenApiSchema GetMetadataPropertySchema(
        OpenApiDocument document,
        string errorCode,
        string metadataKey
    )
    {
        var response = (OpenApiResponse) document.Paths["/decimal-validation"]
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
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Single(schemaId => schemaId.EndsWith("__" + errorCode, StringComparison.Ordinal));
        var metadataSchema = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            errorSchemaId + "__Metadata"
        );
        return (OpenApiSchema) metadataSchema.Properties![metadataKey];
    }
}

public sealed class DecimalPriceDto
{
    public decimal Price { get; init; }
    public decimal Discount { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class DecimalPriceValidator : Validator<DecimalPriceDto>
{
    public DecimalPriceValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<DecimalPriceDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        DecimalPriceDto dto
    )
    {
        context.Check(dto.Price).IsGreaterThan(19.99m);
        context.Check(dto.Discount).IsInRange(9.99m, 99.99m);
        return checkpoint.ToValidatedValue(dto);
    }
}
