using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation.Definitions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class PortableProblemOpenApiBuilderTests
{
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
        await using var app = CreateTypedHelperApp<DateTime>("InRangeDateTimeProblem");

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var metadata = ValidationOpenApiDocumentTestUtilities.GetSchemaComponent(
            document,
            "PortableError__InRangeDateTimeProblem__400__application_problem_json__InRange__Metadata"
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
    public async Task ProducesPortableProblem_ShouldMixGlobalAndEndpointScopedBuiltInContracts()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            contracts => contracts.RegisterBuiltInValidationErrors(),
            endpoints =>
            {
                endpoints
                   .MapGet("/mixed-problem", static () => Results.Problem())
                   .WithName("MixedProblem")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: x => x
                           .WithErrorCodes(ValidationErrorCodes.NotEmpty, ValidationErrorCodes.LengthInRange)
                           .WithInRangeError<int>()
                    );
            }
        );

        var document = await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        var responseItems = ValidationOpenApiDocumentTestUtilities.GetProblemItems(document, "/mixed-problem");

        responseItems.AnyOf.Should().BeNull();
        responseItems.OneOf!.Select(
                static schema =>
                    ValidationOpenApiDocumentTestUtilities.GetSchemaReferenceId((OpenApiSchemaReference) schema)
            )
           .Should()
           .BeEquivalentTo(
                [
                    "PortableError__NotEmpty",
                    "PortableError__LengthInRange",
                    "PortableError__MixedProblem__400__application_problem_json__InRange"
                ]
            );
    }

    [Fact]
    public async Task TypedHelpers_ShouldBeIdempotentWhenRegisteredTwice()
    {
        await using var app = ValidationOpenApiDocumentTestUtilities.CreateApp(
            _ => { },
            endpoints =>
            {
                endpoints
                   .MapGet("/idempotent-problem", static () => Results.Problem())
                   .WithName("IdempotentProblem")
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: builder =>
                        {
                            builder.WithInRangeError<int>();
                            builder.WithInRangeError<int>();
                        }
                    );
            }
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await ValidationOpenApiDocumentTestUtilities.GetOpenApiDocumentAsync(app);
        await act.Should().NotThrowAsync();
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
                   .ProducesPortableProblem(
                        StatusCodes.Status400BadRequest,
                        configure: builder => ValidationTypedHelperTestData.AddTypedHelper<T>(operationName, builder)
                    );
            }
        );
    }
}
