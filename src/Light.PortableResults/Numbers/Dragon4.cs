// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Light.PortableResults.Numbers;

// Shortest-unique mode port of System.Number.Dragon4 in dotnet/runtime.
internal static class Dragon4
{
    public static void Run(double value, ref NumberBuffer number)
    {
        var positiveValue = FloatingPointBits.IsNegative(value) ? -value : value;
        Debug.Assert(positiveValue > 0);
        Debug.Assert(FloatingPointBits.IsFinite(positiveValue));

        var mantissa = FloatingPointBits.ExtractFractionAndBiasedExponent(
            positiveValue,
            out var exponent
        );
        uint mantissaHighBitIndex;
        var hasUnequalMargins = false;

        if ((mantissa >> DiyFp.DoubleImplicitBitIndex) != 0)
        {
            mantissaHighBitIndex = DiyFp.DoubleImplicitBitIndex;
            hasUnequalMargins = mantissa == 1UL << DiyFp.DoubleImplicitBitIndex;
        }
        else
        {
            Debug.Assert(mantissa != 0);
            mantissaHighBitIndex = (uint) BitOperationsCompat.Log2(mantissa);
        }

        var length = (int) GenerateDigits(
            mantissa,
            exponent,
            mantissaHighBitIndex,
            hasUnequalMargins,
            number.Digits,
            out var decimalExponent
        );

        number.Scale = decimalExponent + 1;
        number.DigitsCount = length;
    }

    public static void Run(float value, ref NumberBuffer number)
    {
        var positiveValue = FloatingPointBits.IsNegative(value) ? -value : value;
        Debug.Assert(positiveValue > 0);
        Debug.Assert(FloatingPointBits.IsFinite(positiveValue));

        var mantissa = FloatingPointBits.ExtractFractionAndBiasedExponent(
            positiveValue,
            out var exponent
        );
        uint mantissaHighBitIndex;
        var hasUnequalMargins = false;

        if ((mantissa >> DiyFp.SingleImplicitBitIndex) != 0)
        {
            mantissaHighBitIndex = DiyFp.SingleImplicitBitIndex;
            hasUnequalMargins = mantissa == 1U << DiyFp.SingleImplicitBitIndex;
        }
        else
        {
            Debug.Assert(mantissa != 0);
            mantissaHighBitIndex = (uint) BitOperationsCompat.Log2(mantissa);
        }

        var length = (int) GenerateDigits(
            mantissa,
            exponent,
            mantissaHighBitIndex,
            hasUnequalMargins,
            number.Digits,
            out var decimalExponent
        );

        number.Scale = decimalExponent + 1;
        number.DigitsCount = length;
    }

