using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading.Json;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;
using Light.Results.Metadata;
using Xunit;
using Module = Light.Results.Http.Reading.Module;

namespace Light.Results.Tests.Http;

public sealed class JsonConverterDirectionTests
{
    [Fact]
    public void HttpReadMetadataObjectConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadMetadataObjectJsonConverter();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, MetadataObject.Empty, options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadMetadataObjectConverter_ShouldReadPayload()
    {
        var converter = new HttpReadMetadataObjectJsonConverter();
        var options = new JsonSerializerOptions();
        var reader = new Utf8JsonReader("""{"trace":"abc"}"""u8);

        var metadata = converter.Read(ref reader, typeof(MetadataObject), options);

        metadata.Should().Equal(MetadataObject.Create(("trace", MetadataValue.FromString("abc"))));
    }

    [Fact]
    public void HttpReadMetadataValueConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadMetadataValueJsonConverter();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, MetadataValue.FromInt64(1), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadMetadataValueConverter_ShouldReadPayload()
    {
        var converter = new HttpReadMetadataValueJsonConverter();
        var options = new JsonSerializerOptions();
        var reader = new Utf8JsonReader("42"u8);

        var value = converter.Read(ref reader, typeof(MetadataValue), options);

        value.TryGetInt64(out var intValue).Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void HttpReadFailurePayloadConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadFailureResultPayloadJsonConverter();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var payload = new HttpReadFailureResultPayload(new Errors(new Error { Message = "failure" }), null);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, payload, options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadSuccessPayloadConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadSuccessResultPayloadJsonConverter();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, new HttpReadSuccessResultPayload(null), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadAutoSuccessPayloadConverter_ShouldReadPayload()
    {
        var converter = new HttpReadAutoSuccessResultPayloadJsonConverter<int>();
        var options = Module.CreateDefaultSerializerOptions();
        var reader = new Utf8JsonReader("""{"value":42,"metadata":{"source":"auto"}}"""u8);

        var payload = converter.Read(ref reader, typeof(HttpReadAutoSuccessResultPayload<int>), options);

        payload.Value.Should().Be(42);
        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("auto"))));
    }

    [Fact]
    public void HttpReadAutoSuccessPayloadConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadAutoSuccessResultPayloadJsonConverter<int>();
        var options = new JsonSerializerOptions();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, new HttpReadAutoSuccessResultPayload<int>(42, null), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadBareSuccessPayloadConverter_ShouldReadPayload()
    {
        var converter = new HttpReadBareSuccessResultPayloadJsonConverter<int>();
        var options = Module.CreateDefaultSerializerOptions();
        var reader = new Utf8JsonReader("42"u8);

        var payload = converter.Read(ref reader, typeof(HttpReadBareSuccessResultPayload<int>), options);

        payload.Value.Should().Be(42);
    }

    [Fact]
    public void HttpReadBareSuccessPayloadConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadBareSuccessResultPayloadJsonConverter<int>();
        var options = new JsonSerializerOptions();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, new HttpReadBareSuccessResultPayload<int>(42), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadWrappedSuccessPayloadConverter_ShouldReadPayload()
    {
        var converter = new HttpReadWrappedSuccessResultPayloadJsonConverter<int>();
        var options = Module.CreateDefaultSerializerOptions();
        var reader = new Utf8JsonReader("""{"value":42,"metadata":{"source":"wrapped"}}"""u8);

        var payload = converter.Read(ref reader, typeof(HttpReadWrappedSuccessResultPayload<int>), options);

        payload.Value.Should().Be(42);
        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("wrapped"))));
    }

    [Fact]
    public void HttpReadWrappedSuccessPayloadConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadWrappedSuccessResultPayloadJsonConverter<int>();
        var options = new JsonSerializerOptions();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, new HttpReadWrappedSuccessResultPayload<int>(42, null), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpWriteMetadataObjectConverter_ShouldThrowOnRead()
    {
        var converter = new HttpWriteMetadataObjectJsonConverter();
        var serializerOptions = new JsonSerializerOptions();

        var act = () =>
        {
            var reader = new Utf8JsonReader("{}"u8);
            converter.Read(ref reader, typeof(MetadataObject), serializerOptions);
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpWriteMetadataValueConverter_ShouldThrowOnRead()
    {
        var converter = new HttpWriteMetadataValueJsonConverter();
        var serializerOptions = new JsonSerializerOptions();

        var act = () =>
        {
            var reader = new Utf8JsonReader("{}"u8);
            converter.Read(ref reader, typeof(MetadataValue), serializerOptions);
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpWriteResultConverter_ShouldThrowOnRead()
    {
        var writeOptions = new LightResultsHttpWriteOptions();
        var converter = new HttpWriteResultJsonConverter(writeOptions);
        var serializerOptions = new JsonSerializerOptions();

        var act = () =>
        {
            var reader = new Utf8JsonReader("{}"u8);
            converter.Read(ref reader, typeof(Result), serializerOptions);
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpWriteGenericResultConverter_ShouldThrowOnRead()
    {
        var writeOptions = new LightResultsHttpWriteOptions();
        var converter = new HttpWriteResultJsonConverter<int>(writeOptions);
        var serializerOptions = new JsonSerializerOptions();

        var act = () =>
        {
            var reader = new Utf8JsonReader("{}"u8);
            converter.Read(ref reader, typeof(Result<int>), serializerOptions);
        };

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void SuccessPayloadConverterFactory_ShouldMatchPayloadTypes()
    {
        var factory = new HttpReadSuccessResultPayloadJsonConverterFactory();

        factory.CanConvert(typeof(HttpReadAutoSuccessResultPayload<int>)).Should().BeTrue();
        factory.CanConvert(typeof(HttpReadBareSuccessResultPayload<int>)).Should().BeTrue();
        factory.CanConvert(typeof(HttpReadWrappedSuccessResultPayload<int>)).Should().BeTrue();
        factory.CanConvert(typeof(HttpReadSuccessResultPayload)).Should().BeFalse();
        factory.CreateConverter(typeof(HttpReadAutoSuccessResultPayload<int>), new JsonSerializerOptions())
           .Should().BeOfType<HttpReadAutoSuccessResultPayloadJsonConverter<int>>();
        factory.CreateConverter(typeof(HttpReadBareSuccessResultPayload<int>), new JsonSerializerOptions())
           .Should().BeOfType<HttpReadBareSuccessResultPayloadJsonConverter<int>>();
        factory.CreateConverter(typeof(HttpReadWrappedSuccessResultPayload<int>), new JsonSerializerOptions())
           .Should().BeOfType<HttpReadWrappedSuccessResultPayloadJsonConverter<int>>();
    }

    [Fact]
    public void WriteConverterFactory_ShouldMatchResultOfTOnly()
    {
        var factory = new HttpWriteResultJsonConverterFactory(new LightResultsHttpWriteOptions());

        factory.CanConvert(typeof(Result<int>)).Should().BeTrue();
        factory.CanConvert(typeof(Result)).Should().BeFalse();
        factory.CreateConverter(typeof(Result<int>), new JsonSerializerOptions())
           .Should().BeOfType<HttpWriteResultJsonConverter<int>>();
    }

    [Fact]
    public void WriteConverterFactory_ShouldThrow_WhenOptionsAreNull()
    {
        Action act = () => _ = new HttpWriteResultJsonConverterFactory(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
