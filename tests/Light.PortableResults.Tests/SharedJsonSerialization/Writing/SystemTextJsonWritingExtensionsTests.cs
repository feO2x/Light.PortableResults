using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.SharedJsonSerialization.Writing;

public sealed class SystemTextJsonWritingExtensionsTests
{
    [Fact]
    public void HasKnownPolymorphism_ShouldReturnFalse_ForOpenReferenceTypeWithoutPolymorphism()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var typeInfo = options.GetTypeInfo(typeof(OpenPayload));

        typeInfo.HasKnownPolymorphism().Should().BeFalse();
    }

    [Fact]
    public void HasKnownPolymorphism_ShouldReturnTrue_ForValueType()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var typeInfo = options.GetTypeInfo(typeof(int));

        typeInfo.HasKnownPolymorphism().Should().BeTrue();
    }

    [Fact]
    public void ShouldUseWith_ShouldReturnExpectedResult_ForDifferentRuntimeTypeWithoutKnownPolymorphism()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var typeInfo = options.GetTypeInfo(typeof(OpenPayload));

        typeInfo.ShouldUseWith(typeof(DerivedOpenPayload)).Should().BeFalse();
        typeInfo.ShouldUseWith(typeof(OpenPayload)).Should().BeTrue();
        typeInfo.ShouldUseWith(null).Should().BeTrue();
    }

    [Fact]
    public void WriteGenericValue_ShouldThrow_WhenWriterIsNull()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        var act = () => SystemTextJsonWritingExtensions.WriteGenericValue<string>(null!, "value", options);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteGenericValue_ShouldThrow_WhenOptionsAreNull()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var act = () => writer.WriteGenericValue("value", null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("options");
    }

    [Fact]
    public void WriteGenericValue_ShouldWriteNull_WhenValueIsNull()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteGenericValue<string>(null, options);
        writer.WriteEndObject();
        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("{\"value\":null}");
    }

    [Fact]
    public void WriteGenericValue_ShouldThrow_WhenRuntimeTypeMetadataIsMissing()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = new BasePayloadOnlyResolver() };
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        BasePayload value = new DerivedOpenPayload { Name = "test" };

        var act = () => writer.WriteGenericValue(value, options);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*No JSON serialization metadata was found for type*");
    }

    private sealed class BasePayloadOnlyResolver : IJsonTypeInfoResolver
    {
        private readonly DefaultJsonTypeInfoResolver _innerResolver = new ();

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) =>
            type == typeof(BasePayload) ?
                _innerResolver.GetTypeInfo(type, options) :
                null;
    }

    private class BasePayload;

    private class OpenPayload
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DerivedOpenPayload : BasePayload
    {
        public string Name { get; set; } = string.Empty;
    }
}
