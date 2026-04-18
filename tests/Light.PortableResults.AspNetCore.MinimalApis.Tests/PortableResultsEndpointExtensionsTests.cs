using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

public sealed class PortableResultsEndpointExtensionsTests
{
    public static TheoryData<Action<RouteHandlerBuilder>, Type, int, string> RegistrationCases { get; } = new ()
    {
        {
            builder => builder.ProducesPortableSuccessResponse<ContactDto, MetadataObject>(),
            typeof(PortableSuccessResponse<ContactDto, MetadataObject>),
            StatusCodes.Status200OK,
            "application/json"
        },
        {
            builder => builder.ProducesPortableSuccessResponse<ContactDto, Dictionary<string, long>>(
                statusCode: 201
            ),
            typeof(PortableSuccessResponse<ContactDto, Dictionary<string, long>>),
            StatusCodes.Status201Created,
            "application/json"
        },
        {
            builder => builder.ProducesPortableProblem(),
            typeof(PortableProblemDetails),
            StatusCodes.Status500InternalServerError,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableProblem(statusCode: StatusCodes.Status404NotFound),
            typeof(PortableProblemDetails),
            StatusCodes.Status404NotFound,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableProblem<MetadataObject, MetadataObject>(
                statusCode: StatusCodes.Status409Conflict
            ),
            typeof(PortableProblemDetails<MetadataObject, MetadataObject>),
            StatusCodes.Status409Conflict,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableRichValidationProblem(),
            typeof(PortableRichValidationProblemDetails),
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableRichValidationProblem<MetadataObject, MetadataObject>(
                statusCode: StatusCodes.Status422UnprocessableEntity
            ),
            typeof(PortableRichValidationProblemDetails<MetadataObject, MetadataObject>),
            StatusCodes.Status422UnprocessableEntity,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableAspNetCoreValidationProblem(),
            typeof(PortableAspNetCoreValidationProblemDetails),
            StatusCodes.Status400BadRequest,
            "application/problem+json"
        },
        {
            builder => builder.ProducesPortableAspNetCoreValidationProblem<MetadataObject, MetadataObject>(
                statusCode: StatusCodes.Status422UnprocessableEntity
            ),
            typeof(PortableAspNetCoreValidationProblemDetails<MetadataObject, MetadataObject>),
            StatusCodes.Status422UnprocessableEntity,
            "application/problem+json"
        }
    };

    [Theory]
    [MemberData(nameof(RegistrationCases))]
    public void HelperShouldRegisterExpectedMetadata(
        Action<RouteHandlerBuilder> register,
        Type expectedType,
        int expectedStatusCode,
        string expectedContentType
    )
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        var routeBuilder = app.MapGet("/test", () => "ok");

        register(routeBuilder);

        var endpointRouteBuilder = (IEndpointRouteBuilder) app;
        var endpoint = endpointRouteBuilder.DataSources.Single().Endpoints.OfType<RouteEndpoint>().Single();
        var matches = endpoint.Metadata
           .Where(item => item.GetType().Name == "ProducesResponseTypeMetadata")
           .Select(
                entry => new
                {
                    Type = (Type?) entry.GetType().GetProperty("Type")?.GetValue(entry),
                    StatusCode = (int?) entry.GetType().GetProperty("StatusCode")?.GetValue(entry),
                    ContentTypes = (IEnumerable<string>?) entry
                       .GetType()
                       .GetProperty("ContentTypes")
                      ?.GetValue(entry)
                }
            )
           .ToArray();

        matches
           .Should()
           .ContainSingle(
                entry => entry.Type == expectedType &&
                         entry.StatusCode == expectedStatusCode &&
                         entry.ContentTypes != null &&
                         entry.ContentTypes.Contains(expectedContentType)
            );
    }
}
