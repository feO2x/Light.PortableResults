using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.SharedJsonSerialization.Writing;

public sealed class SharedWritingExtensionsTests
{
    private const int AllocationIterations = 1_000;

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

    [Theory]
    [InlineData("19.99", "19.99")]
    [InlineData("19.50", "19.50")]
    [InlineData("-0.0001", "-0.0001")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
    public void WriteMetadataValue_ShouldWriteDecimalAsUnquotedNumber(string input, string expectedJson)
    {
        var value = MetadataValue.FromDecimal(decimal.Parse(input, CultureInfo.InvariantCulture));

        var json = Serialize(
            writer => writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody)
        );

        json.Should().Be(expectedJson);
    }

    [Fact]
    public void WriteMetadataObject_ShouldWriteDecimalPropertyAsUnquotedNumber()
    {
        var metadata = MetadataObject.Create(("comparativeValue", MetadataValue.FromDecimal(19.99m)));

        var json = Serialize(
            writer => writer.WriteMetadataObject(metadata, MetadataValueAnnotation.SerializeInHttpResponseBody)
        );

        json.Should().Be("{\"comparativeValue\":19.99}");
    }

    [Fact]
    public void WriteMetadataArray_ShouldWriteDecimalElementsAsUnquotedNumbers()
    {
        var array = MetadataArray.Create(
            MetadataValue.FromDecimal(9.99m),
            MetadataValue.FromDecimal(99.90m)
        );

        var json = Serialize(
            writer => writer.WriteMetadataArray(array, MetadataValueAnnotation.SerializeInHttpResponseBody)
        );

        json.Should().Be("[9.99,99.90]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextBearingMetadataShouldPreserveTheWriterUtf16RouteBytes(bool useUnsafeEncoder)
    {
        var values = new (MetadataValue Value, string DefaultJson, string UnsafeJson)[]
        {
            (MetadataValue.FromString("Grüße"), "\"Gr\\u00FC\\u00DFe\"", "\"Grüße\""),
            (MetadataValue.FromString("a\uD800b"), "\"a\\uFFFDb\"", "\"a\\uFFFDb\""),
            (MetadataValue.FromChar('\uD800'), "\"\\uFFFD\"", "\"\\uFFFD\"")
        };
        var options = new JsonWriterOptions
        {
            Encoder = useUnsafeEncoder ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping : null
        };

        foreach (var (value, defaultJson, unsafeJson) in values)
        {
            var actual = SerializeToUtf8(
                writer => writer.WriteMetadataValue(
                    value,
                    MetadataValueAnnotation.SerializeInHttpResponseBody
                ),
                options
            );

            actual.Should().Equal(
                Encoding.UTF8.GetBytes(useUnsafeEncoder ? unsafeJson : defaultJson),
                value.Kind.ToString()
            );
        }
    }

    [Fact]
    public void CanonicallyFormattedJsonValuesShouldNotAllocateStrings()
    {
        var values = new[]
        {
            MetadataValue.FromDouble(36_028_797_018_963_968.0),
            MetadataValue.FromSingle(123_456_789f),
            MetadataValue.FromUInt64(ulong.MaxValue),
            MetadataValue.FromDateTime(
                new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc).AddTicks(1_234_567)
            ),
            MetadataValue.FromTimeSpan(TimeSpan.MaxValue),
            MetadataValue.FromGuid(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")),
            MetadataValue.FromString("Grüße 日本語 😀"),
            MetadataValue.FromChar('ß'),
            MetadataValue.FromUri(new Uri("https://example.com/Grüße?q=日本語"))
        };

        foreach (var value in values)
        {
            MeasureWriterAllocations(value).Should().Be(0, value.Kind.ToString());
        }
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

    private static byte[] SerializeToUtf8(
        Action<Utf8JsonWriter> writeAction,
        JsonWriterOptions options
    )
    {
        var output = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(output, options);
        writeAction(writer);
        writer.Flush();
        return output.WrittenSpan.ToArray();
    }

    private static long MeasureWriterAllocations(MetadataValue value)
    {
        var minimumAllocations = long.MaxValue;
        for (var sample = 0; sample < 5; sample++)
        {
            var output = new ArrayBufferWriter<byte>(1024 * 1024);
            using var writer = new Utf8JsonWriter(output);
            writer.WriteStartArray();
            for (var index = 0; index < 100; index++)
            {
                writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < AllocationIterations; index++)
            {
                writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
            }

            minimumAllocations = Math.Min(
                minimumAllocations,
                GC.GetAllocatedBytesForCurrentThread() - before
            );
        }

        return minimumAllocations;
    }
}
