using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Json;

public sealed class MetadataJsonReaderTests
{
    [Fact]
    public void ReadMetadataObject_ShouldParseAllSupportedMetadataKinds()
    {
        var reader = CreateReader(
            """
            {
              "n": null,
              "b": true,
              "i": 42,
              "d": 3.5,
              "s": "value",
              "a": [1, "2"],
              "o": { "k": "v" }
            }
            """
        );

        var metadata = MetadataJsonReader.ReadMetadataObject(ref reader);
        var expectedMetadata = MetadataObject.Create(
            ("n", MetadataValue.FromNull()),
            ("b", MetadataValue.FromBoolean(true)),
            ("i", MetadataValue.FromInt64(42)),
            ("d", MetadataValue.FromDouble(3.5)),
            ("s", MetadataValue.FromString("value")),
            (
                "a",
                MetadataValue.FromArray(
                    MetadataArray.Create(
                        MetadataValue.FromInt64(1),
                        MetadataValue.FromString("2")
                    )
                )
            ),
            (
                "o",
                MetadataValue.FromObject(
                    MetadataObject.Create(("k", MetadataValue.FromString("v")))
                )
            )
        );

        metadata.Should().Equal(expectedMetadata);
    }

    [Fact]
    public void ReadMetadataObject_ShouldUseLastValue_ForDuplicateKeys()
    {
        var reader = CreateReader("""{"a":"first","a":"second"}""");

        var metadata = MetadataJsonReader.ReadMetadataObject(ref reader);

        metadata.TryGetString("a", out var value).Should().BeTrue();
        value.Should().Be("second");
    }

    [Fact]
    public void ReadMetadataObject_ShouldReturnEmpty_WhenJsonTokenIsNull()
    {
        var reader = CreateReader("null");

        var metadata = MetadataJsonReader.ReadMetadataObject(ref reader);

        metadata.Count.Should().Be(0);
    }

    [Fact]
    public void ReadMetadataObject_ShouldThrow_WhenTokenIsNotObjectOrNull()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("[]");
                MetadataJsonReader.ReadMetadataObject(ref reader);
            }
        );
    }

    [Fact]
    public void ReadMetadataArray_ShouldThrow_WhenTokenIsNotArray()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("{}");
                MetadataJsonReader.ReadMetadataArray(ref reader);
            }
        );
    }

    [Fact]
    public void ReadMetadataValue_ShouldThrow_WhenInputIsEmpty()
    {
        Assert.ThrowsAny<JsonException>(
            () =>
            {
                var reader = new Utf8JsonReader([]);
                MetadataJsonReader.ReadMetadataValue(ref reader);
            }
        );
    }

    [Fact]
    public void ReadMetadataValue_ShouldThrow_WhenInputIsIncompleteAndNoTokenIsAvailable()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = new Utf8JsonReader(
                    Encoding.UTF8.GetBytes(string.Empty),
                    isFinalBlock: false,
                    state: default
                );
                MetadataJsonReader.ReadMetadataValue(ref reader);
            }
        );
    }

    [Fact]
    public void ReadMetadataValue_ShouldApplyAnnotation_ForPrimitiveValue()
    {
        var reader = CreateReader("true");

        var value = MetadataJsonReader.ReadMetadataValue(ref reader, MetadataValueAnnotation.SerializeInHttpHeader);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
        value.TryGetBoolean(out var boolValue).Should().BeTrue();
        boolValue.Should().BeTrue();
    }

    [Fact]
    public void ReadMetadataValue_ShouldParseFalseBoolean()
    {
        var reader = CreateReader("false");

        var value = MetadataJsonReader.ReadMetadataValue(ref reader);

        value.TryGetBoolean(out var boolValue).Should().BeTrue();
        boolValue.Should().BeFalse();
    }

    [Fact]
    public void ReadMetadataValue_ShouldThrow_OnUnsupportedToken()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("{}");
                reader.Read();
                reader.Read();
                MetadataJsonReader.ReadMetadataValue(ref reader);
            }
        );
    }

    private static Utf8JsonReader CreateReader(string json) => new (Encoding.UTF8.GetBytes(json));
}
