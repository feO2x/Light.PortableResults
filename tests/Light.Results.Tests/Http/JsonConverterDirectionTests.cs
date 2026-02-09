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
    public void HttpReadResultConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadResultJsonConverter();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, Result.Ok(), options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void HttpReadGenericResultConverter_ShouldThrowOnWrite()
    {
        var converter = new HttpReadResultJsonConverter<int>();
        var options = new JsonSerializerOptions();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => converter.Write(writer, Result<int>.Ok(1), options);

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
    public void ReadConverterFactory_ShouldMatchResultOfTOnly()
    {
        var factory = new HttpReadResultJsonConverterFactory();

        factory.CanConvert(typeof(Result<int>)).Should().BeTrue();
        factory.CanConvert(typeof(Result)).Should().BeFalse();
        factory.CreateConverter(typeof(Result<int>), new JsonSerializerOptions())
           .Should().BeOfType<HttpReadResultJsonConverter<int>>();
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
