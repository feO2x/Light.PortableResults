using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.SharedJsonSerialization.Writing;

public sealed class SharedWritingExtensionsTests
{
    [Fact]
    public void WriteMetadataValue_ShouldThrow_WhenWriterIsNull()
    {
        var act = () =>
            PortableResults.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataValue(
                null!,
                MetadataValue.Null,
                MetadataValueAnnotation.SerializeInBodies
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteMetadataArray_ShouldThrow_WhenWriterIsNull()
    {
        var act = () =>
            PortableResults.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataArray(
                null!,
                MetadataArray.Empty,
                MetadataValueAnnotation.SerializeInBodies
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteMetadataObject_ShouldThrow_WhenWriterIsNull()
    {
        var act = () =>
            PortableResults.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataObject(
                null!,
                MetadataObject.Empty,
                MetadataValueAnnotation.SerializeInBodies
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteMetadataPropertyAndValue_ShouldThrow_WhenWriterIsNull()
    {
        var act = () =>
            PortableResults.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataPropertyAndValue(
                null!,
                MetadataObject.Empty,
                MetadataValueAnnotation.SerializeInHttpResponseBody
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteMetadataArray_ShouldWriteOnlyValuesWithRequiredAnnotation()
    {
        var array = MetadataArray.Create(
            MetadataValue.FromString("include", MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValue.FromString("skip", MetadataValueAnnotation.SerializeInHttpHeader)
        );

        var json = Serialize(
            writer => writer.WriteMetadataArray(array, MetadataValueAnnotation.SerializeInHttpResponseBody)
        );

        json.Should().Be("[\"include\"]");
    }

    [Fact]
    public void WriteMetadataObject_ShouldWriteOnlyPropertiesWithRequiredAnnotation()
    {
        var metadata = MetadataObject.Create(
            ("included", MetadataValue.FromString("value", MetadataValueAnnotation.SerializeInHttpResponseBody)),
            ("skipped", MetadataValue.FromString("hidden", MetadataValueAnnotation.SerializeInHttpHeader))
        );

        var json = Serialize(
            writer => writer.WriteMetadataObject(metadata, MetadataValueAnnotation.SerializeInHttpResponseBody)
        );

        json.Should().Be("{\"included\":\"value\"}");
    }

    [Fact]
    public void WriteMetadataPropertyAndValue_ShouldWriteMetadataWithAnnotation()
    {
        var metadata = MetadataObject.Create(
            ("traceId", MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInHttpResponseBody)),
            ("secret", MetadataValue.FromString("hidden", MetadataValueAnnotation.SerializeInHttpHeader))
        );

        var json = Serialize(
            writer =>
            {
                writer.WriteStartObject();
                writer.WriteMetadataPropertyAndValue(metadata, MetadataValueAnnotation.SerializeInHttpResponseBody);
                writer.WriteEndObject();
            }
        );

        json.Should().Be("{\"metadata\":{\"traceId\":\"abc\"}}");
    }

    [Fact]
    public void WriteRichErrors_ShouldThrow_WhenWriterIsNull()
    {
        var errors = new Errors(new Error { Message = "Foo" });
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        var act = () => ErrorsExtensions.WriteRichErrors(null!, errors, false, options);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteRichErrors_ShouldThrow_WhenSerializerOptionsIsNull()
    {
        var errors = new Errors(new Error { Message = "Foo" });
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        // ReSharper disable once AccessToDisposedClosure - act is called before disposal
        var act = () => writer.WriteRichErrors(errors, false, null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("serializerOptions");
    }

    [Fact]
    public void GetNormalizedTargetForValidationResponse_ShouldThrow_WhenTargetIsNull()
    {
        var error = new Error { Message = "invalid", Target = null };

        var act = () => error.GetNormalizedTargetForValidationResponse(2);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*index 2*must have the Target property set*");
    }

    [Fact]
    public void GetNormalizedTargetForValidationResponse_ShouldReturnEmptyString_WhenTargetIsWhitespace()
    {
        var error = new Error { Message = "invalid", Target = "   " };

        var result = error.GetNormalizedTargetForValidationResponse(0);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetNormalizedTargetForValidationResponse_ShouldReturnOriginalTarget_WhenTargetIsSet()
    {
        var error = new Error { Message = "invalid", Target = "name" };

        var result = error.GetNormalizedTargetForValidationResponse(0);

        result.Should().Be("name");
    }

    private static string Serialize(Action<Utf8JsonWriter> writeAction)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writeAction(writer);
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
