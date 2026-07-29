using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.PortableResults.Tests.Http.Writing.Headers;

public sealed class DefaultHttpHeaderConversionServiceTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenConvertersAreNull()
    {
        Action act = () => _ = new DefaultHttpHeaderConversionService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PrepareHttpHeader_ShouldUseRegisteredConverter()
    {
        var converter = new TraceIdConverter();
        var converters = new Dictionary<string, HttpHeaderConverter>(StringComparer.OrdinalIgnoreCase)
        {
            ["traceId"] = converter
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        var service = new DefaultHttpHeaderConversionService(converters);
        var metadataValue = MetadataValue.FromString("abc");

        var header = service.PrepareHttpHeader("traceId", metadataValue);

        header.Key.Should().Be("X-Trace-Id");
        header.Value.ToString().Should().Be("abc");
    }

    [Fact]
    public void PrepareHttpHeader_ShouldFallbackToMetadataKeyAndStringValue_WhenNoConverterIsRegistered()
    {
        var service = new DefaultHttpHeaderConversionService(
            new Dictionary<string, HttpHeaderConverter>().ToFrozenDictionary()
        );

        var header = service.PrepareHttpHeader("count", MetadataValue.FromInt64(42));

        header.Key.Should().Be("count");
        header.Value.ToString().Should().Be("42");
    }

    [Fact]
    public void PrepareHttpHeader_ShouldFormatDecimalWithoutQuotes()
    {
        var service = new DefaultHttpHeaderConversionService(
            new Dictionary<string, HttpHeaderConverter>().ToFrozenDictionary()
        );

        var header = service.PrepareHttpHeader("price", MetadataValue.FromDecimal(19.50m));

        header.Key.Should().Be("price");
        header.Value.ToString().Should().Be("19.50");
        header.Value.ToString().Should().NotContain("\"");
    }

    [Fact]
    public void PrepareHttpHeader_ShouldUseCanonicalUnquotedTextForEveryPrimitiveKind()
    {
        var service = new DefaultHttpHeaderConversionService(
            new Dictionary<string, HttpHeaderConverter>().ToFrozenDictionary()
        );
        var values = new (MetadataValue Value, string Expected)[]
        {
            (MetadataValue.Null, "null"),
            (MetadataValue.FromBoolean(true), "true"),
            (MetadataValue.FromInt64(42), "42"),
            (MetadataValue.FromDouble(5), "5.0"),
            (MetadataValue.FromString("plain text"), "plain text"),
            (MetadataValue.FromDecimal(19.50m), "19.50"),
            (MetadataValue.FromUInt64(ulong.MaxValue), "18446744073709551615"),
            (MetadataValue.FromSingle(0.1f), "0.1"),
            (MetadataValue.FromChar('x'), "x"),
            (
                MetadataValue.FromDateTime(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc)),
                "2026-07-26T13:45:30Z"
            ),
            (
                MetadataValue.FromDateTimeOffset(
                    new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2))
                ),
                "2026-07-26T13:45:30+02:00"
            ),
            (MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26)), "2026-07-26"),
            (MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30)), "13:45:30"),
            (MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5)), "PT5S"),
            (
                MetadataValue.FromGuid(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")),
                "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            ),
            (MetadataValue.FromUri(new Uri("https://example.com/items/42")), "https://example.com/items/42")
        };

        foreach (var (value, expected) in values)
        {
            var header = service.PrepareHttpHeader("value", value);

            header.Value.ToString().Should().Be(expected, "the {0} header encoding is canonical", value.Kind);
        }
    }

    private sealed class TraceIdConverter : HttpHeaderConverter
    {
        public TraceIdConverter() : base(["traceId"]) { }

        public override KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value)
        {
            value.TryGetString(out var traceId);
            return new KeyValuePair<string, StringValues>("X-Trace-Id", traceId);
        }
    }
}
