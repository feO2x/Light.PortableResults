using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Xunit;

namespace Light.PortableResults.AspNetCore.Mvc.Tests.UnitTests;

public sealed class ProducesPortableAttributesTests
{
    public static TheoryData<ProducesResponseTypeAttribute, Type, int, string> AttributeCases { get; } =
        new ()
        {
            {
                new ProducesPortableSuccessResponseAttribute<ContactDto, MetadataObject>(),
                typeof(PortableSuccessResponse<ContactDto, MetadataObject>),
                StatusCodes.Status200OK,
                "application/json"
            },
            {
                new ProducesPortableSuccessResponseAttribute<ContactDto, Dictionary<string, long>>(
                    statusCode: StatusCodes.Status201Created
                ),
                typeof(PortableSuccessResponse<ContactDto, Dictionary<string, long>>),
                StatusCodes.Status201Created,
                "application/json"
            },
            {
                new ProducesPortableProblemAttribute(),
                typeof(PortableProblemDetails),
                StatusCodes.Status500InternalServerError,
                "application/problem+json"
            },
            {
                new ProducesPortableProblemAttribute(statusCode: StatusCodes.Status404NotFound),
                typeof(PortableProblemDetails),
                StatusCodes.Status404NotFound,
                "application/problem+json"
            },
            {
                new ProducesPortableProblemAttribute<MetadataObject, MetadataObject>(
                    statusCode: StatusCodes.Status409Conflict
                ),
                typeof(PortableProblemDetails<MetadataObject, MetadataObject>),
                StatusCodes.Status409Conflict,
                "application/problem+json"
            },
            {
                new ProducesPortableRichValidationProblemAttribute(),
                typeof(PortableRichValidationProblemDetails),
                StatusCodes.Status400BadRequest,
                "application/problem+json"
            },
            {
                new ProducesPortableRichValidationProblemAttribute<MetadataObject, MetadataObject>(
                    statusCode: StatusCodes.Status422UnprocessableEntity
                ),
                typeof(PortableRichValidationProblemDetails<MetadataObject, MetadataObject>),
                StatusCodes.Status422UnprocessableEntity,
                "application/problem+json"
            },
            {
                new ProducesPortableAspNetCoreValidationProblemAttribute(),
                typeof(PortableAspNetCoreValidationProblemDetails),
                StatusCodes.Status400BadRequest,
                "application/problem+json"
            },
            {
                new ProducesPortableAspNetCoreValidationProblemAttribute<MetadataObject, MetadataObject>(
                    statusCode: StatusCodes.Status422UnprocessableEntity
                ),
                typeof(PortableAspNetCoreValidationProblemDetails<MetadataObject, MetadataObject>),
                StatusCodes.Status422UnprocessableEntity,
                "application/problem+json"
            }
        };

    [Theory]
    [MemberData(nameof(AttributeCases))]
    public void AttributeExposesExpectedMetadata(
        ProducesResponseTypeAttribute attribute,
        Type expectedType,
        int expectedStatusCode,
        string expectedContentType
    )
    {
        var contentTypes = new MediaTypeCollection();
        ((IApiResponseMetadataProvider) attribute).SetContentTypes(contentTypes);

        attribute.Type.Should().Be(expectedType);
        attribute.StatusCode.Should().Be(expectedStatusCode);
        contentTypes.Should().Contain(expectedContentType);
    }
}
