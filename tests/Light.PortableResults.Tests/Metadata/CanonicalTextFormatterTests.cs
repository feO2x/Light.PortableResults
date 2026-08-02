using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Text;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class CanonicalTextFormatterTests
{
    private const int AllocationIterations = 1_000;

    public static TheoryData<string, MetadataValue, string> CanonicalValues
    {
        get
        {
            var data = new TheoryData<string, MetadataValue, string>
            {
                { "null", MetadataValue.Null, "null" },
                { "boolean-false", MetadataValue.FromBoolean(false), "false" },
                { "boolean-true", MetadataValue.FromBoolean(true), "true" },
                { "int64-minimum", MetadataValue.FromInt64(long.MinValue), Invariant(long.MinValue) },
                { "int64-maximum", MetadataValue.FromInt64(long.MaxValue), Invariant(long.MaxValue) },
                { "double", MetadataValue.FromDouble(-0.0), "-0.0" },
                { "string-ascii", MetadataValue.FromString("canonical text"), "canonical text" },
                { "string-non-ascii", MetadataValue.FromString("Grüße 日本語 😀"), "Grüße 日本語 😀" },
                { "string-unpaired-high", MetadataValue.FromString("a\uD800b"), "a\uD800b" },
                { "string-unpaired-low", MetadataValue.FromString("a\uDC00b"), "a\uDC00b" },
                { "decimal-scale", MetadataValue.FromDecimal(19.50m), Invariant(19.50m) },
                { "decimal-negative-zero", MetadataValue.FromDecimal(-0.000m), Invariant(-0.000m) },
                { "decimal-minimum", MetadataValue.FromDecimal(decimal.MinValue), Invariant(decimal.MinValue) },
                { "decimal-maximum", MetadataValue.FromDecimal(decimal.MaxValue), Invariant(decimal.MaxValue) },
                {
                    "decimal-smallest-scaled",
                    MetadataValue.FromDecimal(0.0000000000000000000000000001m),
                    Invariant(0.0000000000000000000000000001m)
                },
                { "uint64-maximum", MetadataValue.FromUInt64(ulong.MaxValue), Invariant(ulong.MaxValue) },
                { "single", MetadataValue.FromSingle(float.Epsilon), "1E-45" },
                { "char-ascii", MetadataValue.FromChar('x'), "x" },
                { "char-non-ascii", MetadataValue.FromChar('ß'), "ß" },
                { "char-unpaired", MetadataValue.FromChar('\uD800'), "\uD800" }
            };

            var utcMinimum = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            var utcMaximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
            var unspecified = new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified).AddTicks(1_230_000);
            data.Add("datetime-utc-minimum", MetadataValue.FromDateTime(utcMinimum), Format(utcMinimum));
            data.Add("datetime-utc-maximum", MetadataValue.FromDateTime(utcMaximum), Format(utcMaximum));
            data.Add("datetime-unspecified-fraction", MetadataValue.FromDateTime(unspecified), Format(unspecified));

            var offsetMinimum = DateTimeOffset.MinValue;
            var offsetMaximum = DateTimeOffset.MaxValue;
            var offsetFraction = new DateTimeOffset(
                2026,
                7,
                26,
                13,
                45,
                30,
                TimeSpan.FromHours(-3.5)
            ).AddTicks(1_234_500);
            data.Add("datetimeoffset-minimum", MetadataValue.FromDateTimeOffset(offsetMinimum), Format(offsetMinimum));
            data.Add("datetimeoffset-maximum", MetadataValue.FromDateTimeOffset(offsetMaximum), Format(offsetMaximum));
            data.Add(
                "datetimeoffset-fraction",
                MetadataValue.FromDateTimeOffset(offsetFraction),
                Format(offsetFraction)
            );

#if !TESTING_NETSTANDARD_ASSET
            data.Add("date-minimum", MetadataValue.FromDateOnly(DateOnly.MinValue), Format(DateOnly.MinValue));
            data.Add("date-maximum", MetadataValue.FromDateOnly(DateOnly.MaxValue), Format(DateOnly.MaxValue));
            data.Add("time-minimum", MetadataValue.FromTimeOnly(TimeOnly.MinValue), Format(TimeOnly.MinValue));
            data.Add("time-maximum", MetadataValue.FromTimeOnly(TimeOnly.MaxValue), Format(TimeOnly.MaxValue));
#endif

            var duration = TimeSpan.FromDays(2) +
                           TimeSpan.FromHours(3) +
                           TimeSpan.FromMinutes(4) +
                           TimeSpan.FromSeconds(5) +
                           TimeSpan.FromTicks(600_000);
            data.Add("timespan-zero", MetadataValue.FromTimeSpan(TimeSpan.Zero), XmlConvert.ToString(TimeSpan.Zero));
            data.Add(
                "timespan-days-only",
                MetadataValue.FromTimeSpan(TimeSpan.FromDays(2)),
                XmlConvert.ToString(TimeSpan.FromDays(2))
            );
            data.Add("timespan-components", MetadataValue.FromTimeSpan(duration), XmlConvert.ToString(duration));
            data.Add(
                "timespan-minimum",
                MetadataValue.FromTimeSpan(TimeSpan.MinValue),
                XmlConvert.ToString(TimeSpan.MinValue)
            );
            data.Add(
                "timespan-maximum",
                MetadataValue.FromTimeSpan(TimeSpan.MaxValue),
                XmlConvert.ToString(TimeSpan.MaxValue)
            );

            var guid = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
            data.Add("guid-empty", MetadataValue.FromGuid(Guid.Empty), Guid.Empty.ToString("D"));
            data.Add("guid", MetadataValue.FromGuid(guid), guid.ToString("D"));
            data.Add(
                "uri",
                MetadataValue.FromUri(new Uri("https://example.com/Grüße?q=日本語#😀")),
                "https://example.com/Grüße?q=日本語#😀"
            );

            return data;
        }
    }

    public static TheoryData<MetadataValue> AllocationValues
    {
        get
        {
            var data = new TheoryData<MetadataValue>
            {
                MetadataValue.Null,
                MetadataValue.FromBoolean(true),
                MetadataValue.FromInt64(long.MinValue),
                MetadataValue.FromDouble(double.MaxValue),
                MetadataValue.FromString("Grüße 日本語 😀"),
                MetadataValue.FromString("a\uD800b"),
                MetadataValue.FromDecimal(0.0000000000000000000000000001m),
                MetadataValue.FromUInt64(ulong.MaxValue),
                MetadataValue.FromSingle(float.Epsilon),
                MetadataValue.FromChar('\uD800'),
                MetadataValue.FromDateTime(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)),
                MetadataValue.FromDateTimeOffset(DateTimeOffset.MaxValue)
            };
#if !TESTING_NETSTANDARD_ASSET
            data.Add(MetadataValue.FromDateOnly(DateOnly.MaxValue));
            data.Add(MetadataValue.FromTimeOnly(TimeOnly.MaxValue));
#endif
            data.Add(MetadataValue.FromTimeSpan(TimeSpan.MinValue));
            data.Add(MetadataValue.FromGuid(Guid.Empty));
            data.Add(MetadataValue.FromUri(new Uri("https://example.com/Grüße?q=日本語")));

            return data;
        }
    }

    public static TheoryData<int, string> ValidatorValues
    {
        get
        {
            var data = new TheoryData<int, string>
            {
                { 0, Invariant(long.MinValue) },
                { 1, Invariant(ulong.MaxValue) },
                { 2, "0.1" },
                { 3, "2026-07-26T13:45:30.123Z" },
                { 4, "2026-07-26T13:45:30.123+02:00" }
            };
#if !TESTING_NETSTANDARD_ASSET
            data.Add(5, "2026-07-26");
            data.Add(6, "13:45:30.123");
#endif
            data.Add(7, "P2DT3H4M5.06S");
            data.Add(8, "a1b2c3d4-e5f6-7890-abcd-ef1234567890");

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(CanonicalValues))]
    public void MetadataCanonicalFormattingShouldMatchTheExistingTextInBothEncodings(
        string caseName,
        MetadataValue value,
        string expected
    )
    {
        var expectedUtf8 = Encoding.UTF8.GetBytes(expected);
        var chars = new char[expected.Length];
        var bytes = new byte[expectedUtf8.Length];

        value.TryFormatCanonical(chars, out var charsWritten).Should().BeTrue(caseName);
        value.TryFormatCanonicalUtf8(bytes, out var bytesWritten).Should().BeTrue(caseName);

        charsWritten.Should().Be(expected.Length, caseName);
        bytesWritten.Should().Be(expectedUtf8.Length, caseName);
        chars.AsSpan().SequenceEqual(expected.AsSpan()).Should().BeTrue(caseName);
        bytes.Should().Equal(expectedUtf8, caseName);
        value.ToCanonicalString().Should().Be(expected, caseName);
        if (IsAscii(expected))
        {
            bytesWritten.Should().Be(charsWritten, caseName);
        }
    }

    [Theory]
    [MemberData(nameof(CanonicalValues))]
    public void MetadataCanonicalFormattingShouldBeAtomicWhenCapacityIsOneShort(
        string caseName,
        MetadataValue value,
        string expected
    )
    {
        var expectedUtf8Length = Encoding.UTF8.GetByteCount(expected);
        var chars = CreateFilledArray(Math.Max(expected.Length, 1), '\uA55A');
        var bytes = CreateFilledArray(Math.Max(expectedUtf8Length, 1), (byte) 0xA5);

        value.TryFormatCanonical(chars.AsSpan(0, expected.Length - 1), out var charsWritten)
           .Should()
           .BeFalse(caseName);
        value.TryFormatCanonicalUtf8(bytes.AsSpan(0, expectedUtf8Length - 1), out var bytesWritten)
           .Should()
           .BeFalse(caseName);

        charsWritten.Should().Be(0, caseName);
        bytesWritten.Should().Be(0, caseName);
        chars.Should().OnlyContain(character => character == '\uA55A', caseName);
        bytes.Should().OnlyContain(value => value == 0xA5, caseName);
    }

    [Theory]
    [MemberData(nameof(AllocationValues))]
    public void MetadataCanonicalSpanFormattingShouldNotAllocate(MetadataValue value)
    {
        Span<char> chars = stackalloc char[128];
        Span<byte> bytes = stackalloc byte[256];
        for (var index = 0; index < 100; index++)
        {
            value.TryFormatCanonical(chars, out _);
            value.TryFormatCanonicalUtf8(bytes, out _);
        }

        var minimumAllocations = long.MaxValue;
        for (var sample = 0; sample < 5; sample++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < AllocationIterations; index++)
            {
                value.TryFormatCanonical(chars, out _);
                value.TryFormatCanonicalUtf8(bytes, out _);
            }

            minimumAllocations = Math.Min(
                minimumAllocations,
                GC.GetAllocatedBytesForCurrentThread() - before
            );
        }

        minimumAllocations.Should().Be(0, value.Kind.ToString());
    }

    [Theory]
    [MemberData(nameof(ValidatorValues))]
    public void CanonicalRoundTripValidatorsShouldNotAllocateCandidateText(int validator, string text)
    {
        var value = MetadataValue.FromString(text);
        for (var index = 0; index < 100; index++)
        {
            Validate(value, validator).Should().BeTrue();
        }

        var minimumAllocations = long.MaxValue;
        var allValid = true;
        for (var sample = 0; sample < 5; sample++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < AllocationIterations; index++)
            {
                allValid &= Validate(value, validator);
            }

            minimumAllocations = Math.Min(
                minimumAllocations,
                GC.GetAllocatedBytesForCurrentThread() - before
            );
        }

        allValid.Should().BeTrue();
        minimumAllocations.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ExistingAllocationFreeCanonicalStringsShouldReuseTheirText(int caseIndex)
    {
        var value = caseIndex switch
        {
            0 => MetadataValue.Null,
            1 => MetadataValue.FromBoolean(true),
            2 => MetadataValue.FromString("stored string"),
            _ => MetadataValue.FromUri(new Uri("https://example.com/stored"))
        };
        var expected = value.ToCanonicalString();

        for (var index = 0; index < 100; index++)
        {
            _ = value.ToCanonicalString();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var allReferencesMatch = true;
        for (var index = 0; index < AllocationIterations; index++)
        {
            allReferencesMatch &= ReferenceEquals(value.ToCanonicalString(), expected);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();
        allReferencesMatch.Should().BeTrue();
        after.Should().Be(before);
    }

    [Fact]
    public void CanonicalTextFormatterShouldResolveACharLiteralToTheCharOverload()
    {
        Span<char> destination = stackalloc char[CanonicalTextFormatter.MaximumCharLength];

        CanonicalTextFormatter.TryFormat('x', destination, out var charsWritten).Should().BeTrue();

        destination.Slice(0, charsWritten).ToString().Should().Be("x");

        CanonicalTextFormatter.TryFormat(42, destination, out charsWritten).Should().BeTrue();
        destination.Slice(0, charsWritten).ToString().Should().Be("42");
    }

    [Fact]
    public void CanonicalCodeUnitShouldConvertAsciiToBothSupportedCodeUnits()
    {
        const byte value = (byte) 'x';

        CanonicalCodeUnit.FromAscii<byte>(value).Should().Be(value);
        CanonicalCodeUnit.FromAscii<char>(value).Should().Be('x');
    }

    [Fact]
    public void CanonicalCodeUnitShouldRejectUnsupportedCodeUnitTypes()
    {
        var act = () => CanonicalCodeUnit.FromAscii<ushort>((byte) 'x');

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void CanonicalTextFormatterShouldNormalizeOnlyLocalDateTimes()
    {
        var local = new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Local).AddTicks(1_230_000);
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        Span<char> localText = stackalloc char[CanonicalTextFormatter.MaximumDateTimeLength];
        Span<char> unspecifiedText = stackalloc char[CanonicalTextFormatter.MaximumDateTimeLength];

        CanonicalTextFormatter.TryFormat(local, localText, out var localLength).Should().BeTrue();
        CanonicalTextFormatter.TryFormat(unspecified, unspecifiedText, out var unspecifiedLength).Should().BeTrue();

        localText.Slice(0, localLength).ToString().Should().Be(Format(local.ToUniversalTime()));
        localLength.Should().BeLessThanOrEqualTo(CanonicalTextFormatter.MaximumDateTimeLength);
        unspecifiedText.Slice(0, unspecifiedLength).ToString().Should().Be(Format(unspecified));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(CanonicalTextFormatter.MaximumDayNumber + 1)]
    public void CanonicalDateFormatterShouldRejectInvalidDayNumbers(int dayNumber)
    {
        var chars = CreateFilledArray(CanonicalTextFormatter.MaximumDateLength, '\uA55A');
        var bytes = CreateFilledArray(CanonicalTextFormatter.MaximumDateLength, (byte) 0xA5);

        var charAct = () => CanonicalTextFormatter.TryFormatDate(dayNumber, chars, out _);
        var byteAct = () => CanonicalTextFormatter.TryFormatDateUtf8(dayNumber, bytes, out _);

        charAct.Should().Throw<ArgumentOutOfRangeException>();
        byteAct.Should().Throw<ArgumentOutOfRangeException>();
        chars.Should().OnlyContain(value => value == '\uA55A');
        bytes.Should().OnlyContain(value => value == 0xA5);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(CanonicalTextFormatter.MaximumTimeOfDayTicks + 1)]
    public void CanonicalTimeFormatterShouldRejectInvalidTicks(long ticks)
    {
        var chars = CreateFilledArray(CanonicalTextFormatter.MaximumTimeLength, '\uA55A');
        var bytes = CreateFilledArray(CanonicalTextFormatter.MaximumTimeLength, (byte) 0xA5);

        var charAct = () => CanonicalTextFormatter.TryFormatTime(ticks, chars, out _);
        var byteAct = () => CanonicalTextFormatter.TryFormatTimeUtf8(ticks, bytes, out _);

        charAct.Should().Throw<ArgumentOutOfRangeException>();
        byteAct.Should().Throw<ArgumentOutOfRangeException>();
        chars.Should().OnlyContain(value => value == '\uA55A');
        bytes.Should().OnlyContain(value => value == 0xA5);
    }

    [Fact]
    public void ComplexAndMalformedMetadataShouldThrowInBothEncodings()
    {
        var values = new List<MetadataValue>
        {
            MetadataValue.FromArray(MetadataArray.Empty),
            MetadataValue.FromObject(MetadataObject.Empty),
            MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateTime, long.MaxValue),
            MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateTime, -1L),
            MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateOnly, long.MaxValue),
            MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.TimeOnly, long.MaxValue)
        };

        foreach (var value in values)
        {
            var chars = new char[MetadataValue.MaximumPrimitiveCanonicalLength];
            var bytes = new byte[MetadataValue.MaximumPrimitiveCanonicalLength];
            var charAct = () => value.TryFormatCanonical(chars, out _);
            var byteAct = () => value.TryFormatCanonicalUtf8(bytes, out _);

            charAct.Should().Throw<InvalidOperationException>(value.Kind.ToString());
            byteAct.Should().Throw<InvalidOperationException>(value.Kind.ToString());
        }
    }

    [Fact]
    public void LegacyDecimalFieldOrderShouldMatchTheContractualBits()
    {
        var value = new decimal(
            lo: 0x11111111,
            mid: 0x22222222,
            hi: 0x33333333,
            isNegative: true,
            scale: 5
        );
        var expected = decimal.GetBits(value);
        ref var layout = ref Unsafe.As<decimal, DecimalLayout>(ref value);

        new[] { layout.Low, layout.Middle, layout.High, layout.Flags }
           .Should()
           .Equal((uint) expected[0], (uint) expected[1], (uint) expected[2], (uint) expected[3]);
    }

    private static bool IsAscii(string text)
    {
        foreach (var character in text)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Validate(MetadataValue value, int validator) =>
        validator switch
        {
            0 => value.TryGetInt64(out _),
            1 => value.TryGetUInt64(out _),
            2 => value.TryGetSingle(out _),
            3 => value.TryGetDateTime(out _),
            4 => value.TryGetDateTimeOffset(out _),
#if !TESTING_NETSTANDARD_ASSET
            5 => value.TryGetDateOnly(out _),
            6 => value.TryGetTimeOnly(out _),
#endif
            7 => value.TryGetTimeSpan(out _),
            _ => value.TryGetGuid(out _)
        };

    private static string Invariant<T>(T value)
        where T : IFormattable =>
        value.ToString(null, CultureInfo.InvariantCulture);

    private static string Format(DateTime value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture);

    private static string Format(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);

    private static T[] CreateFilledArray<T>(int length, T value)
    {
        var array = new T[length];
        Array.Fill(array, value);
        return array;
    }

#pragma warning disable CS0649 // Fields are populated by reinterpreting decimal storage via Unsafe.As.
    private struct DecimalLayout
    {
        public uint Flags;
        public uint High;
        public uint Low;
        public uint Middle;
    }
#pragma warning restore CS0649

#if !TESTING_NETSTANDARD_ASSET
    private static string Format(DateOnly value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Format(TimeOnly value) =>
        value.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
#endif
}
