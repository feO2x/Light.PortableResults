// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Light.PortableResults.Numbers;

// This is a port of the `Grisu3` implementation here: https://github.com/google/double-conversion/blob/a711666ddd063eb1e4b181a6cb981d39a1fc8bac/double-conversion/fast-dtoa.cc
// The backing algorithm and the proofs behind it are described in more detail here: http://www.cs.tufts.edu/~nr/cs257/archive/florian-loitsch/printf.pdf
// ========================================================================================================================================
//
// Overview:
//
// The general idea behind Grisu3 is to leverage additional bits and cached powers of ten to generate the correct digits.
// The algorithm is imprecise for some numbers. Fortunately, the algorithm itself can determine this scenario and gives us
// a result indicating success or failure. We must fallback to a different algorithm for the failing scenario.
internal static class Grisu3
{
    private const int CachedPowersDecimalExponentDistance = 8;
    private const int CachedPowersMinDecimalExponent = -348;
    private const int CachedPowersPowerMaxDecimalExponent = 340;
    private const int CachedPowersOffset = -CachedPowersMinDecimalExponent;

    // 1 / Log2(10)
    private const double D1Log210 = 0.301029995663981195;

    // The minimal and maximal target exponents define the range of w's binary exponent,
    // where w is the result of multiplying the input by a cached power of ten.
    //
    // A different range might be chosen on a different platform, to optimize digit generation,
    // but a smaller range requires more powers of ten to be cached.
    private const int MaximalTargetExponent = -32;
    private const int MinimalTargetExponent = -60;

    private static readonly short[] s_CachedPowersBinaryExponent = new short[]
    {
    -1220,
    -1193,
    -1166,
    -1140,
    -1113,
    -1087,
    -1060,
    -1034,
    -1007,
    -980,
    -954,
    -927,
    -901,
    -874,
    -847,
    -821,
    -794,
    -768,
    -741,
    -715,
    -688,
    -661,
    -635,
    -608,
    -582,
    -555,
    -529,
    -502,
    -475,
    -449,
    -422,
    -396,
    -369,
    -343,
    -316,
    -289,
    -263,
    -236,
    -210,
    -183,
    -157,
    -130,
    -103,
    -77,
    -50,
    -24,
    3,
    30,
    56,
    83,
    109,
    136,
    162,
    189,
    216,
    242,
    269,
    295,
    322,
    348,
    375,
    402,
    428,
    455,
    481,
    508,
    534,
    561,
    588,
    614,
    641,
    667,
    694,
    720,
    747,
    774,
    800,
    827,
    853,
    880,
    907,
    933,
    960,
    986,
    1013,
    1039,
    1066,
    };

    private static readonly short[] s_CachedPowersDecimalExponent = new short[]
    {
    CachedPowersMinDecimalExponent,
    -340,
    -332,
    -324,
    -316,
    -308,
    -300,
    -292,
    -284,
    -276,
    -268,
    -260,
    -252,
    -244,
    -236,
    -228,
    -220,
    -212,
    -204,
    -196,
    -188,
    -180,
    -172,
    -164,
    -156,
    -148,
    -140,
    -132,
    -124,
    -116,
    -108,
    -100,
    -92,
    -84,
    -76,
    -68,
    -60,
    -52,
    -44,
    -36,
    -28,
    -20,
    -12,
    -4,
    4,
    12,
    20,
    28,
    36,
    44,
    52,
    60,
    68,
    76,
    84,
    92,
    100,
    108,
    116,
    124,
    132,
    140,
    148,
    156,
    164,
    172,
    180,
    188,
    196,
    204,
    212,
    220,
    228,
    236,
    244,
    252,
    260,
    268,
    276,
    284,
    292,
    300,
    308,
    316,
    324,
    332,
    CachedPowersPowerMaxDecimalExponent,
    };

