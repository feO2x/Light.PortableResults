using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Writing.Json;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Writing;

[Collection(AllocationMeasurementCollection.Name)]
public sealed class JsonCloudEventsExtensionsTests
{
    private const int AllocationSampleCount = 5;

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldThrowWhenWriterIsNull()
    {
        Utf8JsonWriter writer = null!;

        var act = () => writer.WriteCloudEventsExtensionAttribute("attempt", MetadataValue.FromInt64(1));

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "writer");
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldRejectNullNameBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute(null!, MetadataValue.FromInt64(1));

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "attributeName");
        writer.BytesPending.Should().Be(bytesPending);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void WriteCloudEventsExtensionAttributeShouldRejectBlankNameBeforeWriting(string attributeName)
    {
        AssertNameRejectedBeforeWriting(attributeName, "*empty or whitespace*");
    }

    [Theory]
    [InlineData("Traceid")]
    [InlineData("trace-id")]
    [InlineData("trace_id")]
    [InlineData("tracé")]
    public void WriteCloudEventsExtensionAttributeShouldRejectNonLowercaseAlphanumericNameBeforeWriting(
        string attributeName
    )
    {
        AssertNameRejectedBeforeWriting(attributeName, "*lowercase alphanumeric*");
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldRejectReservedNameBeforeWriting()
    {
        AssertNameRejectedBeforeWriting("lproutcome", "*reserved*");
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldRejectStandardNameBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute("type", MetadataValue.FromInt64(1));

        act.Should().Throw<ArgumentException>().WithMessage("*standard*");
        writer.BytesPending.Should().Be(bytesPending);
    }

    [Theory]
    [InlineData('\u0001', "U+0001")]
    [InlineData('\u0080', "U+0080")]
    [InlineData('\uD800', "U+D800")]
    [InlineData('\uDC00', "U+DC00")]
    [InlineData('\uFDD0', "U+FDD0")]
    public void WriteCloudEventsExtensionAttributeShouldRejectInvalidCharBeforeWriting(
        char character,
        string expectedCodePoint
    )
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute(
            "attempt",
            MetadataValue.FromChar(character)
        );

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"*attempt*{expectedCodePoint}*");
        writer.BytesPending.Should().Be(bytesPending);
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldAcceptSurrogatePairAndNonAsciiText()
    {
        using var document = WriteAttribute(MetadataValue.FromString("Grüße 日本語 😀"));

        document.RootElement.GetProperty("attribute").GetString().Should().Be("Grüße 日本語 😀");
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldValidateUriOriginalText()
    {
        var uri = new Uri("https://example.com/invalid\uFDD0text");
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute(
            "location",
            MetadataValue.FromUri(uri)
        );

        act.Should().Throw<InvalidOperationException>().WithMessage("*location*U+FDD0*");
        writer.BytesPending.Should().Be(bytesPending);
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldNameComplexKindBeforeWriting()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute(
            "attribute",
            MetadataValue.FromArray(MetadataArray.Empty)
        );

        act.Should().Throw<InvalidOperationException>().WithMessage("*Array*");
        writer.BytesPending.Should().Be(bytesPending);
    }

    [Fact]
    public void WriteCloudEventsExtensionAttributeShouldOmitNullAtomically()
    {
        using var withNull = WriteAttribute(MetadataValue.Null);
        using var withoutAttribute = JsonDocument.Parse("{}");

        withNull.RootElement.GetRawText().Should().Be(withoutAttribute.RootElement.GetRawText());
    }

    [Fact]
    public void FloatingPointExtensionAttributeWritingShouldAllocateNothingAfterWarmup()
    {
        var doubleValue = MetadataValue.FromDouble(36_028_797_018_963_968.0);
        var singleValue = MetadataValue.FromSingle(123_456_789f);

        MeasureMinimumWriterAllocations(doubleValue).Should().Be(0);
        MeasureMinimumWriterAllocations(singleValue).Should().Be(0);
    }

    [Fact]
    public void OtherStringMappedKindsShouldNotAllocateCanonicalText()
    {
        var values = new[]
        {
            MetadataValue.FromInt64((long) int.MaxValue + 1),
            MetadataValue.FromString(new string('x', 64)),
            MetadataValue.FromDecimal(decimal.MinValue),
            MetadataValue.FromUInt64(ulong.MaxValue),
            MetadataValue.FromChar('ß'),
            MetadataValue.FromDateTime(
                new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc).AddTicks(1_234_567)
            ),
            MetadataValue.FromDateTimeOffset(
                new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2)).AddTicks(1_234_567)
            ),
#if !TESTING_NETSTANDARD_ASSET
            MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26)),
            MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30, 123)),
#endif
            MetadataValue.FromTimeSpan(TimeSpan.MaxValue),
            MetadataValue.FromGuid(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")),
            MetadataValue.FromUri(new Uri("https://example.com/a/path/longer/than/thirty-six/characters"))
        };

        foreach (var value in values)
        {
            var canonicalAllocations = MeasureMinimumCanonicalFormattingAllocations(value);
            var writerAllocations = MeasureMinimumWriterAllocations(value);

            canonicalAllocations.Should().Be(0, "formatting {0} is span-based", value.Kind);
            writerAllocations.Should().Be(0, "writing {0} should not materialize canonical text", value.Kind);
        }
    }

    private static void AssertNameRejectedBeforeWriting(string attributeName, string expectedMessage)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        var bytesPending = writer.BytesPending;

        var act = () => writer.WriteCloudEventsExtensionAttribute(
            attributeName,
            MetadataValue.FromInt64(1)
        );

        act.Should().Throw<ArgumentException>().WithMessage(expectedMessage);
        writer.BytesPending.Should().Be(bytesPending);
    }

    private static JsonDocument WriteAttribute(MetadataValue value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteCloudEventsExtensionAttribute("attribute", value);
            writer.WriteEndObject();
            writer.Flush();
        }

        return JsonDocument.Parse(stream.ToArray());
    }

    // A persistent per-write allocation appears in every sample; taking the minimum filters out
    // occasional runtime or test-host allocations recorded on the current thread.
    private static long MeasureMinimumCanonicalFormattingAllocations(MetadataValue value)
    {
        var minimumAllocations = long.MaxValue;
        for (var sample = 0; sample < AllocationSampleCount; sample++)
        {
            minimumAllocations = Math.Min(
                minimumAllocations,
                MeasureCanonicalFormattingAllocations(value)
            );
        }

        return minimumAllocations;
    }

    private static long MeasureMinimumWriterAllocations(MetadataValue value)
    {
        var minimumAllocations = long.MaxValue;
        for (var sample = 0; sample < AllocationSampleCount; sample++)
        {
            minimumAllocations = Math.Min(minimumAllocations, MeasureWriterAllocations(value));
        }

        return minimumAllocations;
    }

    private static long MeasureCanonicalFormattingAllocations(MetadataValue value)
    {
        Span<char> destination = stackalloc char[36];
        for (var index = 0; index < 100; index++)
        {
            value.TryFormatCanonical(destination, out _);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            value.TryFormatCanonical(destination, out _);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static long MeasureWriterAllocations(MetadataValue value)
    {
        var output = new ArrayBufferWriter<byte>(1024 * 1024);
        using var writer = new Utf8JsonWriter(output);
        writer.WriteStartObject();
        for (var index = 0; index < 100; index++)
        {
            writer.WriteCloudEventsExtensionAttribute("attribute", value);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            writer.WriteCloudEventsExtensionAttribute("attribute", value);
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
