// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Light.PortableResults.Numbers;

// Ported from System.Number.DiyFp in dotnet/runtime.
internal readonly ref struct DiyFp
{
    public const int DoubleImplicitBitIndex = 52;
    public const int SingleImplicitBitIndex = 23;
    public const int SignificandSize = 64;

    public DiyFp(double value)
    {
        Debug.Assert(FloatingPointBits.IsFinite(value));
        Debug.Assert(value > 0.0);
        F = FloatingPointBits.ExtractFractionAndBiasedExponent(value, out var exponent);
        E = exponent;
    }

    public DiyFp(float value)
    {
        Debug.Assert(FloatingPointBits.IsFinite(value));
        Debug.Assert(value > 0.0f);
        F = FloatingPointBits.ExtractFractionAndBiasedExponent(value, out var exponent);
        E = exponent;
    }

    public DiyFp(ulong significand, int exponent)
    {
        F = significand;
        E = exponent;
    }

    public ulong F { get; }

    public int E { get; }

    public static DiyFp CreateAndGetBoundaries(double value, out DiyFp minus, out DiyFp plus)
    {
        var result = new DiyFp(value);
        result.GetBoundaries(DoubleImplicitBitIndex, out minus, out plus);
        return result;
    }

    public static DiyFp CreateAndGetBoundaries(float value, out DiyFp minus, out DiyFp plus)
    {
        var result = new DiyFp(value);
        result.GetBoundaries(SingleImplicitBitIndex, out minus, out plus);
        return result;
    }

    public DiyFp Multiply(in DiyFp other)
    {
        var a = (uint) (F >> 32);
        var b = (uint) F;
        var c = (uint) (other.F >> 32);
        var d = (uint) other.F;

        var ac = (ulong) a * c;
        var bc = (ulong) b * c;
        var ad = (ulong) a * d;
        var bd = (ulong) b * d;
        var temporary = (bd >> 32) + (uint) ad + (uint) bc + (1U << 31);

        return new DiyFp(
            ac + (ad >> 32) + (bc >> 32) + (temporary >> 32),
            E + other.E + SignificandSize
        );
    }

    public DiyFp Normalize()
    {
        Debug.Assert(F != 0);
        var leadingZeroCount = BitOperationsCompat.LeadingZeroCount(F);
        return new DiyFp(F << leadingZeroCount, E - leadingZeroCount);
    }

    public DiyFp Subtract(in DiyFp other)
    {
        Debug.Assert(E == other.E);
        Debug.Assert(F >= other.F);
        return new DiyFp(F - other.F, E);
    }

    private void GetBoundaries(int implicitBitIndex, out DiyFp minus, out DiyFp plus)
    {
        plus = new DiyFp((F << 1) + 1, E - 1).Normalize();

        minus = F == 1UL << implicitBitIndex ?
            new DiyFp((F << 2) - 1, E - 2) :
            new DiyFp((F << 1) - 1, E - 1);

        minus = new DiyFp(minus.F << (minus.E - plus.E), plus.E);
    }
}
