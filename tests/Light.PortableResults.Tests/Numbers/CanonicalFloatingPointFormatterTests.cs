using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Numbers;
using Xunit;

namespace Light.PortableResults.Tests.Numbers;

public sealed class CanonicalFloatingPointFormatterTests
{
    private const int CorpusSize = 50_000;
    private const int CorpusSeed = 0x5EED_0058;

    private const byte UnwrittenByte = 0xCC;

    private static readonly ForcedDoubleCharFormatter ForceDragon4DoubleChars =
        CreateDoubleDragon4Formatter<char, ForcedDoubleCharFormatter>();

    private static readonly ForcedDoubleByteFormatter ForceDragon4DoubleBytes =
        CreateDoubleDragon4Formatter<byte, ForcedDoubleByteFormatter>();

    private static readonly ForcedSingleCharFormatter ForceDragon4SingleChars =
        CreateSingleDragon4Formatter<char, ForcedSingleCharFormatter>();

    private static readonly ForcedSingleByteFormatter ForceDragon4SingleBytes =
        CreateSingleDragon4Formatter<byte, ForcedSingleByteFormatter>();

    public static TheoryData<ulong, string> DoubleScenarios =>
        new ()
        {
            { 0x3F1A36E2EB1C432DUL, "0.0001" },
            { 0x3EE4F8B588E368F1UL, "1E-05" },
            { 0x4341C37937E08000UL, "10000000000000000.0" },
            { 0x4376345785D8A000UL, "1E+17" },
            { 0x4360000000000000UL, "36028797018963970.0" },
            { 0x8000000000000000UL, "-0.0" },
            { 0x0000000000000002UL, "1E-323" },
            { 0x0000000000000001UL, "5E-324" },
            { 0x7FEFFFFFFFFFFFFFUL, "1.7976931348623157E+308" },
            { 0xFFEFFFFFFFFFFFFFUL, "-1.7976931348623157E+308" },
            { 0x44B52D02C7E14AF6UL, "1E+23" },
            // Boundary-inclusive round-to-even selects the shorter coefficient for this even mantissa.
            { 0xC352BD2668E077C4UL, "-21098088986959630.0" },
            { 0x3FF3C0CA428C59DDUL, "1.23456789012345" },
            { 0x3FF3C0CA428C59F8UL, "1.234567890123456" },
            { 0x3FF3C0CA428C59FBUL, "1.2345678901234567" },
            // Powers of two carry only the implicit mantissa bit, so the distance to the next
            // representable value is twice the distance to the previous one. Dragon4 tracks the two
            // margins separately for them. A random bit-pattern corpus practically never produces a
            // zero fraction, so the unequal-margin arms need named values. 2^55 above covers the
            // whole-number arm; the following cover the fractional-exponent arm, and each one selects
            // a different decimal pre-scaling and denominator-alignment path inside that arm.
            { 0x3FF0000000000000UL, "1.0" },
            { 0x3FE0000000000000UL, "0.5" },
            { 0x4020000000000000UL, "8.0" },
            { 0x3F70000000000000UL, "0.00390625" },
            { 0x0010000000000000UL, "2.2250738585072014E-308" }
        };

    public static TheoryData<uint, string> SingleScenarios =>
        new ()
        {
            { 0x38D1B717U, "0.0001" },
            { 0x3727C5ACU, "1E-05" },
            { 0x4CBEBC20U, "100000000.0" },
            { 0x4E6E6B28U, "1E+09" },
            { 0x4CEB79A3U, "123456790.0" },
            { 0x80000000U, "-0.0" },
            { 0x00000002U, "3E-45" },
            { 0x00000001U, "1E-45" },
            { 0x7F7FFFFFU, "3.4028235E+38" },
            { 0xFF7FFFFFU, "-3.4028235E+38" },
            // Exact value 0.330078125 is halfway after the final 8; the final decimal digit stays even.
            { 0x3EA90000U, "0.33007812" },
            // Binary32 powers of two reach Dragon4's unequal-margin arms for the same reason as the
            // binary64 entries above.
            { 0x3F800000U, "1.0" },
            { 0x40000000U, "2.0" },
            { 0x3D800000U, "0.0625" },
            { 0x00800000U, "1.1754944E-38" }
        };

