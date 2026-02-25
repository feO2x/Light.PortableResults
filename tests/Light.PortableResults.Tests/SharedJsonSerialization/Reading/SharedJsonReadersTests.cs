using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Reading;
using Xunit;

namespace Light.PortableResults.Tests.SharedJsonSerialization.Reading;

public sealed class SharedJsonReadersTests
{
    [Fact]
    public void ReadMetadataObject_ShouldThrow_WhenEncounteringNonPropertyTokenInsideObject()
    {
        var act = ReadMetadataObjectWithComments;

        act.Should().Throw<JsonException>()
           .WithMessage("*Expected property name in metadata object*");
        return;

        static void ReadMetadataObjectWithComments()
        {
            var reader = CreateReaderWithComments("{/*comment*/\"key\":\"value\"}");
            _ = MetadataJsonReader.ReadMetadataObject(ref reader);
        }
    }

    [Fact]
    public void ReadMetadataObject_ShouldThrow_WhenJsonEndsAfterPropertyName()
    {
        var act = ReadMetadataObjectFromPartialJson;

        act.Should().Throw<JsonException>()
           .WithMessage("*Unexpected end of JSON while reading metadata value*");
        return;

        static void ReadMetadataObjectFromPartialJson()
        {
            var reader = CreatePartialReader("{\"key\":");
            _ = MetadataJsonReader.ReadMetadataObject(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenTokenIsNotStartArray()
    {
        var act = ReadRichErrorsFromObject;

        act.Should().Throw<JsonException>()
           .WithMessage("*errors must be an array*");
        return;

        static void ReadRichErrorsFromObject()
        {
            var reader = CreateReader("{}");
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenErrorObjectContainsNonPropertyToken()
    {
        var act = ReadRichErrorsWithCommentInsideErrorObject;

        act.Should().Throw<JsonException>()
           .WithMessage("*Expected property name in error object*");
        return;

        static void ReadRichErrorsWithCommentInsideErrorObject()
        {
            var reader = CreateReaderWithComments("[{/*comment*/\"message\":\"error\"}]");
            reader.Read().Should().BeTrue();
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenRequiredStringValueIsMissingDueToTruncatedJson()
    {
        var act = ReadRichErrorsWithTruncatedRequiredString;

        act.Should().Throw<JsonException>()
           .WithMessage("*Unexpected end of JSON while reading string value*");
        return;

        static void ReadRichErrorsWithTruncatedRequiredString()
        {
            var reader = CreatePartialReader("[{\"message\":");
            reader.Read().Should().BeTrue();
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenRequiredStringValueIsNotAString()
    {
        var act = ReadRichErrorsWithNonStringMessage;

        act.Should().Throw<JsonException>()
           .WithMessage("*Expected string value*");
        return;

        static void ReadRichErrorsWithNonStringMessage()
        {
            var reader = CreateReader("[{\"message\":5}]");
            reader.Read().Should().BeTrue();
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenOptionalStringValueIsMissingDueToTruncatedJson()
    {
        var act = ReadRichErrorsWithTruncatedOptionalString;

        act.Should().Throw<JsonException>()
           .WithMessage("*Unexpected end of JSON while reading string value*");
        return;

        static void ReadRichErrorsWithTruncatedOptionalString()
        {
            var reader = CreatePartialReader("[{\"message\":\"ok\",\"code\":");
            reader.Read().Should().BeTrue();
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldThrow_WhenOptionalStringValueIsNeitherStringNorNull()
    {
        var act = ReadRichErrorsWithInvalidOptionalString;

        act.Should().Throw<JsonException>()
           .WithMessage("*Expected string or null value*");
        return;

        static void ReadRichErrorsWithInvalidOptionalString()
        {
            var reader = CreateReader("[{\"message\":\"ok\",\"code\":5}]");
            reader.Read().Should().BeTrue();
            _ = SharedResultJsonReader.ReadRichErrors(ref reader);
        }
    }

    [Fact]
    public void ReadRichErrors_ShouldReadErrorWithMetadataAnnotation()
    {
        var reader = CreateReader("[{\"message\":\"m\",\"metadata\":{\"a\":1}}]");
        reader.Read().Should().BeTrue();

        var errors = SharedResultJsonReader.ReadRichErrors(ref reader, MetadataValueAnnotation.SerializeInHttpHeader);

        errors.Count.Should().Be(1);
        errors[0].Metadata.HasValue.Should().BeTrue();
        errors[0].Metadata!.Value.TryGetValue("a", out var metadataValue).Should().BeTrue();
        metadataValue.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
    }

    private static Utf8JsonReader CreateReader(string json) => new (Encoding.UTF8.GetBytes(json));

    private static Utf8JsonReader CreatePartialReader(string json) =>
        new (Encoding.UTF8.GetBytes(json), isFinalBlock: false, state: default);

    private static Utf8JsonReader CreateReaderWithComments(string json) =>
        new (Encoding.UTF8.GetBytes(json), new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
}