    private static readonly ulong[] s_CachedPowersSignificand = new ulong[]
    {
    0xFA8FD5A0081C0288,
    0xBAAEE17FA23EBF76,
    0x8B16FB203055AC76,
    0xCF42894A5DCE35EA,
    0x9A6BB0AA55653B2D,
    0xE61ACF033D1A45DF,
    0xAB70FE17C79AC6CA,
    0xFF77B1FCBEBCDC4F,
    0xBE5691EF416BD60C,
    0x8DD01FAD907FFC3C,
    0xD3515C2831559A83,
    0x9D71AC8FADA6C9B5,
    0xEA9C227723EE8BCB,
    0xAECC49914078536D,
    0x823C12795DB6CE57,
    0xC21094364DFB5637,
    0x9096EA6F3848984F,
    0xD77485CB25823AC7,
    0xA086CFCD97BF97F4,
    0xEF340A98172AACE5,
    0xB23867FB2A35B28E,
    0x84C8D4DFD2C63F3B,
    0xC5DD44271AD3CDBA,
    0x936B9FCEBB25C996,
    0xDBAC6C247D62A584,
    0xA3AB66580D5FDAF6,
    0xF3E2F893DEC3F126,
    0xB5B5ADA8AAFF80B8,
    0x87625F056C7C4A8B,
    0xC9BCFF6034C13053,
    0x964E858C91BA2655,
    0xDFF9772470297EBD,
    0xA6DFBD9FB8E5B88F,
    0xF8A95FCF88747D94,
    0xB94470938FA89BCF,
    0x8A08F0F8BF0F156B,
    0xCDB02555653131B6,
    0x993FE2C6D07B7FAC,
    0xE45C10C42A2B3B06,
    0xAA242499697392D3,
    0xFD87B5F28300CA0E,
    0xBCE5086492111AEB,
    0x8CBCCC096F5088CC,
    0xD1B71758E219652C,
    0x9C40000000000000,
    0xE8D4A51000000000,
    0xAD78EBC5AC620000,
    0x813F3978F8940984,
    0xC097CE7BC90715B3,
    0x8F7E32CE7BEA5C70,
    0xD5D238A4ABE98068,
    0x9F4F2726179A2245,
    0xED63A231D4C4FB27,
    0xB0DE65388CC8ADA8,
    0x83C7088E1AAB65DB,
    0xC45D1DF942711D9A,
    0x924D692CA61BE758,
    0xDA01EE641A708DEA,
    0xA26DA3999AEF774A,
    0xF209787BB47D6B85,
    0xB454E4A179DD1877,
    0x865B86925B9BC5C2,
    0xC83553C5C8965D3D,
    0x952AB45CFA97A0B3,
    0xDE469FBD99A05FE3,
    0xA59BC234DB398C25,
    0xF6C69A72A3989F5C,
    0xB7DCBF5354E9BECE,
    0x88FCF317F22241E2,
    0xCC20CE9BD35C78A5,
    0x98165AF37B2153DF,
    0xE2A0B5DC971F303A,
    0xA8D9D1535CE3B396,
    0xFB9B7CD9A4A7443C,
    0xBB764C4CA7A44410,
    0x8BAB8EEFB6409C1A,
    0xD01FEF10A657842C,
    0x9B10A4E5E9913129,
    0xE7109BFBA19C0C9D,
    0xAC2820D9623BF429,
    0x80444B5E7AA7CF85,
    0xBF21E44003ACDD2D,
    0x8E679C2F5E44FF8F,
    0xD433179D9C8CB841,
    0x9E19DB92B4E31BA9,
    0xEB96BF6EBADF77D9,
    0xAF87023B9BF0EE6B,
    };

    private static readonly uint[] s_SmallPowersOfTen = new uint[]
    {
    1,          // 10^0
    10,         // 10^1
    100,        // 10^2
    1000,       // 10^3
    10000,      // 10^4
    100000,     // 10^5
    1000000,    // 10^6
    10000000,   // 10^7
    100000000,  // 10^8
    1000000000, // 10^9
    };


    internal static bool TryRun(double value, ref NumberBuffer number)
    {
        var positiveValue = FloatingPointBits.IsNegative(value) ? -value : value;
        Debug.Assert(positiveValue > 0);
        Debug.Assert(FloatingPointBits.IsFinite(positiveValue));

        var valueWithBoundaries = DiyFp.CreateAndGetBoundaries(
            positiveValue,
            out var boundaryMinus,
            out var boundaryPlus
        ).Normalize();
        var result = TryRunShortest(
            in boundaryMinus,
            in valueWithBoundaries,
            in boundaryPlus,
            number.Digits,
            out var length,
            out var decimalExponent
        );

        if (result)
        {
            number.Scale = length + decimalExponent;
            number.DigitsCount = length;
        }

        return result;
    }

