using System;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Numbers;

/// <summary>
/// Formats finite IEEE 754 binary floating-point values with one runtime-independent invariant encoding.
/// </summary>
/// <remarks>
/// <para>
/// The coefficient is the shortest decimal digit sequence that round-trips under round-to-nearest-even.
/// Candidates at a binary rounding boundary are accepted only for an even binary mantissa. When equally
/// close decimal candidates exist, the candidate with an even final decimal digit is selected.
/// </para>
/// <para>
/// Positional notation is used for decimal scales from -3 through 17 for <see cref="double" /> and from
/// -3 through 9 for <see cref="float" />. Other values use an uppercase <c>E</c>, an explicit exponent
/// sign, and at least two exponent digits. Negative zero retains its sign. Positional whole numbers,
/// including zero, end in <c>.0</c> so their floating-point JSON shape is unambiguous.
/// </para>
/// </remarks>
public static class CanonicalFloatingPointFormatter
{
    /// <summary>
    /// The maximum number of UTF-16 characters or UTF-8 bytes produced for a finite
    /// double-precision value.
    /// </summary>
    public const int MaximumDoubleLength = 32;

    /// <summary>
    /// The maximum number of UTF-16 characters or UTF-8 bytes produced for a finite
    /// single-precision value.
    /// </summary>
    public const int MaximumSingleLength = 24;

    private const int DoubleDigitsLength = 17;
    private const int SingleDigitsLength = 9;

    /// <summary>Formats a finite double-precision value.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static string Format(double value)
    {
        Span<char> buffer = stackalloc char[MaximumDoubleLength];
        TryFormatCore(value, buffer, out var charsWritten, forceDragon4: false);
        return buffer.Slice(0, charsWritten).ToString();
    }

    /// <summary>Formats a finite single-precision value.</summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static string Format(float value)
    {
        Span<char> buffer = stackalloc char[MaximumSingleLength];
        TryFormatCore(value, buffer, out var charsWritten, forceDragon4: false);
        return buffer.Slice(0, charsWritten).ToString();
    }

    /// <summary>Attempts to format a finite double-precision value into a destination span.</summary>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// On failure, <paramref name="charsWritten" /> is zero and the destination is not modified.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static bool TryFormat(double value, Span<char> destination, out int charsWritten) =>
        TryFormatCore(value, destination, out charsWritten, forceDragon4: false);

    /// <summary>Attempts to format a finite single-precision value into a destination span.</summary>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// On failure, <paramref name="charsWritten" /> is zero and the destination is not modified.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static bool TryFormat(float value, Span<char> destination, out int charsWritten) =>
        TryFormatCore(value, destination, out charsWritten, forceDragon4: false);

    /// <summary>Attempts to format a finite double-precision value as a UTF-8 numeric token.</summary>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// On failure, <paramref name="bytesWritten" /> is zero and the destination is not modified. No byte-order
    /// mark or terminator is written.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static bool TryFormatUtf8(double value, Span<byte> destination, out int bytesWritten) =>
        TryFormatCore(value, destination, out bytesWritten, forceDragon4: false);

    /// <summary>Attempts to format a finite single-precision value as a UTF-8 numeric token.</summary>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// On failure, <paramref name="bytesWritten" /> is zero and the destination is not modified. No byte-order
    /// mark or terminator is written.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    public static bool TryFormatUtf8(float value, Span<byte> destination, out int bytesWritten) =>
        TryFormatCore(value, destination, out bytesWritten, forceDragon4: false);

    private static bool TryFormatCore<TCodeUnit>(
        double value,
        Span<TCodeUnit> destination,
        out int unitsWritten,
        bool forceDragon4
    )
        where TCodeUnit : unmanaged
    {
        if (!FloatingPointBits.IsFinite(value))
        {
            throw new ArgumentException("NaN and Infinity cannot be formatted.", nameof(value));
        }

        Span<byte> digits = stackalloc byte[DoubleDigitsLength];
        var number = new NumberBuffer(digits);
        var bits = FloatingPointBits.ToUInt64(value);

        if ((bits & 0x7FFFFFFFFFFFFFFFUL) == 0)
        {
            digits[0] = (byte) '0';
            number.DigitsCount = 1;
            number.Scale = 1;
        }
        else if (forceDragon4 || !Grisu3.TryRun(value, ref number))
        {
            Dragon4.Run(value, ref number);
        }

        return TryRender(
            digits[..number.DigitsCount],
            number.Scale,
            FloatingPointBits.IsNegative(value),
            positionalMaximumScale: 17,
            destination,
            out unitsWritten
        );
    }

