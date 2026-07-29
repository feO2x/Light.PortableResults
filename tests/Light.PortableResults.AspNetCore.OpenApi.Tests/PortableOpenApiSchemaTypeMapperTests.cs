using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Microsoft.OpenApi;
using Xunit;

namespace Light.PortableResults.AspNetCore.OpenApi.Tests;

public sealed class PortableOpenApiSchemaTypeMapperTests
{
    // The schema column of the vocabulary table in ai-plans/0055-metadata-restructuring.md is normative, and
    // this is its only guard: the validation source generator emits Map<T>() calls rather than keeping a table
    // of its own. Keying the expectations on MetadataKind means declaring a kind fails this test until its
    // schema has been decided, instead of silently degrading to a schema without a type.
    private static readonly
        Dictionary<MetadataKind, (Type ClrType, JsonSchemaType SchemaType, string? Format)> VocabularySchemas =
            new ()
            {
                [MetadataKind.Boolean] = (typeof(bool), JsonSchemaType.Boolean, null),
                [MetadataKind.Int64] = (typeof(long), JsonSchemaType.Integer, null),
                [MetadataKind.Double] = (typeof(double), JsonSchemaType.Number, null),
                [MetadataKind.String] = (typeof(string), JsonSchemaType.String, null),
                [MetadataKind.Decimal] = (typeof(decimal), JsonSchemaType.Number, null),
                [MetadataKind.UInt64] = (typeof(ulong), JsonSchemaType.String, "uint64"),
                [MetadataKind.Single] = (typeof(float), JsonSchemaType.Number, "float"),
                [MetadataKind.Char] = (typeof(char), JsonSchemaType.String, "char"),
                [MetadataKind.DateTime] = (typeof(DateTime), JsonSchemaType.String, "date-time"),
                [MetadataKind.DateTimeOffset] = (typeof(DateTimeOffset), JsonSchemaType.String, "date-time"),
                [MetadataKind.DateOnly] = (typeof(DateOnly), JsonSchemaType.String, "date"),
                [MetadataKind.TimeOnly] = (typeof(TimeOnly), JsonSchemaType.String, "time"),
                [MetadataKind.TimeSpan] = (typeof(TimeSpan), JsonSchemaType.String, "duration"),
                [MetadataKind.Guid] = (typeof(Guid), JsonSchemaType.String, "uuid"),
                [MetadataKind.Uri] = (typeof(Uri), JsonSchemaType.String, "uri-reference")
            };

    // Null has no CLR type of its own, and arrays and objects are not scalar schemas.
    private static readonly MetadataKind[] KindsWithoutAScalarSchema =
        [MetadataKind.Null, MetadataKind.Array, MetadataKind.Object];

    // The smaller integer types have no kind of their own - MetadataValue widens them to Int64 - so their
    // schema has to be the Int64 schema rather than an independently maintained one. ulong is deliberately
    // absent: it is the one integral type with a dedicated kind, because it does not fit into Int64.
    public static TheoryData<Type> IntegerTypesWideningToInt64 =>
        [typeof(int), typeof(short), typeof(byte), typeof(sbyte), typeof(uint), typeof(ushort)];

    public static TheoryData<MetadataKind> VocabularyKinds
    {
        get
        {
            var data = new TheoryData<MetadataKind>();
            foreach (var kind in VocabularySchemas.Keys)
            {
                data.Add(kind);
            }

            return data;
        }
    }

    [Fact]
    public void EveryDeclaredKindShouldHaveASchemaOrAnExplicitExclusion()
    {
        foreach (var kind in Enum.GetValues<MetadataKind>())
        {
            (VocabularySchemas.ContainsKey(kind) || KindsWithoutAScalarSchema.Contains(kind))
               .Should()
               .BeTrue("MetadataKind.{0} needs a row in the vocabulary table or an explicit exclusion", kind);
        }
    }

    [Theory]
    [MemberData(nameof(VocabularyKinds))]
    public void MapShouldMatchTheMetadataVocabulary(MetadataKind kind)
    {
        var (clrType, expectedType, expectedFormat) = VocabularySchemas[kind];

        var schema = PortableOpenApiSchemaTypeMapper.Map(clrType);

        schema.Type.Should().Be(expectedType);
        schema.Format.Should().Be(expectedFormat);
    }

    [Theory]
    [MemberData(nameof(IntegerTypesWideningToInt64))]
    public void MapShouldGiveTheInt64SchemaToTypesThatWidenIntoIt(Type type)
    {
        var (_, expectedType, expectedFormat) = VocabularySchemas[MetadataKind.Int64];

        var schema = PortableOpenApiSchemaTypeMapper.Map(type);

        schema.Type.Should().Be(expectedType);
        schema.Format.Should().Be(expectedFormat);
    }

    [Fact]
    public void GenericMapShouldMatchTheVocabularyToo()
    {
        // Generated validator code calls Map<T>(), so this is the overload that decides the schemas in a
        // published document. It must not drift from the reflection overload.
        var genericMap = typeof(PortableOpenApiSchemaTypeMapper)
           .GetMethods(BindingFlags.Public | BindingFlags.Static)
           .Single(method => method.Name == nameof(PortableOpenApiSchemaTypeMapper.Map) &&
                             method.IsGenericMethodDefinition);

        foreach (var (kind, (clrType, expectedType, expectedFormat)) in VocabularySchemas)
        {
            var schema = (OpenApiSchema) genericMap.MakeGenericMethod(clrType).Invoke(null, parameters: null)!;

            schema.Type.Should().Be(expectedType, "Map<{0}>() maps the {1} kind", clrType.Name, kind);
            schema.Format.Should().Be(expectedFormat, "Map<{0}>() maps the {1} kind", clrType.Name, kind);
        }
    }

    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(TimeSpan?))]
    [InlineData(typeof(Guid?))]
    [InlineData(typeof(DateOnly?))]
    public void MapShouldUnwrapNullableVocabularyTypes(Type nullableType)
    {
        var underlyingType = Nullable.GetUnderlyingType(nullableType)!;

        var schema = PortableOpenApiSchemaTypeMapper.Map(nullableType);
        var underlyingSchema = PortableOpenApiSchemaTypeMapper.Map(underlyingType);

        schema.Type.Should().Be(underlyingSchema.Type);
        schema.Format.Should().Be(underlyingSchema.Format);
    }

    [Fact]
    public void MapShouldReturnUnconstrainedSchemaForUnknownTypes()
    {
        var schema = PortableOpenApiSchemaTypeMapper.Map(typeof(Dictionary<string, object>));

        schema.Type.Should().BeNull();
        schema.Format.Should().BeNull();
    }
}