    internal static bool TryRun(float value, ref NumberBuffer number)
    {
        var positiveValue = FloatingPointBits.IsNegative(value) ? -value : value;
        Debug.Assert(positiveValue > 0);
        Debug.Assert(FloatingPointBits.IsFinite(positiveValue));

        var valueWithBoundaries = DiyFp.CreateAndGetBoundaries(
            positiveValue,
            out var boundaryMinus,
            out var boundaryPlus
        ).Normalize();
        var result = TryRunShortest(
            in boundaryMinus,
            in valueWithBoundaries,
            in boundaryPlus,
            number.Digits,
            out var length,
            out var decimalExponent
        );

        if (result)
        {
            number.Scale = length + decimalExponent;
            number.DigitsCount = length;
        }

        return result;
    }

    // Provides a decimal representation of v.
    // Returns true if it succeeds; otherwise, the result cannot be trusted.
    //
    // There will be length digits inside the buffer (not null-terminated).
    // If the function returns true then:
    //      v == (double)(buffer * 10^decimalExponent)
    //
    // The digits in the buffer are the shortest represenation possible (no 0.09999999999999999 instead of 0.1).
    // The shorter representation will even be chosen if the longer one would be closer to v.
    //
    // The last digit will be closest to the actual v.
    // That is, even if several digits might correctly yield 'v' when read again, the closest will be computed.
    private static bool TryRunShortest(in DiyFp boundaryMinus, in DiyFp w, in DiyFp boundaryPlus, Span<byte> buffer, out int length, out int decimalExponent)
    {
        // boundaryMinus and boundaryPlus are the boundaries between v and its closest floating-point neighbors.
        // Any number strictly between boundaryMinus and boundaryPlus will round to v when converted to a double.
        // Grisu3 will never output representations that lie exactly on a boundary.

        Debug.Assert(boundaryPlus.E == w.E);

        int tenMkMinimalBinaryExponent = MinimalTargetExponent - (w.E + DiyFp.SignificandSize);
        int tenMkMaximalBinaryExponent = MaximalTargetExponent - (w.E + DiyFp.SignificandSize);

        DiyFp tenMk = GetCachedPowerForBinaryExponentRange(tenMkMinimalBinaryExponent, tenMkMaximalBinaryExponent, out int mk);

        Debug.Assert(MinimalTargetExponent <= (w.E + tenMk.E + DiyFp.SignificandSize));
        Debug.Assert(MaximalTargetExponent >= (w.E + tenMk.E + DiyFp.SignificandSize));

        // Note that tenMk is only an approximation of 10^-k.
        // A DiyFp only contains a 64-bit significan and tenMk is thus only precise up to 64-bits.

        // The DiyFp.Multiply procedure rounds its result and tenMk is approximated too.
        // The variable scaledW (as well as scaledBoundaryMinus/Plus) are now off by a small amount.
        //
        // In fact, scaledW - (w * 10^k) < 1ulp (unit in last place) of scaledW.
        // In other words, let f = scaledW.F and e = scaledW.E, then:
        //      (f - 1) * 2^e < (w * 10^k) < (f + 1) * 2^e

        DiyFp scaledW = w.Multiply(in tenMk);
        Debug.Assert(scaledW.E == (boundaryPlus.E + tenMk.E + DiyFp.SignificandSize));

        // In theory, it would be possible to avoid some recomputations by computing the difference between w
        // and boundaryMinus/Plus (a power of 2) and to compute scaledBoundaryMinus/Plus by subtracting/adding
        // from scaledW. However, the code becomes much less readable and the speed enhancements are not terrific.

        DiyFp scaledBoundaryMinus = boundaryMinus.Multiply(in tenMk);
        DiyFp scaledBoundaryPlus = boundaryPlus.Multiply(in tenMk);

        // DigitGen will generate the digits of scaledW. Therefore, we have:
        //      v == (double)(scaledW * 10^-mk)
        //
        // Set decimalExponent == -mk and pass it to DigitGen and if scaledW is not an integer than it will be updated.
        // For instance, if scaledW == 1.23 then the buffer will be filled with "123" and the decimalExponent will be decreased by 2.

        bool result = TryDigitGenShortest(in scaledBoundaryMinus, in scaledW, in scaledBoundaryPlus, buffer, out length, out int kappa);
        decimalExponent = -mk + kappa;
        return result;
    }

