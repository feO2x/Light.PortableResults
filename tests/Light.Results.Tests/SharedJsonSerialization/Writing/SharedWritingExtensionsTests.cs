using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.Results.Tests.SharedJsonSerialization.Writing;

public sealed class SharedWritingExtensionsTests
{
    [Fact]
    public void WriteMetadataValue_ShouldThrow_WhenWriterIsNull()
    {
        var act = () =>
            Results.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataValue(
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
            Results.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataArray(
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
            Results.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataObject(
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
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        var act = () =>
            Results.SharedJsonSerialization.Writing.MetadataExtensions.WriteMetadataPropertyAndValue(
                null!,
                MetadataObject.Empty,
                options
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("writer");
    }

    [Fact]
    public void WriteMetadataPropertyAndValue_ShouldThrow_WhenSerializerOptionsIsNull()
    {
        var act = () =>
            Serialize(
                writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteMetadataPropertyAndValue(MetadataObject.Empty, null!);
                    writer.WriteEndObject();
                }
            );

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("serializerOptions");
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
    public void WriteMetadataPropertyAndValue_ShouldThrow_WhenMetadataTypeInfoIsMissing()
    {
        var metadata = MetadataObject.Create(("traceId", MetadataValue.FromString("abc")));
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

        var act = () =>
            Serialize(
                writer =>
                {
                    writer.WriteStartObject();
                    writer.WriteMetadataPropertyAndValue(metadata, options);
                    writer.WriteEndObject();
                }
            );

        act.Should().Throw<NotSupportedException>();
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
