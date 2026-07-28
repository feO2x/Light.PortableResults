using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableOpenApiSchemaTypeMapperTests
{
    public static TheoryData<Type, JsonSchemaType, string?> VocabularyTypes =>
        new ()
        {
            { typeof(bool), JsonSchemaType.Boolean, null },
            { typeof(long), JsonSchemaType.Integer, null },
            { typeof(double), JsonSchemaType.Number, null },
            { typeof(string), JsonSchemaType.String, null },
            { typeof(decimal), JsonSchemaType.Number, null },
            { typeof(ulong), JsonSchemaType.String, "uint64" },
            { typeof(float), JsonSchemaType.Number, "float" },
            { typeof(char), JsonSchemaType.String, "char" },
            { typeof(DateTime), JsonSchemaType.String, "date-time" },
            { typeof(DateTimeOffset), JsonSchemaType.String, "date-time" },
            { typeof(DateOnly), JsonSchemaType.String, "date" },
            { typeof(TimeOnly), JsonSchemaType.String, "time" },
            { typeof(TimeSpan), JsonSchemaType.String, "duration" },
            { typeof(Guid), JsonSchemaType.String, "uuid" },
            { typeof(Uri), JsonSchemaType.String, "uri-reference" }
        };

    [Theory]
    [MemberData(nameof(VocabularyTypes))]
    public void MapShouldMatchTheMetadataVocabulary(
        Type type,
        JsonSchemaType expectedType,
        string? expectedFormat
    )
    {
        var schema = PortableOpenApiSchemaTypeMapper.Map(type);

        schema.Type.Should().Be(expectedType);
        schema.Format.Should().Be(expectedFormat);
    }

    [Fact]
    public void MapShouldUnwrapNullableVocabularyTypes()
    {
        var schema = PortableOpenApiSchemaTypeMapper.Map(typeof(TimeSpan?));

        schema.Type.Should().Be(JsonSchemaType.String);
        schema.Format.Should().Be("duration");
    }

    [Fact]
    public void MapShouldReturnUnconstrainedSchemaForUnknownTypes()
    {
        var schema = PortableOpenApiSchemaTypeMapper.Map(typeof(Dictionary<string, object>));

        schema.Type.Should().BeNull();
        schema.Format.Should().BeNull();
    }
}
