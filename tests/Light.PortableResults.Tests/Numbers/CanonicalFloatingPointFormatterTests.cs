using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Numbers;
using Xunit;

namespace Light.PortableResults.Tests.Numbers;

public sealed class CanonicalFloatingPointFormatterTests
{
    private const int CorpusSize = 50_000;
    private const int CorpusSeed = 0x5EED_0058;

    private static readonly ForcedDoubleFormatter ForceDragon4Double = CreateDoubleDragon4Formatter();
    private static readonly ForcedSingleFormatter ForceDragon4Single = CreateSingleDragon4Formatter();

    private delegate bool ForcedDoubleFormatter(
        double value,
        Span<char> destination,
        out int charsWritten,
        bool forceDragon4
    );

    private delegate bool ForcedSingleFormatter(
        float value,
        Span<char> destination,
        out int charsWritten,
        bool forceDragon4
    );

    public static TheoryData<ulong, string> DoubleScenarios =>
        new()
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
            { 0x3FF3C0CA428C59FBUL, "1.2345678901234567" }
        };

    public static TheoryData<uint, string> SingleScenarios =>
        new()
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
            { 0x3EA90000U, "0.33007812" }
        };

    [Theory]
    [MemberData(nameof(DoubleScenarios))]
    public void DoubleNamedScenariosShouldUseTheCanonicalEncoding(ulong bits, string expected)
    {
        var value = BitConverter.Int64BitsToDouble((long) bits);

        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
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
        MetadataValue.FromSingle(value).ToCanonicalString().Should().Be(expected);
    }

    [Fact]
    public void NonFiniteDoubleValuesShouldBeRejectedByEveryOverload()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var format = () => CanonicalFloatingPointFormatter.Format(value);
            var tryFormat = () => CanonicalFloatingPointFormatter.TryFormat(value, new char[32], out _);

            format.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormat.Should().Throw<ArgumentException>().WithParameterName("value");
        }
    }

    [Fact]
    public void NonFiniteSingleValuesShouldBeRejectedByEveryOverload()
    {
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var format = () => CanonicalFloatingPointFormatter.Format(value);
            var tryFormat = () => CanonicalFloatingPointFormatter.TryFormat(value, new char[24], out _);

            format.Should().Throw<ArgumentException>().WithParameterName("value");
            tryFormat.Should().Throw<ArgumentException>().WithParameterName("value");
        }
    }

    [Fact]
    public void InsufficientDestinationsShouldRemainUnmodified()
    {
        Span<char> doubleDestination = stackalloc char[3];
        Span<char> singleDestination = stackalloc char[2];
        doubleDestination.Fill('x');
        singleDestination.Fill('y');

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

        doubleCharsWritten.Should().Be(0);
        singleCharsWritten.Should().Be(0);
        doubleDestination.ToArray().Should().OnlyContain(character => character == 'x');
        singleDestination.ToArray().Should().OnlyContain(character => character == 'y');
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
            singleCount++;
        }
    }

    [Fact]
    public void EveryBinaryExponentShouldMatchTheRuntimeOracle()
    {
        const ulong doubleMantissa = 0x000A5A5A5A5A5A5AUL;
        for (ulong exponent = 0; exponent < 0x7FF; exponent++)
        {
            var bits = exponent << 52 | doubleMantissa;
            AssertMatchesOracle(BitConverter.Int64BitsToDouble((long) bits));
            AssertMatchesOracle(BitConverter.Int64BitsToDouble((long) (bits | 1UL << 63)));
        }

        const uint singleMantissa = 0x005A5A5AU;
        for (uint exponent = 0; exponent < 0xFF; exponent++)
        {
            var bits = exponent << 23 | singleMantissa;
            AssertMatchesOracle(BitConverter.Int32BitsToSingle((int) bits));
            AssertMatchesOracle(BitConverter.Int32BitsToSingle((int) (bits | 1U << 31)));
        }
    }

    [Fact]
    public void FloatingPointSpanFormattingShouldAllocateNothingAfterWarmup()
    {
        const double doubleValue = 36_028_797_018_963_968.0;
        const float singleValue = 123_456_789f;
        var doubleMetadata = MetadataValue.FromDouble(doubleValue);
        var singleMetadata = MetadataValue.FromSingle(singleValue);
        Span<char> destination = stackalloc char[32];

        for (var index = 0; index < 100; index++)
        {
            CanonicalFloatingPointFormatter.TryFormat(doubleValue, destination, out _);
            CanonicalFloatingPointFormatter.TryFormat(singleValue, destination, out _);
            doubleMetadata.TryFormatCanonical(destination, out _);
            singleMetadata.TryFormatCanonical(destination, out _);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1_000; index++)
        {
            CanonicalFloatingPointFormatter.TryFormat(doubleValue, destination, out _);
            CanonicalFloatingPointFormatter.TryFormat(singleValue, destination, out _);
            doubleMetadata.TryFormatCanonical(destination, out _);
            singleMetadata.TryFormatCanonical(destination, out _);
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
    }

    private static void AssertMatchesOracle(float value)
    {
        var expected = CanonicalizeRuntimeText(value.ToString("R", CultureInfo.InvariantCulture));
        CanonicalFloatingPointFormatter.Format(value).Should().Be(expected);
        FormatWithDragon4(value).Should().Be(expected);
    }

    private static string CanonicalizeRuntimeText(string value) =>
        value.IndexOfAny(new[] { '.', 'E', 'e' }) < 0 ? value + ".0" : value;

    private static string Format(double value)
    {
        Span<char> destination = stackalloc char[32];
        CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten)
           .Should()
           .BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string Format(float value)
    {
        Span<char> destination = stackalloc char[24];
        CanonicalFloatingPointFormatter.TryFormat(value, destination, out var charsWritten)
           .Should()
           .BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string FormatWithDragon4(double value)
    {
        Span<char> destination = stackalloc char[32];
        ForceDragon4Double(value, destination, out var charsWritten, true).Should().BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static string FormatWithDragon4(float value)
    {
        Span<char> destination = stackalloc char[24];
        ForceDragon4Single(value, destination, out var charsWritten, true).Should().BeTrue();
        return new string(destination[..charsWritten]);
    }

    private static ForcedDoubleFormatter CreateDoubleDragon4Formatter() =>
        (ForcedDoubleFormatter) GetTryFormatCore(typeof(double)).CreateDelegate(
            typeof(ForcedDoubleFormatter)
        );

    private static ForcedSingleFormatter CreateSingleDragon4Formatter() =>
        (ForcedSingleFormatter) GetTryFormatCore(typeof(float)).CreateDelegate(
            typeof(ForcedSingleFormatter)
        );

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
}
