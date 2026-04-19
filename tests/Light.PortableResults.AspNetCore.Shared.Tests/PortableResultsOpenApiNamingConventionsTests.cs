using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.AspNetCore.Shared.Tests;

public sealed class PortableResultsOpenApiNamingConventionsTests
{
    public static TheoryData<Type, string> AllObjectGenericArgsCases { get; } =
        new ()
        {
            { typeof(PortableError<object>), "PortableError" },
            { typeof(PortableValidationErrorDetail<object>), "PortableValidationErrorDetail" },
            { typeof(PortableSuccessResponse<object, object>), "PortableSuccessResponse" },
            { typeof(PortableProblemDetails<object, object>), "PortableProblemDetails" },
            {
                typeof(PortableRichValidationProblemDetails<object, object>),
                "PortableRichValidationProblemDetails"
            },
            {
                typeof(PortableAspNetCoreValidationProblemDetails<object, object>),
                "PortableAspNetCoreValidationProblemDetails"
            }
        };

    public static TheoryData<Type> FallthroughCases { get; } =
        new ()
        {
            // Non-object generic args: caller's default naming should apply.
            typeof(PortableError<SomeMetadata>),
            typeof(PortableProblemDetails<SomeMetadata, object>),
            typeof(PortableProblemDetails<object, SomeMetadata>),
            typeof(PortableSuccessResponse<string, SomeMetadata>),
            // Non-generic schema types stay with their default name.
            typeof(PortableError),
            typeof(PortableProblemDetails),
            // Unrelated types are never handled by the helper.
            typeof(string),
            typeof(SomeMetadata)
        };

    [Theory]
    [MemberData(nameof(AllObjectGenericArgsCases))]
    public void TryCreateSchemaReferenceId_ReturnsSimpleNameWhenAllGenericArgsAreObject(
        Type type, string expected
    )
    {
        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, JsonSerializerOptions.Default);

        var result = PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId(typeInfo);

        result.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(FallthroughCases))]
    public void TryCreateSchemaReferenceId_ReturnsNullToSignalFallthrough(Type type)
    {
        var typeInfo = JsonTypeInfo.CreateJsonTypeInfo(type, JsonSerializerOptions.Default);

        var result = PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId(typeInfo);

        result.Should().BeNull();
    }

    [Fact]
    public void TryCreateSchemaReferenceId_ThrowsWhenTypeInfoIsNull()
    {
        Action act = () => PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("typeInfo");
    }

    public sealed class SomeMetadata
    {
        public string? Name { get; init; }
    }
}