    [Fact]
    public void MaximumLengthsShouldBoundBothEncodings()
    {
        CanonicalFloatingPointFormatter.MaximumDoubleLength.Should().Be(32);
        CanonicalFloatingPointFormatter.MaximumSingleLength.Should().Be(24);
    }

    [Theory]
    [MemberData(nameof(DoubleScenarios))]
    public void DoubleNamedScenariosShouldUseTheCanonicalEncoding(ulong bits, string expected)
    {
        var value = BitConverter.Int64BitsToDouble((long) bits);

        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
        AssertUtf8Matches(value, expected, forceDragon4: false);
        AssertUtf8Matches(value, expected, forceDragon4: true);
        MetadataValue.FromDouble(value).ToCanonicalString().Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(SingleScenarios))]
    public void SingleNamedScenariosShouldUseTheCanonicalEncoding(uint bits, string expected)
    {
        var value = BitConverter.Int32BitsToSingle((int) bits);

        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
        AssertUtf8Matches(value, expected, forceDragon4: false);
        AssertUtf8Matches(value, expected, forceDragon4: true);
        MetadataValue.FromSingle(value).ToCanonicalString().Should().Be(expected);
    }

    [Fact]
    public void NonFiniteDoubleValuesShouldBeRejectedByEveryOverload()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var format = () => CanonicalFloatingPointFormatter.Format(value);
            var tryFormat = () =>
                CanonicalFloatingPointFormatter.TryFormat(
                    value,
                    new char[CanonicalFloatingPointFormatter.MaximumDoubleLength],
                    out _
                );
            var tryFormatUtf8 = () =>
                CanonicalFloatingPointFormatter.TryFormatUtf8(
                    value,
                    new byte[CanonicalFloatingPointFormatter.MaximumDoubleLength],
                    out _
                );

            format.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormat.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormatUtf8.Should().Throw<ArgumentException>().WithParameterName("value");
        }
    }

    [Fact]
    public void NonFiniteSingleValuesShouldBeRejectedByEveryOverload()
    {
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var format = () => CanonicalFloatingPointFormatter.Format(value);
            var tryFormat = () =>
                CanonicalFloatingPointFormatter.TryFormat(
                    value,
                    new char[CanonicalFloatingPointFormatter.MaximumSingleLength],
                    out _
                );
            var tryFormatUtf8 = () =>
                CanonicalFloatingPointFormatter.TryFormatUtf8(
                    value,
                    new byte[CanonicalFloatingPointFormatter.MaximumSingleLength],
                    out _
                );

            format.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormat.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormatUtf8.Should().Throw<ArgumentException>().WithParameterName("value");
        }
    }

    [Fact]
    public void InsufficientDestinationsShouldRemainUnmodified()
    {
        Span<char> doubleDestination = stackalloc char[3];
        Span<char> singleDestination = stackalloc char[2];
        Span<byte> doubleUtf8Destination = stackalloc byte[3];
        Span<byte> singleUtf8Destination = stackalloc byte[2];
        doubleDestination.Fill('x');
        singleDestination.Fill('y');
        doubleUtf8Destination.Fill(0xAA);
        singleUtf8Destination.Fill(0xBB);

        CanonicalFloatingPointFormatter.TryFormat(
                double.MaxValue,
                doubleDestination,
                out var doubleCharsWritten
            )
           .Should()
           .BeFalse();
        CanonicalFloatingPointFormatter.TryFormat(
                float.MaxValue,
                singleDestination,
                out var singleCharsWritten
            )
           .Should()
           .BeFalse();
        CanonicalFloatingPointFormatter.TryFormatUtf8(
                double.MaxValue,
                doubleUtf8Destination,
                out var doubleBytesWritten
            )
           .Should()
           .BeFalse();
        CanonicalFloatingPointFormatter.TryFormatUtf8(
                float.MaxValue,
                singleUtf8Destination,
                out var singleBytesWritten
            )
           .Should()
           .BeFalse();

        doubleCharsWritten.Should().Be(0);
        singleCharsWritten.Should().Be(0);
        doubleBytesWritten.Should().Be(0);
        singleBytesWritten.Should().Be(0);
        doubleDestination.ToArray().Should().OnlyContain(character => character == 'x');
        singleDestination.ToArray().Should().OnlyContain(character => character == 'y');
        doubleUtf8Destination.ToArray().Should().OnlyContain(value => value == 0xAA);
        singleUtf8Destination.ToArray().Should().OnlyContain(value => value == 0xBB);
    }

    [Fact]
    public void RandomFiniteBitPatternsShouldMatchTheRuntimeOracle()
    {
        var random = new Random(CorpusSeed);
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        var doubleCount = 0;
        var singleCount = 0;

        while (doubleCount < CorpusSize)
        {
            random.NextBytes(bytes);
            var bits = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
            if ((bits & 0x7FF0000000000000UL) == 0x7FF0000000000000UL)
            {
                continue;
            }

            var value = BitConverter.Int64BitsToDouble((long) bits);
            var expected = CanonicalizeRuntimeText(value.ToString("R", CultureInfo.InvariantCulture));
            CanonicalFloatingPointFormatter.Format(value)
               .Should()
               .Be(expected, "binary64 bits 0x{0:X16} must be deterministic", bits);
            FormatWithDragon4(value)
               .Should()
               .Be(expected, "Dragon4 must independently match binary64 bits 0x{0:X16}", bits);
            AssertUtf8Matches(value, expected, forceDragon4: false);
            AssertUtf8Matches(value, expected, forceDragon4: true);
            doubleCount++;
        }

        while (singleCount < CorpusSize)
        {
            random.NextBytes(bytes[..sizeof(uint)]);
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            if ((bits & 0x7F800000U) == 0x7F800000U)
            {
                continue;
            }

            var value = BitConverter.Int32BitsToSingle((int) bits);
            var expected = CanonicalizeRuntimeText(value.ToString("R", CultureInfo.InvariantCulture));
            CanonicalFloatingPointFormatter.Format(value)
               .Should()
               .Be(expected, "binary32 bits 0x{0:X8} must be deterministic", bits);
            FormatWithDragon4(value)
               .Should()
               .Be(expected, "Dragon4 must independently match binary32 bits 0x{0:X8}", bits);
            AssertUtf8Matches(value, expected, forceDragon4: false);
            AssertUtf8Matches(value, expected, forceDragon4: true);
            singleCount++;
        }
    }

    [Fact]
    public void EveryBinaryExponentShouldMatchTheRuntimeOracle()
    {
        const ulong doubleMantissa = 0x000A5A5A5A5A5A5AUL;
        for (ulong exponent = 0; exponent < 0x7FF; exponent++)
        {
            var bits = (exponent << 52) | doubleMantissa;
            AssertMatchesOracle(BitConverter.Int64BitsToDouble((long) bits));
            AssertMatchesOracle(BitConverter.Int64BitsToDouble((long) (bits | (1UL << 63))));
        }

        const uint singleMantissa = 0x005A5A5AU;
        for (uint exponent = 0; exponent < 0xFF; exponent++)
        {
            var bits = (exponent << 23) | singleMantissa;
            AssertMatchesOracle(BitConverter.Int32BitsToSingle((int) bits));
            AssertMatchesOracle(BitConverter.Int32BitsToSingle((int) (bits | (1U << 31))));
        }
    }

    [Fact]
    public void FloatingPointSpanFormattingShouldAllocateNothingAfterWarmup()
    {
        const double doubleValue = 36_028_797_018_963_968.0;
        const float singleValue = 123_456_789f;
        var doubleMetadata = MetadataValue.FromDouble(doubleValue);
        var singleMetadata = MetadataValue.FromSingle(singleValue);
        Span<char> charDestination = stackalloc char[CanonicalFloatingPointFormatter.MaximumDoubleLength];
        Span<byte> byteDestination = stackalloc byte[CanonicalFloatingPointFormatter.MaximumDoubleLength];

        for (var index = 0; index < 100; index++)
        {
            CanonicalFloatingPointFormatter.TryFormat(doubleValue, charDestination, out _);
            CanonicalFloatingPointFormatter.TryFormat(singleValue, charDestination, out _);
            CanonicalFloatingPointFormatter.TryFormatUtf8(doubleValue, byteDestination, out _);
            CanonicalFloatingPointFormatter.TryFormatUtf8(singleValue, byteDestination, out _);
            ForceDragon4DoubleChars(doubleValue, charDestination, out _, true);
            ForceDragon4SingleChars(singleValue, charDestination, out _, true);
            ForceDragon4DoubleBytes(doubleValue, byteDestination, out _, true);
            ForceDragon4SingleBytes(singleValue, byteDestination, out _, true);
            doubleMetadata.TryFormatCanonical(charDestination, out _);
            singleMetadata.TryFormatCanonical(charDestination, out _);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            CanonicalFloatingPointFormatter.TryFormat(doubleValue, charDestination, out _);
            CanonicalFloatingPointFormatter.TryFormat(singleValue, charDestination, out _);
            CanonicalFloatingPointFormatter.TryFormatUtf8(doubleValue, byteDestination, out _);
            CanonicalFloatingPointFormatter.TryFormatUtf8(singleValue, byteDestination, out _);
            ForceDragon4DoubleChars(doubleValue, charDestination, out _, true);
            ForceDragon4SingleChars(singleValue, charDestination, out _, true);
            ForceDragon4DoubleBytes(doubleValue, byteDestination, out _, true);
            ForceDragon4SingleBytes(singleValue, byteDestination, out _, true);
            doubleMetadata.TryFormatCanonical(charDestination, out _);
            singleMetadata.TryFormatCanonical(charDestination, out _);
        }

        GC.GetAllocatedBytesForCurrentThread().Should().Be(before);
    }

    [Fact]
    public void StringFormattingShouldAllocateOnlyTheReturnedStrings()
    {
        const double doubleValue = 36_028_797_018_963_968.0;
        const float singleValue = 123_456_789f;
        var doubleText = CanonicalFloatingPointFormatter.Format(doubleValue);
        var singleText = CanonicalFloatingPointFormatter.Format(singleValue);
        var doubleMetadata = MetadataValue.FromDouble(doubleValue);
        var singleMetadata = MetadataValue.FromSingle(singleValue);

        for (var index = 0; index < 100; index++)
        {
            _ = CanonicalFloatingPointFormatter.Format(doubleValue);
            _ = CanonicalFloatingPointFormatter.Format(singleValue);
            _ = doubleMetadata.ToCanonicalString();
            _ = singleMetadata.ToCanonicalString();
        }

        var doubleBaseline = MeasureStringAllocations(() => new string(doubleText.AsSpan()));
        var singleBaseline = MeasureStringAllocations(() => new string(singleText.AsSpan()));

        MeasureStringAllocations(() => CanonicalFloatingPointFormatter.Format(doubleValue))
           .Should()
           .Be(doubleBaseline);
        MeasureStringAllocations(() => doubleMetadata.ToCanonicalString())
           .Should()
           .Be(doubleBaseline);
        MeasureStringAllocations(() => CanonicalFloatingPointFormatter.Format(singleValue))
           .Should()
           .Be(singleBaseline);
        MeasureStringAllocations(() => singleMetadata.ToCanonicalString())
           .Should()
           .Be(singleBaseline);
    }

    private static void AssertMatchesOracle(double value)
    {
        var expected = CanonicalizeRuntimeText(value.ToString("R", CultureInfo.InvariantCulture));
        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
        AssertUtf8Matches(value, expected, forceDragon4: false);
        AssertUtf8Matches(value, expected, forceDragon4: true);
    }

    private static void AssertMatchesOracle(float value)
    {
        var expected = CanonicalizeRuntimeText(value.ToString("R", CultureInfo.InvariantCulture));
        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
        AssertUtf8Matches(value, expected, forceDragon4: false);
        AssertUtf8Matches(value, expected, forceDragon4: true);
    }

    private static string CanonicalizeRuntimeText(string value) =>
        value.IndexOfAny(new[] { '.', 'E', 'e' }) < 0 ? value + ".0" : value;

    private static string Format(double value)
    {
        Span<char> destination = stackalloc char[CanonicalFloatingPointFormatter.MaximumDoubleLength];
        CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten)
           .Should()
           .BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string Format(float value)
    {
        Span<char> destination = stackalloc char[CanonicalFloatingPointFormatter.MaximumSingleLength];
        CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten)
           .Should()
           .BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string FormatWithDragon4(double value)
    {
        Span<char> destination = stackalloc char[CanonicalFloatingPointFormatter.MaximumDoubleLength];
        ForceDragon4DoubleChars(value, destination, out var charsWritten, true).Should().BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string FormatWithDragon4(float value)
    {
        Span<char> destination = stackalloc char[CanonicalFloatingPointFormatter.MaximumSingleLength];
        ForceDragon4SingleChars(value, destination, out var charsWritten, true).Should().BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static void AssertUtf8Matches(double value, string expected, bool forceDragon4)
    {
        Span<char> chars = stackalloc char[CanonicalFloatingPointFormatter.MaximumDoubleLength];
        Span<byte> bytes = stackalloc byte[CanonicalFloatingPointFormatter.MaximumDoubleLength + 1];
        bytes.Fill(UnwrittenByte);

        var charsSucceeded = forceDragon4 ?
            ForceDragon4DoubleChars(value, chars, out var charsWritten, true) :
            CanonicalFloatingPointFormatter.TryFormat(value, chars, out charsWritten);
        var bytesSucceeded = forceDragon4 ?
            ForceDragon4DoubleBytes(value, bytes, out var bytesWritten, true) :
            CanonicalFloatingPointFormatter.TryFormatUtf8(value, bytes, out bytesWritten);

        charsSucceeded.Should().BeTrue();
        bytesSucceeded.Should().BeTrue();
        bytesWritten.Should().Be(charsWritten);
        new string(chars[..charsWritten]).Should().Be(expected);
        bytes[..bytesWritten].ToArray().Should().Equal(Encoding.ASCII.GetBytes(expected));
        bytes[bytesWritten].Should().Be(UnwrittenByte);
    }

    private static void AssertUtf8Matches(float value, string expected, bool forceDragon4)
    {
        Span<char> chars = stackalloc char[CanonicalFloatingPointFormatter.MaximumSingleLength];
        Span<byte> bytes = stackalloc byte[CanonicalFloatingPointFormatter.MaximumSingleLength + 1];
        bytes.Fill(UnwrittenByte);

        var charsSucceeded = forceDragon4 ?
            ForceDragon4SingleChars(value, chars, out var charsWritten, true) :
            CanonicalFloatingPointFormatter.TryFormat(value, chars, out charsWritten);
        var bytesSucceeded = forceDragon4 ?
            ForceDragon4SingleBytes(value, bytes, out var bytesWritten, true) :
            CanonicalFloatingPointFormatter.TryFormatUtf8(value, bytes, out bytesWritten);

        charsSucceeded.Should().BeTrue();
        bytesSucceeded.Should().BeTrue();
        bytesWritten.Should().Be(charsWritten);
        new string(chars[..charsWritten]).Should().Be(expected);
        bytes[..bytesWritten].ToArray().Should().Equal(Encoding.ASCII.GetBytes(expected));
        bytes[bytesWritten].Should().Be(UnwrittenByte);
    }

    private static TFormatter CreateDoubleDragon4Formatter<TCodeUnit, TFormatter>()
        where TCodeUnit : unmanaged
        where TFormatter : Delegate =>
        (TFormatter) GetTryFormatCore(typeof(double))
           .MakeGenericMethod(typeof(TCodeUnit))
           .CreateDelegate(typeof(TFormatter));

    private static TFormatter CreateSingleDragon4Formatter<TCodeUnit, TFormatter>()
        where TCodeUnit : unmanaged
        where TFormatter : Delegate =>
        (TFormatter) GetTryFormatCore(typeof(float))
           .MakeGenericMethod(typeof(TCodeUnit))
           .CreateDelegate(typeof(TFormatter));

    private static MethodInfo GetTryFormatCore(Type numberType) =>
        typeof(CanonicalFloatingPointFormatter)
           .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
           .Single(
                method =>
                    method.Name == "TryFormatCore" &&
                    method.GetParameters()[0].ParameterType == numberType
            );

    private static long MeasureStringAllocations(Func<string> factory)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            GC.KeepAlive(factory());
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private delegate bool ForcedDoubleCharFormatter(
        double value,
        Span<char> destination,
        out int charsWritten,
        bool forceDragon4
    );

    private delegate bool ForcedDoubleByteFormatter(
        double value,
        Span<byte> destination,
        out int bytesWritten,
        bool forceDragon4
    );

    private delegate bool ForcedSingleCharFormatter(
        float value,
        Span<char> destination,
        out int charsWritten,
        bool forceDragon4
    );

    private delegate bool ForcedSingleByteFormatter(
        float value,
        Span<byte> destination,
        out int bytesWritten,
        bool forceDragon4
    );
}
