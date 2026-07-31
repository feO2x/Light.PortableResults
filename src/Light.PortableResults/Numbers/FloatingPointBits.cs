// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Light.PortableResults.Numbers;

internal static class FloatingPointBits
{
    [StructLayout(LayoutKind.Explicit)]
    private struct DoubleUInt64
    {
        [FieldOffset(0)]
        public double Double;

        [FieldOffset(0)]
        public ulong UInt64;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SingleUInt32
    {
        [FieldOffset(0)]
        public float Single;

        [FieldOffset(0)]
        public uint UInt32;
    }

    public static ulong ToUInt64(double value) => new DoubleUInt64 { Double = value }.UInt64;

    public static uint ToUInt32(float value) => new SingleUInt32 { Single = value }.UInt32;

    public static bool IsFinite(double value) =>
        (ToUInt64(value) & 0x7FF0000000000000UL) != 0x7FF0000000000000UL;

    public static bool IsFinite(float value) => (ToUInt32(value) & 0x7F800000U) != 0x7F800000U;

    public static bool IsNegative(double value) => (ToUInt64(value) & 0x8000000000000000UL) != 0;

    public static bool IsNegative(float value) => (ToUInt32(value) & 0x80000000U) != 0;

    public static ulong ExtractFractionAndBiasedExponent(double value, out int exponent)
    {
        var bits = ToUInt64(value);
        var fraction = bits & 0x000FFFFFFFFFFFFFUL;
        exponent = (int) (bits >> 52) & 0x7FF;

        if (exponent != 0)
        {
            fraction |= 1UL << 52;
            exponent -= 1075;
        }
        else
        {
            exponent = -1074;
        }

        return fraction;
    }

    public static uint ExtractFractionAndBiasedExponent(float value, out int exponent)
    {
        var bits = ToUInt32(value);
        var fraction = bits & 0x007FFFFFU;
        exponent = (int) (bits >> 23) & 0xFF;

        if (exponent != 0)
        {
            fraction |= 1U << 23;
            exponent -= 150;
        }
        else
        {
            exponent = -149;
        }

        return fraction;
    }
}
