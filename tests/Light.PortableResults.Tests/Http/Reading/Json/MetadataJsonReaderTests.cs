using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Http.Reading.Json;

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

    // A JSON number carries no discriminator between a decimal and a double, thus the reader deliberately never
    // produces MetadataKind.Decimal. These tests pin that non-guarantee - decimals do not round-trip as decimals.
    [Theory]
    [InlineData("42", MetadataKind.Int64)]
    [InlineData("-9223372036854775808", MetadataKind.Int64)]
    [InlineData("3.5", MetadataKind.Double)]
    [InlineData("19.99", MetadataKind.Double)]
    [InlineData("1e100", MetadataKind.Double)]
    public void ReadMetadataValue_ShouldNeverProduceDecimalKind(string json, MetadataKind expectedKind)
    {
        var reader = CreateReader(json);

        var value = MetadataJsonReader.ReadMetadataValue(ref reader);

        value.Kind.Should().Be(expectedKind);
    }

    [Fact]
    public void ReadMetadataValue_ShouldReadWrittenDecimalAsDouble()
    {
        var reader = CreateReader(MetadataValue.FromDecimal(19.99m).ToString());

        var value = MetadataJsonReader.ReadMetadataValue(ref reader);

        value.Kind.Should().Be(MetadataKind.Double);
        value.TryGetDecimal(out var decimalValue).Should().BeTrue();
        decimalValue.Should().Be(19.99m);
    }

    [Fact]
    public void ReadMetadataValue_ShouldReadWrittenIntegralDecimalAsInt64()
    {
        var reader = CreateReader(MetadataValue.FromDecimal(20m).ToString());

        var value = MetadataJsonReader.ReadMetadataValue(ref reader);

        value.Kind.Should().Be(MetadataKind.Int64);
        value.TryGetDecimal(out var decimalValue).Should().BeTrue();
        decimalValue.Should().Be(20m);
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