    private static bool TryFormatCore<TCodeUnit>(
        float value,
        Span<TCodeUnit> destination,
        out int unitsWritten,
        bool forceDragon4
    )
        where TCodeUnit : unmanaged
    {
        if (!FloatingPointBits.IsFinite(value))
        {
            throw new ArgumentException("NaN and Infinity cannot be formatted.", nameof(value));
        }

        Span<byte> digits = stackalloc byte[SingleDigitsLength];
        var number = new NumberBuffer(digits);
        var bits = FloatingPointBits.ToUInt32(value);

        if ((bits & 0x7FFFFFFFU) == 0)
        {
            digits[0] = (byte) '0';
            number.DigitsCount = 1;
            number.Scale = 1;
        }
        else if (forceDragon4 || !Grisu3.TryRun(value, ref number))
        {
            Dragon4.Run(value, ref number);
        }

        return TryRender(
            digits[..number.DigitsCount],
            number.Scale,
            FloatingPointBits.IsNegative(value),
            positionalMaximumScale: 9,
            destination,
            out unitsWritten
        );
    }

    private static bool TryRender<TCodeUnit>(
        ReadOnlySpan<byte> digits,
        int scale,
        bool isNegative,
        int positionalMaximumScale,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        var useScientificNotation = scale < -3 || scale > positionalMaximumScale;
        var requiredLength = isNegative ? 1 : 0;

        if (useScientificNotation)
        {
            var exponent = scale - 1;
            var exponentMagnitude = exponent < 0 ? -exponent : exponent;
            var exponentDigits = exponentMagnitude >= 100 ? 3 : 2;
            requiredLength += digits.Length + (digits.Length > 1 ? 1 : 0) + 2 + exponentDigits;
        }
        else if (scale <= 0)
        {
            requiredLength += 2 - scale + digits.Length;
        }
        else if (scale < digits.Length)
        {
            requiredLength += digits.Length + 1;
        }
        else
        {
            requiredLength += scale + 2;
        }

        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        var index = 0;
        if (isNegative)
        {
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '-');
        }

        if (useScientificNotation)
        {
            destination[index++] = ToCodeUnit<TCodeUnit>(digits[0]);
            if (digits.Length > 1)
            {
                destination[index++] = ToCodeUnit<TCodeUnit>((byte) '.');
                CopyDigits(digits[1..], destination, ref index);
            }

            destination[index++] = ToCodeUnit<TCodeUnit>((byte) 'E');
            var exponent = scale - 1;
            destination[index++] = ToCodeUnit<TCodeUnit>(exponent < 0 ? (byte) '-' : (byte) '+');
            WriteExponent(exponent < 0 ? -exponent : exponent, destination, ref index);
        }
        else if (scale <= 0)
        {
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '0');
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '.');
            for (var zeroIndex = 0; zeroIndex < -scale; zeroIndex++)
            {
                destination[index++] = ToCodeUnit<TCodeUnit>((byte) '0');
            }

            CopyDigits(digits, destination, ref index);
        }
        else if (scale < digits.Length)
        {
            CopyDigits(digits[..scale], destination, ref index);
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '.');
            CopyDigits(digits[scale..], destination, ref index);
        }
        else
        {
            CopyDigits(digits, destination, ref index);
            for (var zeroIndex = digits.Length; zeroIndex < scale; zeroIndex++)
            {
                destination[index++] = ToCodeUnit<TCodeUnit>((byte) '0');
            }

            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '.');
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) '0');
        }

        unitsWritten = index;
        return true;
    }

    private static void CopyDigits<TCodeUnit>(
        ReadOnlySpan<byte> digits,
        Span<TCodeUnit> destination,
        ref int destinationIndex
    )
        where TCodeUnit : unmanaged
    {
        for (var index = 0; index < digits.Length; index++)
        {
            destination[destinationIndex++] = ToCodeUnit<TCodeUnit>(digits[index]);
        }
    }

    private static void WriteExponent<TCodeUnit>(
        int exponent,
        Span<TCodeUnit> destination,
        ref int index
    )
        where TCodeUnit : unmanaged
    {
        if (exponent >= 100)
        {
            destination[index++] = ToCodeUnit<TCodeUnit>((byte) ('0' + exponent / 100));
            exponent %= 100;
        }

        destination[index++] = ToCodeUnit<TCodeUnit>((byte) ('0' + exponent / 10));
        destination[index++] = ToCodeUnit<TCodeUnit>((byte) ('0' + exponent % 10));
    }

    private static TCodeUnit ToCodeUnit<TCodeUnit>(byte value)
        where TCodeUnit : unmanaged
    {
        if (typeof(TCodeUnit) == typeof(byte))
        {
            return Unsafe.As<byte, TCodeUnit>(ref value);
        }

        var character = (char) value;
        return Unsafe.As<char, TCodeUnit>(ref character);
    }
}
