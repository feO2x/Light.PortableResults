using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class TypedMetadataValueTests
{
    private static readonly DateTime UtcDateTime = new (2026, 7, 26, 13, 45, 30, DateTimeKind.Utc);

    private static readonly DateTimeOffset OffsetDateTime = new (
        2026,
        7,
        26,
        13,
        45,
        30,
        TimeSpan.FromHours(2)
    );

    private static readonly Guid GuidValue = new ("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    private static readonly Uri UriValue = new ("https://example.com/items/42?view=full#summary");

    [Fact]
    public void TypedFactoriesAndAccessorsShouldPreserveExactValues()
    {
        MetadataValue.FromUInt64(ulong.MaxValue).TryGetUInt64(out var uint64).Should().BeTrue();
        uint64.Should().Be(ulong.MaxValue);

        MetadataValue.FromSingle(0.1f).TryGetSingle(out var single).Should().BeTrue();
        single.Should().Be(0.1f);

        MetadataValue.FromChar('ß').TryGetChar(out var character).Should().BeTrue();
        character.Should().Be('ß');

        MetadataValue.FromDateTime(UtcDateTime).TryGetDateTime(out var dateTime).Should().BeTrue();
        dateTime.Should().Be(UtcDateTime);
        dateTime.Kind.Should().Be(DateTimeKind.Utc);

        MetadataValue.FromDateTimeOffset(OffsetDateTime)
           .TryGetDateTimeOffset(out var dateTimeOffset)
           .Should()
           .BeTrue();
        dateTimeOffset.Should().Be(OffsetDateTime);

        var dateOnly = new DateOnly(2026, 7, 26);
        MetadataValue.FromDateOnly(dateOnly).TryGetDateOnly(out var storedDateOnly).Should().BeTrue();
        storedDateOnly.Should().Be(dateOnly);

        var timeOnly = new TimeOnly(13, 45, 30, 123);
        MetadataValue.FromTimeOnly(timeOnly).TryGetTimeOnly(out var storedTimeOnly).Should().BeTrue();
        storedTimeOnly.Should().Be(timeOnly);

        var timeSpan = TimeSpan.FromDays(2) + TimeSpan.FromMilliseconds(125);
        MetadataValue.FromTimeSpan(timeSpan).TryGetTimeSpan(out var storedTimeSpan).Should().BeTrue();
        storedTimeSpan.Should().Be(timeSpan);

        MetadataValue.FromGuid(GuidValue).TryGetGuid(out var guid).Should().BeTrue();
        guid.Should().Be(GuidValue);

        MetadataValue.FromUri(UriValue).TryGetUri(out var uri).Should().BeTrue();
        uri.Should().BeSameAs(UriValue);
    }

    [Fact]
    public void TypedImplicitConversionsShouldUseDedicatedKinds()
    {
        MetadataValue uint64 = ulong.MaxValue;
        MetadataValue single = 0.1f;
        MetadataValue character = 'x';
        MetadataValue dateTime = UtcDateTime;
        MetadataValue dateTimeOffset = OffsetDateTime;
        MetadataValue dateOnly = new DateOnly(2026, 7, 26);
        MetadataValue timeOnly = new TimeOnly(13, 45, 30);
        MetadataValue timeSpan = TimeSpan.FromSeconds(5);
        MetadataValue guid = GuidValue;
        MetadataValue uri = UriValue;

        new[]
            {
                uint64.Kind,
                single.Kind,
                character.Kind,
                dateTime.Kind,
                dateTimeOffset.Kind,
                dateOnly.Kind,
                timeOnly.Kind,
                timeSpan.Kind,
                guid.Kind,
                uri.Kind
            }
           .Should()
           .Equal(
                MetadataKind.UInt64,
                MetadataKind.Single,
                MetadataKind.Char,
                MetadataKind.DateTime,
                MetadataKind.DateTimeOffset,
                MetadataKind.DateOnly,
                MetadataKind.TimeOnly,
                MetadataKind.TimeSpan,
                MetadataKind.Guid,
                MetadataKind.Uri
            );
    }

    [Fact]
    public void IntegralImplicitConversionsShouldRemainUnambiguous()
    {
        MetadataValue byteValue = (byte) 1;
        MetadataValue sbyteValue = (sbyte) -2;
        MetadataValue int16Value = (short) -3;
        MetadataValue uint16Value = (ushort) 4;
        MetadataValue uint32Value = 5U;

        new[] { byteValue, sbyteValue, int16Value, uint16Value, uint32Value }
           .Should()
           .OnlyContain(value => value.Kind == MetadataKind.Int64);
    }

    [Fact]
    public void DateTimeFactoryShouldNormalizeLocalValuesToUtc()
    {
        var local = new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Local);

        var value = MetadataValue.FromDateTime(local);

        value.TryGetDateTime(out var stored).Should().BeTrue();
        stored.Should().Be(local.ToUniversalTime());
        stored.Kind.Should().Be(DateTimeKind.Utc);
        value.ToCanonicalString().Should().EndWith("Z");
    }

    [Fact]
    public void DateTimeFactoryShouldPreserveUnspecifiedValuesWithoutADesignator()
    {
        var unspecified = new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified);

        var value = MetadataValue.FromDateTime(unspecified);

        value.Kind.Should().Be(MetadataKind.DateTime);
        value.TryGetDateTime(out var stored).Should().BeTrue();
        stored.Should().Be(unspecified);
        stored.Kind.Should().Be(DateTimeKind.Unspecified);
        value.ToCanonicalString().Should().Be("2026-07-26T13:45:30");
        Serialize(value).Should().Be("\"2026-07-26T13:45:30\"");
    }

    [Fact]
    public void UnspecifiedAndUtcDateTimesWithTheSameWallClockShouldNotBeEqual()
    {
        var unspecified = MetadataValue.FromDateTime(
            new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified)
        );
        var utc = MetadataValue.FromDateTime(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc));

        unspecified.Should().NotBe(utc);
        unspecified.ToCanonicalString().Should().NotBe(utc.ToCanonicalString());
    }

    [Theory]
    [InlineData("2026-07-26T13:45:30", DateTimeKind.Unspecified)]
    [InlineData("2026-07-26T13:45:30Z", DateTimeKind.Utc)]
    public void DateTimeAccessorShouldAcceptBothCanonicalStringEncodings(string text, DateTimeKind expectedKind)
    {
        MetadataValue.FromString(text).TryGetDateTime(out var value).Should().BeTrue();

        value.Kind.Should().Be(expectedKind);
        MetadataValue.FromDateTime(value).ToCanonicalString().Should().Be(text);
    }

    [Theory]
    [InlineData("2026-07-26T11:45:30+00:00")]
    [InlineData("2026-07-26T11:45:30Z")]
    public void DateTimeOffsetAccessorShouldAcceptBothZeroOffsetDesignators(string text)
    {
        // Only "+00:00" is written by this library, but "Z" is what RFC 3339 emitters produce almost
        // everywhere, so a value degraded to a string on the wire has to read back either way.
        MetadataValue.FromString(text).TryGetDateTimeOffset(out var value).Should().BeTrue();

        value.Should().Be(OffsetDateTime);
        value.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void DateTimeOffsetAccessorShouldRejectTextWithoutAnOffset()
    {
        // Without an offset this is not a point in time. Accepting it would resolve against the reading
        // machine's local offset, so the same text would mean different instants on different hosts.
        MetadataValue.FromString("2026-07-26T13:45:30").TryGetDateTimeOffset(out _).Should().BeFalse();
    }

    [Fact]
    public void DateTimeAccessorShouldRejectTextCarryingANumericOffset()
    {
        // This is the DateTimeOffset encoding. Resolving it into a DateTime would depend on the time zone of
        // whichever machine happens to read the value.
        MetadataValue.FromString("2026-07-26T13:45:30+02:00").TryGetDateTime(out _).Should().BeFalse();
    }

    [Fact]
    public void StringAccessorsShouldAcceptOnlyCanonicalEncodings()
    {
        MetadataValue.FromString(ulong.MaxValue.ToString())
           .TryGetUInt64(out var uint64)
           .Should()
           .BeTrue();
        uint64.Should().Be(ulong.MaxValue);
        MetadataValue.FromString("01").TryGetUInt64(out _).Should().BeFalse();

        MetadataValue.FromString("0.1").TryGetSingle(out var single).Should().BeTrue();
        single.Should().Be(0.1f);
        MetadataValue.FromString("0.10").TryGetSingle(out _).Should().BeFalse();

        MetadataValue.FromString("x").TryGetChar(out var character).Should().BeTrue();
        character.Should().Be('x');
        MetadataValue.FromString("xy").TryGetChar(out _).Should().BeFalse();

        MetadataValue.FromString("2026-07-26T13:45:30Z")
           .TryGetDateTime(out var dateTime)
           .Should()
           .BeTrue();
        dateTime.Should().Be(UtcDateTime);
        MetadataValue.FromString("07/26/2026 13:45:30").TryGetDateTime(out _).Should().BeFalse();

        MetadataValue.FromString("2026-07-26T13:45:30+02:00")
           .TryGetDateTimeOffset(out var dateTimeOffset)
           .Should()
           .BeTrue();
        dateTimeOffset.Should().Be(OffsetDateTime);

        MetadataValue.FromString("2026-07-26").TryGetDateOnly(out var dateOnly).Should().BeTrue();
        dateOnly.Should().Be(new DateOnly(2026, 7, 26));
        MetadataValue.FromString("07/26/2026").TryGetDateOnly(out _).Should().BeFalse();

        MetadataValue.FromString("13:45:30").TryGetTimeOnly(out var timeOnly).Should().BeTrue();
        timeOnly.Should().Be(new TimeOnly(13, 45, 30));
        MetadataValue.FromString("13:45").TryGetTimeOnly(out _).Should().BeFalse();

        MetadataValue.FromString("PT5S").TryGetTimeSpan(out var timeSpan).Should().BeTrue();
        timeSpan.Should().Be(TimeSpan.FromSeconds(5));
        MetadataValue.FromString("00:00:05").TryGetTimeSpan(out _).Should().BeFalse();
        MetadataValue.FromString("not a duration").TryGetTimeSpan(out _).Should().BeFalse();
        // A well-formed duration exceeding the TimeSpan range makes XmlConvert.ToTimeSpan throw
        // OverflowException rather than FormatException - it must not escape the accessor.
        MetadataValue.FromString("P10675200D").TryGetTimeSpan(out _).Should().BeFalse();

        MetadataValue.FromString(GuidValue.ToString("D")).TryGetGuid(out var guid).Should().BeTrue();
        guid.Should().Be(GuidValue);
        MetadataValue.FromString(GuidValue.ToString("D").ToUpperInvariant()).TryGetGuid(out _).Should().BeFalse();

        MetadataValue.FromString(UriValue.OriginalString).TryGetUri(out var uri).Should().BeTrue();
        uri!.OriginalString.Should().Be(UriValue.OriginalString);
        MetadataValue.FromString("hello world").TryGetUri(out _).Should().BeFalse();
    }

    [Fact]
    public void EveryKindShouldSerializeWithItsVocabularyEncoding()
    {
        var values = new (MetadataValue Value, string Json)[]
        {
            (MetadataValue.Null, "null"),
            (MetadataValue.FromBoolean(true), "true"),
            (MetadataValue.FromInt64(-42), "-42"),
            (MetadataValue.FromDouble(5), "5.0"),
            (MetadataValue.FromString("text"), "\"text\""),
            (MetadataValue.FromDecimal(5m), "5"),
            (MetadataValue.FromUInt64(ulong.MaxValue), "\"18446744073709551615\""),
            (MetadataValue.FromSingle(0.1f), "0.1"),
            (MetadataValue.FromChar('x'), "\"x\""),
            (MetadataValue.FromDateTime(UtcDateTime), "\"2026-07-26T13:45:30Z\""),
            (MetadataValue.FromDateTimeOffset(OffsetDateTime), "\"2026-07-26T13:45:30\\u002B02:00\""),
            (MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26)), "\"2026-07-26\""),
            (MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30)), "\"13:45:30\""),
            (MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5)), "\"PT5S\""),
            (MetadataValue.FromGuid(GuidValue), "\"a1b2c3d4-e5f6-7890-abcd-ef1234567890\""),
            (MetadataValue.FromUri(UriValue), "\"https://example.com/items/42?view=full#summary\""),
            (MetadataValue.FromArray(MetadataArray.Create(1, "x")), "[1,\"x\"]"),
            (MetadataValue.FromObject(MetadataObject.Create(("value", 1))), "{\"value\":1}")
        };

        foreach (var (value, expectedJson) in values)
        {
            Serialize(value).Should().Be(expectedJson, "the {0} vocabulary encoding is normative", value.Kind);
        }
    }

    // Int64 and Decimal are written through the writer's own number formatting while Double and Single go
    // through the canonical text so that whole numbers keep their trailing ".0". These are the values where
    // the two paths could diverge - exponent forms, negative zero, and the extremes of each range.
    [Fact]
    public void NumericKindsShouldSerializeIdenticallyAcrossTheirRange()
    {
        var values = new (MetadataValue Value, string Json)[]
        {
            (MetadataValue.FromInt64(0), "0"),
            (MetadataValue.FromInt64(long.MinValue), "-9223372036854775808"),
            (MetadataValue.FromInt64(long.MaxValue), "9223372036854775807"),
            (MetadataValue.FromDecimal(19.50m), "19.50"),
            (MetadataValue.FromDecimal(-0.0m), "0.0"),
            (MetadataValue.FromDecimal(0.0000000001m), "0.0000000001"),
            (MetadataValue.FromDecimal(decimal.MaxValue), "79228162514264337593543950335"),
            (MetadataValue.FromDecimal(decimal.MinValue), "-79228162514264337593543950335"),
            (MetadataValue.FromDouble(0.1), "0.1"),
            (MetadataValue.FromDouble(-0.0), "-0.0"),
            (MetadataValue.FromDouble(1e21), "1E+21"),
            (MetadataValue.FromDouble(5e-324), "5E-324"),
            (MetadataValue.FromDouble(double.MaxValue), "1.7976931348623157E+308"),
            (MetadataValue.FromSingle(1e20f), "1E+20"),
            (MetadataValue.FromSingle(float.Epsilon), "1E-45")
        };

        foreach (var (value, expectedJson) in values)
        {
            Serialize(value).Should().Be(expectedJson, "the {0} number encoding is normative", value.Kind);
        }
    }

    [Fact]
    public void DecimalAccessorShouldConvertFromSingle()
    {
        // Without this conversion, an in-process Single would fail TryGetDecimal while the same value
        // degraded to the string "0.1" on the wire would succeed - the exact inversion the lenient
        // accessors exist to prevent. Narrowing back to float keeps the decimal at 0.1m instead of the
        // widened double 0.10000000149011612m.
        MetadataValue.FromSingle(0.1f).TryGetDecimal(out var value).Should().BeTrue();
        value.Should().Be(0.1m);

        MetadataValue.FromSingle(float.MaxValue).TryGetDecimal(out _).Should().BeFalse();
    }

    [Fact]
    public void SingleShouldWidenToDoubleWithoutChangingItsOwnAccessor()
    {
        var value = MetadataValue.FromSingle(0.1f);

        value.TryGetSingle(out var single).Should().BeTrue();
        value.TryGetDouble(out var @double).Should().BeTrue();

        single.Should().Be(0.1f);
        @double.Should().Be(0.1f);
        @double.Should().NotBe(0.1);
    }

    [Fact]
    public void EqualityHashingAnnotationsAndObjectStorageShouldCoverEveryKind()
    {
        var values = CreateEveryKind();
        using var builder = MetadataObjectBuilder.Create(values.Count);

        foreach (var (key, value) in values)
        {
            var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
                value,
                MetadataValueAnnotation.SerializeInCloudEventsData
            );

            rewritten.Should().Be(value);
            rewritten.GetHashCode().Should().Be(value.GetHashCode());
            rewritten.ToString().Should().NotBeNull();
            builder.Add(key, value);
        }

        var metadata = builder.Build();
        foreach (var (key, value) in values)
        {
            metadata[key].Should().Be(value);
        }

        MetadataValue.FromUri(new Uri("https://EXAMPLE.com/items#fragment"))
           .Should()
           .NotBe(MetadataValue.FromUri(new Uri("https://example.com/items#fragment")));
        MetadataValue.FromGuid(GuidValue).Should().NotBe(MetadataValue.FromString(GuidValue.ToString("D")));
    }

    [Fact]
    public void CanonicalFormatterShouldReportDestinationCapacity()
    {
        Span<char> sufficient = stackalloc char[36];
        Span<char> insufficient = stackalloc char[4];
        var value = MetadataValue.FromGuid(GuidValue);

        value.TryFormatCanonical(sufficient, out var charsWritten).Should().BeTrue();
        charsWritten.Should().Be(36);
        sufficient.ToString().Should().Be(GuidValue.ToString("D"));

        value.TryFormatCanonical(insufficient, out charsWritten).Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ComplexValuesShouldNotHavePrimitiveCanonicalText()
    {
        var arrayAct = () => MetadataValue.FromArray(MetadataArray.Empty).ToCanonicalString();
        var objectAct = () => MetadataValue.FromObject(MetadataObject.Empty).ToCanonicalString();

        arrayAct.Should().Throw<InvalidOperationException>();
        objectAct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MetadataPayloadShouldRejectStandaloneEqualityAndHashing()
    {
        var payloadType = typeof(MetadataValue).Assembly.GetType(
            "Light.PortableResults.Metadata.MetadataPayload",
            throwOnError: true
        )!;
        var payload = Activator.CreateInstance(payloadType)!;

        var equalityAct = () => payload.Equals(payload);
        var hashAct = () => payload.GetHashCode();

        equalityAct.Should().Throw<NotSupportedException>();
        hashAct.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void MalformedInlineTemporalPayloadsShouldBeRejected()
    {
        var dateTime = MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateTime, long.MaxValue);
        // DateTime.FromBinary does not throw for this one - it decodes into a Local value, which FromDateTime
        // never stores and whose rendering would depend on the reading machine's time zone.
        var localDateTime = MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateTime, -1L);
        var dateOnly = MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateOnly, long.MaxValue);
        var timeOnly = MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.TimeOnly, long.MaxValue);

        dateTime.TryGetDateTime(out _).Should().BeFalse();
        localDateTime.TryGetDateTime(out _).Should().BeFalse();
        dateOnly.TryGetDateOnly(out _).Should().BeFalse();
        timeOnly.TryGetTimeOnly(out _).Should().BeFalse();

        var dateTimeFormatAct = () => dateTime.ToCanonicalString();
        var localDateTimeFormatAct = () => localDateTime.ToCanonicalString();
        var dateOnlyFormatAct = () => dateOnly.ToCanonicalString();
        var timeOnlyFormatAct = () => timeOnly.ToCanonicalString();

        dateTimeFormatAct.Should().Throw<InvalidOperationException>();
        localDateTimeFormatAct.Should().Throw<InvalidOperationException>();
        dateOnlyFormatAct.Should().Throw<InvalidOperationException>();
        timeOnlyFormatAct.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TypedAccessorShouldReturnFalseForNonStringWrongKind()
    {
        MetadataValue.Null.TryGetUInt64(out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    private static Dictionary<string, MetadataValue> CreateEveryKind() =>
        new (StringComparer.Ordinal)
        {
            ["null"] = MetadataValue.Null,
            ["boolean"] = MetadataValue.FromBoolean(true),
            ["int64"] = MetadataValue.FromInt64(42),
            ["double"] = MetadataValue.FromDouble(12.5),
            ["string"] = MetadataValue.FromString("text"),
            ["decimal"] = MetadataValue.FromDecimal(19.50m),
            ["uint64"] = MetadataValue.FromUInt64(ulong.MaxValue),
            ["single"] = MetadataValue.FromSingle(0.1f),
            ["char"] = MetadataValue.FromChar('x'),
            ["dateTime"] = MetadataValue.FromDateTime(UtcDateTime),
            ["dateTimeUnspecified"] = MetadataValue.FromDateTime(
                new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified)
            ),
            ["dateTimeOffset"] = MetadataValue.FromDateTimeOffset(OffsetDateTime),
            ["dateOnly"] = MetadataValue.FromDateOnly(new DateOnly(2026, 7, 26)),
            ["timeOnly"] = MetadataValue.FromTimeOnly(new TimeOnly(13, 45, 30)),
            ["timeSpan"] = MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5)),
            ["guid"] = MetadataValue.FromGuid(GuidValue),
            ["uri"] = MetadataValue.FromUri(UriValue),
            ["array"] = MetadataValue.FromArray(MetadataArray.Create(1, "x")),
            ["object"] = MetadataValue.FromObject(MetadataObject.Create(("value", 1)))
        };

    private static string Serialize(MetadataValue value)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