    private static uint GenerateDigits(
        ulong mantissa,
        int exponent,
        uint mantissaHighBitIndex,
        bool hasUnequalMargins,
        Span<byte> buffer,
        out int decimalExponent
    )
    {
        Debug.Assert(mantissa != 0);
        Debug.Assert(buffer.Length > 0);

        Dragon4BigInteger scale;
        Dragon4BigInteger scaledValue;
        Dragon4BigInteger scaledMarginLow;
        Dragon4BigInteger optionalMarginHigh;

        if (hasUnequalMargins)
        {
            if (exponent > 0)
            {
                Dragon4BigInteger.SetUInt64(out scaledValue, 4 * mantissa);
                scaledValue.ShiftLeft((uint) exponent);
                Dragon4BigInteger.SetUInt32(out scale, 4);
                Dragon4BigInteger.Pow2((uint) exponent, out scaledMarginLow);
                Dragon4BigInteger.Pow2((uint) (exponent + 1), out optionalMarginHigh);
            }
            else
            {
                Dragon4BigInteger.SetUInt64(out scaledValue, 4 * mantissa);
                Dragon4BigInteger.Pow2((uint) (-exponent + 2), out scale);
                Dragon4BigInteger.SetUInt32(out scaledMarginLow, 1);
                Dragon4BigInteger.SetUInt32(out optionalMarginHigh, 2);
            }
        }
        else
        {
            if (exponent > 0)
            {
                Dragon4BigInteger.SetUInt64(out scaledValue, 2 * mantissa);
                scaledValue.ShiftLeft((uint) exponent);
                Dragon4BigInteger.SetUInt32(out scale, 2);
                Dragon4BigInteger.Pow2((uint) exponent, out scaledMarginLow);
            }
            else
            {
                Dragon4BigInteger.SetUInt64(out scaledValue, 2 * mantissa);
                Dragon4BigInteger.Pow2((uint) (-exponent + 1), out scale);
                Dragon4BigInteger.SetUInt32(out scaledMarginLow, 1);
            }

            Dragon4BigInteger.SetUInt32(out optionalMarginHigh, 0);
        }

        ref var scaledMarginHigh = ref (
            hasUnequalMargins ? ref optionalMarginHigh : ref scaledMarginLow
        );

        const double Log10Of2 = 0.30102999566398119521373889472449;
        var digitExponent = (int) Math.Ceiling(
            ((int) mantissaHighBitIndex + exponent) * Log10Of2 - 0.69
        );

        if (digitExponent > 0)
        {
            scale.MultiplyPow10((uint) digitExponent);
        }
        else if (digitExponent < 0)
        {
            Dragon4BigInteger.Pow10((uint) -digitExponent, out var powerOfTen);
            scaledValue.Multiply(ref powerOfTen);
            scaledMarginLow.Multiply(ref powerOfTen);

            if (hasUnequalMargins)
            {
                Dragon4BigInteger.Multiply(
                    ref scaledMarginLow,
                    2,
                    out optionalMarginHigh
                );
            }
        }

        var isEven = (mantissa & 1) == 0;
        Dragon4BigInteger.Add(
            ref scaledValue,
            ref scaledMarginHigh,
            out var scaledValueHigh
        );
        var highComparison = Dragon4BigInteger.Compare(ref scaledValueHigh, ref scale);
        var estimateTooLow = isEven ? highComparison >= 0 : highComparison > 0;

        if (estimateTooLow)
        {
            digitExponent++;
        }
        else
        {
            scaledValue.Multiply10();
            scaledMarginLow.Multiply10();

            if (hasUnequalMargins)
            {
                Dragon4BigInteger.Multiply(
                    ref scaledMarginLow,
                    2,
                    out optionalMarginHigh
                );
            }
        }

        var cutoffExponent = digitExponent - buffer.Length;
        decimalExponent = --digitExponent;

        var highBlock = scale.GetBlock(scale.Length - 1);
        if (highBlock < 8 || highBlock > 429496729)
        {
            var highBlockLog2 = (uint) BitOperationsCompat.Log2(highBlock);
            var shift = (32U + 27U - highBlockLog2) % 32U;
            scale.ShiftLeft(shift);
            scaledValue.ShiftLeft(shift);
            scaledMarginLow.ShiftLeft(shift);

            if (hasUnequalMargins)
            {
                Dragon4BigInteger.Multiply(
                    ref scaledMarginLow,
                    2,
                    out optionalMarginHigh
                );
            }
        }

        var currentDigit = 0;
        bool low;
        bool high;
        uint outputDigit;

        while (true)
        {
            outputDigit = Dragon4BigInteger.HeuristicDivide(ref scaledValue, ref scale);
            Debug.Assert(outputDigit < 10);

            Dragon4BigInteger.Add(
                ref scaledValue,
                ref scaledMarginHigh,
                out scaledValueHigh
            );
            var lowComparison = Dragon4BigInteger.Compare(
                ref scaledValue,
                ref scaledMarginLow
            );
            highComparison = Dragon4BigInteger.Compare(ref scaledValueHigh, ref scale);

            if (isEven)
            {
                low = lowComparison <= 0;
                high = highComparison >= 0;
            }
            else
            {
                low = lowComparison < 0;
                high = highComparison > 0;
            }

            if (low || high || digitExponent == cutoffExponent)
            {
                break;
            }

            buffer[currentDigit++] = (byte) ('0' + outputDigit);
            scaledValue.Multiply10();
            scaledMarginLow.Multiply10();

            if (hasUnequalMargins)
            {
                Dragon4BigInteger.Multiply(
                    ref scaledMarginLow,
                    2,
                    out optionalMarginHigh
                );
            }

            digitExponent--;
        }

        var roundDown = low;
        if (low == high)
        {
            scaledValue.ShiftLeft(1);
            var comparison = Dragon4BigInteger.Compare(ref scaledValue, ref scale);
            roundDown = comparison < 0 || comparison == 0 && (outputDigit & 1) == 0;
        }

        if (roundDown)
        {
            buffer[currentDigit++] = (byte) ('0' + outputDigit);
        }
        else if (outputDigit == 9)
        {
            while (true)
            {
                if (currentDigit == 0)
                {
                    buffer[currentDigit++] = (byte) '1';
                    decimalExponent++;
                    break;
                }

                currentDigit--;
                if (buffer[currentDigit] != '9')
                {
                    buffer[currentDigit++]++;
                    break;
                }
            }
        }
        else
        {
            buffer[currentDigit++] = (byte) ('0' + outputDigit + 1);
        }

        Debug.Assert(currentDigit <= buffer.Length);
        return (uint) currentDigit;
    }
}
