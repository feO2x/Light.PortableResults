using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
using Xunit;

namespace Light.Results.Tests.Http.Writing;

public sealed class HttpResultForWritingSerializationTests
{
    private static readonly ResolvedHttpWriteOptions DefaultOptions =
        new LightResultsHttpWriteOptions().ToResolvedHttpWriteOptions();

    [Fact]
    public void ToResolvedHttpWriteOptions_ShouldCaptureAllFields()
    {
        var mutableOptions = new LightResultsHttpWriteOptions
        {
            ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich,
            MetadataSerializationMode = MetadataSerializationMode.Always,
            FirstErrorCategoryIsLeadingCategory = false
        };

        var resolved = mutableOptions.ToResolvedHttpWriteOptions();

        var expected = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.Rich,
            MetadataSerializationMode.Always,
            null,
            false
        );
        resolved.Should().Be(expected);
    }

    [Fact]
    public void ToHttpResultForWriting_ShouldWrapResultWithResolvedOptions()
    {
        var result = Result.Ok();
        var options = new LightResultsHttpWriteOptions();

        var wrapper = result.ToHttpResultForWriting(options);

        wrapper.Data.Should().Be(result);
        wrapper.ResolvedOptions.Should().Be(options.ToResolvedHttpWriteOptions());
    }

    [Fact]
    public void ToHttpResultForWriting_Generic_ShouldWrapResultWithResolvedOptions()
    {
        var result = Result<int>.Ok(42);
        var options = new LightResultsHttpWriteOptions();

        var wrapper = result.ToHttpResultForWriting(options);

        wrapper.Data.Should().Be(result);
        wrapper.ResolvedOptions.Should().Be(options.ToResolvedHttpWriteOptions());
    }

    [Fact]
    public void ToHttpResultForWriting_ShouldAcceptResolvedOptions()
    {
        var result = Result.Ok();
        var resolved = DefaultOptions;

        var wrapper = result.ToHttpResultForWriting(resolved);

        wrapper.Data.Should().Be(result);
        wrapper.ResolvedOptions.Should().Be(resolved);
    }

    [Fact]
    public void ToHttpResultForWriting_Generic_ShouldAcceptResolvedOptions()
    {
        var result = Result<string>.Ok("hello");
        var resolved = DefaultOptions;

        var wrapper = result.ToHttpResultForWriting(resolved);

        wrapper.Data.Should().Be(result);
        wrapper.ResolvedOptions.Should().Be(resolved);
    }

    [Fact]
    public void Serialize_ValidResult_WithErrorsOnlyMode_ShouldWriteNothing()
    {
        var wrapper = new HttpResultForWriting(Result.Ok(), DefaultOptions);

        var json = Serialize(wrapper);

        json.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ValidResult_WithAlwaysMode_NoMetadata_ShouldWriteNothing()
    {
        var options = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            MetadataSerializationMode.Always,
            null,
            true
        );
        var wrapper = new HttpResultForWriting(Result.Ok(), options);

        var json = Serialize(wrapper);

        json.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_ValidResult_WithAlwaysMode_WithMetadata_ShouldWriteMetadataObject()
    {
        var options = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            MetadataSerializationMode.Always,
            null,
            true
        );
        var metadata = MetadataObject.Create(
            ("traceId", MetadataValue.FromString("abc-123", MetadataValueAnnotation.SerializeInHttpResponseBody))
        );
        var result = Result.Ok(metadata);
        var wrapper = new HttpResultForWriting(result, options);

        var json = Serialize(wrapper);

        json.Should().Be("{\"metadata\":{\"traceId\":\"abc-123\"}}");
    }

    [Fact]
    public void Serialize_InvalidResult_ShouldWriteProblemDetails()
    {
        var result = Result.Fail(new Error { Message = "Something went wrong" });
        var wrapper = new HttpResultForWriting(result, DefaultOptions);

        var json = Serialize(wrapper);

        json.Should().Contain("\"title\":");
        json.Should().Contain("\"status\":");
    }

    [Fact]
    public void Serialize_Generic_ValidResult_WithErrorsOnlyMode_ShouldWriteValueDirectly()
    {
        var result = Result<int>.Ok(42);
        var wrapper = new HttpResultForWriting<int>(result, DefaultOptions);

        var json = Serialize(wrapper);

        json.Should().Be("42");
    }

    [Fact]
    public void Serialize_Generic_ValidResult_WithAlwaysMode_ShouldWrapValueAndMetadata()
    {
        var options = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            MetadataSerializationMode.Always,
            null,
            true
        );
        var metadata = MetadataObject.Create(
            ("traceId", MetadataValue.FromString("abc-123", MetadataValueAnnotation.SerializeInHttpResponseBody))
        );
        var result = Result<int>.Ok(42, metadata);
        var wrapper = new HttpResultForWriting<int>(result, options);

        var json = Serialize(wrapper);

        json.Should().Contain("\"value\":42");
        json.Should().Contain("\"metadata\":{\"traceId\":\"abc-123\"}");
    }

    [Fact]
    public void Serialize_Generic_ValidResult_WithAlwaysMode_NoMetadata_ShouldWrapValueOnly()
    {
        var options = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            MetadataSerializationMode.Always,
            null,
            true
        );
        var result = Result<int>.Ok(42);
        var wrapper = new HttpResultForWriting<int>(result, options);

        var json = Serialize(wrapper);

        json.Should().Be("{\"value\":42}");
    }

    [Fact]
    public void Serialize_Generic_InvalidResult_ShouldWriteProblemDetails()
    {
        var result = Result<int>.Fail(new Error { Message = "Not found" });
        var wrapper = new HttpResultForWriting<int>(result, DefaultOptions);

        var json = Serialize(wrapper);

        json.Should().Contain("\"title\":");
        json.Should().Contain("\"status\":");
    }

    [Fact]
    public void Instance_ShouldReturnSharedConverter()
    {
        var instance1 = HttpResultForWritingJsonConverter.Instance;
        var instance2 = HttpResultForWritingJsonConverter.Instance;

        instance1.Should().BeSameAs(instance2);
    }

    private static string Serialize<T>(T value)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        options.AddDefaultLightResultsHttpWriteJsonConverters();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var typeInfo = options.GetTypeInfo(typeof(T));
        var converter = (JsonConverter<T>) typeInfo.Converter;
        converter.Write(writer, value, options);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