    // Returns the biggest power of ten that is less than or equal to the given number.
    // We furthermore receive the maximum number of bits 'number' has.
    //
    // Returns power == 10^(exponent) such that
    //      power <= number < power * 10
    // If numberBits == 0, then 0^(0-1) is returned.
    // The number of bits must be <= 32.
    //
    // Preconditions:
    //      number < (1 << (numberBits + 1))
    private static uint BiggestPowerTen(uint number, int numberBits, out int exponentPlusOne)
    {
        // Inspired by the method for finding an integer log base 10 from here:
        // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLog10

        Debug.Assert(number < (1U << (numberBits + 1)));

        // 1233/4096 is approximately 1/log2(10)
        int exponentGuess = ((numberBits + 1) * 1233) >> 12;
        Debug.Assert((uint) (exponentGuess) < s_SmallPowersOfTen.Length);

        uint power = s_SmallPowersOfTen[exponentGuess];

        // We don't have any guarantees that 2^numberBits <= number
        if (number < power)
        {
            exponentGuess--;
            power = s_SmallPowersOfTen[exponentGuess];
        }

        exponentPlusOne = exponentGuess + 1;
        return power;
    }

    //      -> w's integral part is 0x1234
    //      w's fractional part is therefore 0x567890abcdef.
    //
    // Printing w's integral part is easy (simply print 0x1234 in decimal).
    //
    // In order to print its fraction we repeatedly multiply the fraction by 10 and get each digit.
    // For example, the first digit after the point would be computed by
    //      (0x567890abcdef * 10) >> 48. -> 3
    //
    // The whole thing becomes slightly more complicated because we want to stop once we have enough digits.
    // That is, once the digits inside the buffer represent 'w' we can stop.
    //
    // Everything inside the interval low - high represents w.
    // However we have to pay attention to low, high and w's imprecision.
    private static bool TryDigitGenShortest(in DiyFp low, in DiyFp w, in DiyFp high, Span<byte> buffer, out int length, out int kappa)
    {
        Debug.Assert(low.E == w.E);
        Debug.Assert(w.E == high.E);

        Debug.Assert((low.F + 1) <= (high.F - 1));

        Debug.Assert(MinimalTargetExponent <= w.E);
        Debug.Assert(w.E <= MaximalTargetExponent);

        // low, w, and high are imprecise, but by less than one ulp (unit in the last place).
        //
        // If we remove (resp. add) 1 ulp from low (resp. high) we are certain that the new numbers
        // are outside of the interval we want the final representation to lie in.
        //
        // Inversely adding (resp. removing) 1 ulp from low (resp. high) would yield numbers that
        // are certain to lie in the interval. We will use this fact later on.
        //
        // We will now start by generating the digits within the uncertain interval.
        // Later, we will weed out representations that lie outside the safe interval and thus might lie outside the correct interval.

        ulong unit = 1;

        var tooLow = new DiyFp(low.F - unit, low.E);
        var tooHigh = new DiyFp(high.F + unit, high.E);

        // tooLow and tooHigh are guaranteed to lie outside the interval we want the generated number in.

        DiyFp unsafeInterval = tooHigh.Subtract(in tooLow);

        // We now cut the input number into two parts: the integral digits and the fractional digits.
        // We will not write any decimal separator, but adapt kappa instead.
        //
        // Reminder: we are currently computing the digits (Stored inside the buffer) such that:
        //      tooLow < buffer * 10^kappa < tooHigh
        //
        // We use tooHigh for the digitGeneration and stop as soon as possible.
        // If we stop early, we effectively round down.

        var one = new DiyFp(1UL << -w.E, w.E);

        // Division by one is a shift.
        uint integrals = (uint) (tooHigh.F >> -one.E);

        // Modulo by one is an and.
        ulong fractionals = tooHigh.F & (one.F - 1);

        uint divisor = BiggestPowerTen(integrals, DiyFp.SignificandSize - (-one.E), out kappa);
        length = 0;

        // Loop invariant:
        //      buffer = tooHigh / 10^kappa (integer division)
        // These invariants hold for the first iteration:
        //      kappa has been initialized with the divisor exponent + 1
        //      The divisor is the biggest power of ten that is smaller than integrals
        while (kappa > 0)
        {
            var digit = integrals / divisor;
            integrals %= divisor;
            Debug.Assert(digit <= 9);
            buffer[length] = (byte) ('0' + digit);

            length++;
            kappa--;

            // Note that kappa now equals the exponent of the
            // divisor and that the invariant thus holds again.

            ulong rest = ((ulong) (integrals) << -one.E) + fractionals;

            // Invariant: tooHigh = buffer * 10^kappa + DiyFp(rest, one.E)
            // Reminder: unsafeInterval.E == one.E

            if (rest < unsafeInterval.F)
            {
                // Rounding down (by not emitting the remaining digits)
                // yields a number that lies within the unsafe interval

                return TryRoundWeedShortest(
                    buffer,
                    length,
                    tooHigh.Subtract(w).F,
                    unsafeInterval.F,
                    rest,
                    tenKappa: ((ulong) (divisor)) << -one.E,
                    unit
                );
            }

            divisor /= 10;
        }

        // The integrals have been generated and we are at the point of the decimal separator.
        // In the following loop, we simply multiply the remaining digits by 10 and divide by one.
        // We just need to pay attention to multiply associated data (the unit), too.
        // Note that the multiplication by 10 does not overflow because:
        //      w.E >= -60 and thus one.E >= -60

        Debug.Assert(one.E >= MinimalTargetExponent);
        Debug.Assert(fractionals < one.F);
        Debug.Assert((ulong.MaxValue / 10) >= one.F);

        while (true)
        {
            fractionals *= 10;
            unit *= 10;

            unsafeInterval = new DiyFp(unsafeInterval.F * 10, unsafeInterval.E);

            // Integer division by one.
            uint digit = (uint) (fractionals >> -one.E);
            Debug.Assert(digit <= 9);
            buffer[length] = (byte) ('0' + digit);

            length++;
            kappa--;

            // Modulo by one.
            fractionals &= (one.F - 1);

            if (fractionals < unsafeInterval.F)
            {
                return TryRoundWeedShortest(
                    buffer,
                    length,
                    tooHigh.Subtract(w).F * unit,
                    unsafeInterval.F,
                    rest: fractionals,
                    tenKappa: one.F,
                    unit
                );
            }
        }
    }

