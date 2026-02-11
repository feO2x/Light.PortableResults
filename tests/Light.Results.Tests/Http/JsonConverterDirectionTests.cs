using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading.Json;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;
using Light.Results.Metadata;
using Xunit;

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