    // Returns a cached power-of-ten with a binary exponent in the range [minExponent; maxExponent] (boundaries included).
    private static DiyFp GetCachedPowerForBinaryExponentRange(int minExponent, int maxExponent, out int decimalExponent)
    {
        Debug.Assert(s_CachedPowersSignificand.Length == s_CachedPowersBinaryExponent.Length);
        Debug.Assert(s_CachedPowersSignificand.Length == s_CachedPowersDecimalExponent.Length);

        double k = Math.Ceiling((minExponent + DiyFp.SignificandSize - 1) * D1Log210);
        int index = ((CachedPowersOffset + (int) (k) - 1) / CachedPowersDecimalExponentDistance) + 1;

        Debug.Assert((uint) (index) < s_CachedPowersSignificand.Length);

        Debug.Assert(minExponent <= s_CachedPowersBinaryExponent[index]);
        Debug.Assert(s_CachedPowersBinaryExponent[index] <= maxExponent);

        decimalExponent = s_CachedPowersDecimalExponent[index];
        return new DiyFp(s_CachedPowersSignificand[index], s_CachedPowersBinaryExponent[index]);
    }

    // Adjusts the last digit of the generated number and screens out generated solutions that may be inaccurate.
    // A solution may be inaccurate if it is outside the safe interval or if we cannot provide that it is closer to the input than a neighboring representation of the same length.
    //
    // Input:
    //      buffer containing the digits of tooHigh / 10^kappa
    //      the buffer's length
    //      distanceTooHighW == (tooHigh - w).F * unit
    //      unsafeInterval == (tooHigh - tooLow).F * unit
    //      rest = (tooHigh - buffer * 10^kapp).F * unit
    //      tenKappa = 10^kappa * unit
    //      unit = the common multiplier
    //
    // Output:
    //      Returns true if the buffer is guaranteed to contain the closest representable number to the input.
    //
    // Modifies the generated digits in the buffer to approach (round towards) w.
    private static bool TryRoundWeedShortest(Span<byte> buffer, int length, ulong distanceTooHighW, ulong unsafeInterval, ulong rest, ulong tenKappa, ulong unit)
    {
        ulong smallDistance = distanceTooHighW - unit;
        ulong bigDistance = distanceTooHighW + unit;

        // Let wLow = tooHigh - bigDistance, and wHigh = tooHigh - smallDistance.
        //
        // Note: wLow < w < wHigh
        //
        // The real w * unit must lie somewhere inside the interval
        //      ]w_low; w_high[ (often written as "(w_low; w_high)")

        // Basically the buffer currently contains a number in the unsafe interval
        //      ]too_low; too_high[ with too_low < w < too_high
        //
        //  tooHigh - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //                    ^v 1 unit            ^      ^                 ^      ^
        //  boundaryHigh ---------------------     .      .                 .      .
        //                    ^v 1 unit            .      .                 .      .
        //  - - - - - - - - - - - - - - - - - - -  +  - - + - - - - - -     .      .
        //                                         .      .         ^       .      .
        //                                         .  bigDistance   .       .      .
        //                                         .      .         .       .    rest
        //                              smallDistance     .         .       .      .
        //                                         v      .         .       .      .
        //  wHigh - - - - - - - - - - - - - - - - - -     .         .       .      .
        //                    ^v 1 unit                   .         .       .      .
        //  w ---------------------------------------     .         .       .      .
        //                    ^v 1 unit                   v         .       .      .
        //  wLow  - - - - - - - - - - - - - - - - - - - - -         .       .      .
        //                                                          .       .      v
        //  buffer -------------------------------------------------+-------+--------
        //                                                          .       .
        //                                                  safeInterval    .
        //                                                          v       .
        //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -     .
        //                    ^v 1 unit                                     .
        //  boundaryLow -------------------------                     unsafeInterval
        //                    ^v 1 unit                                     v
        //  tooLow  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //
        //
        // Note that the value of buffer could lie anywhere inside the range tooLow to tooHigh.
        //
        // boundaryLow, boundaryHigh and w are approximations of the real boundaries and v (the input number).
        // They are guaranteed to be precise up to one unit.
        // In fact the error is guaranteed to be strictly less than one unit.
        //
        // Anything that lies outside the unsafe interval is guaranteed not to round to v when read again.
        // Anything that lies inside the safe interval is guaranteed to round to v when read again.
        //
        // If the number inside the buffer lies inside the unsafe interval but not inside the safe interval
        // then we simply do not know and bail out (returning false).
        //
        // Similarly we have to take into account the imprecision of 'w' when finding the closest representation of 'w'.
        // If we have two potential representations, and one is closer to both wLow and wHigh, then we know it is closer to the actual value v.
        //
        // By generating the digits of tooHigh we got the largest (closest to tooHigh) buffer that is still in the unsafe interval.
        // In the case where wHigh < buffer < tooHigh we try to decrement the buffer.
        // This way the buffer approaches (rounds towards) w.
        //
        // There are 3 conditions that stop the decrementation process:
        //   1) the buffer is already below wHigh
        //   2) decrementing the buffer would make it leave the unsafe interval
        //   3) decrementing the buffer would yield a number below wHigh and farther away than the current number.
        //
        // In other words:
        //      (buffer{-1} < wHigh) && wHigh - buffer{-1} > buffer - wHigh
        //
        // Instead of using the buffer directly we use its distance to tooHigh.
        //
        // Conceptually rest ~= tooHigh - buffer
        //
        // We need to do the following tests in this order to avoid over- and underflows.

        Debug.Assert(rest <= unsafeInterval);

        while ((rest < smallDistance) && ((unsafeInterval - rest) >= tenKappa) && (((rest + tenKappa) < smallDistance) || ((smallDistance - rest) >= (rest + tenKappa - smallDistance))))
        {
            buffer[length - 1]--;
            rest += tenKappa;
        }

        // We have approached w+ as much as possible.
        // We now test if approaching w- would require changing the buffer.
        // If yes, then we have two possible representations close to w, but we cannot decide which one is closer.
        if ((rest < bigDistance) && ((unsafeInterval - rest) >= tenKappa) && (((rest + tenKappa) < bigDistance) || ((bigDistance - rest) > (rest + tenKappa - bigDistance))))
        {
            return false;
        }

        // Weeding test.
        //
        // The safe interval is [tooLow + 2 ulp; tooHigh - 2 ulp]
        // Since tooLow = tooHigh - unsafeInterval this is equivalent to
        //      [tooHigh - unsafeInterval + 4 ulp; tooHigh - 2 ulp]
        //
        // Conceptually we have: rest ~= tooHigh - buffer
        return ((2 * unit) <= rest) && (rest <= (unsafeInterval - 4 * unit));
    }
}
