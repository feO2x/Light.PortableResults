// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
#if !NET10_0_OR_GREATER
using System.Text;
#endif
#if NET10_0_OR_GREATER
using System.Text.Unicode;
#endif

namespace Light.PortableResults.Text;

/// <summary>
/// Formats primitive values with the culture-independent canonical encodings used by metadata.
/// </summary>
/// <remarks>
/// Every maximum-length constant bounds both UTF-16 characters and UTF-8 bytes. Except for
/// <see cref="MaximumCharLength" />, the corresponding canonical alphabet is ASCII, so the counts
/// in both encodings are equal.
/// </remarks>
public static partial class CanonicalTextFormatter
{
    /// <summary>The maximum canonical length of a signed 64-bit integer.</summary>
    public const int MaximumInt64Length = 20;

    /// <summary>The maximum canonical length of an unsigned 64-bit integer.</summary>
    public const int MaximumUInt64Length = 20;

    /// <summary>The maximum canonical length of a decimal value.</summary>
    public const int MaximumDecimalLength = 31;

    /// <summary>The maximum canonical length of one UTF-16 code unit in either encoding.</summary>
    public const int MaximumCharLength = 3;

    /// <summary>The largest valid day number, equal to <c>DateOnly.MaxValue.DayNumber</c>.</summary>
    public const int MaximumDayNumber = 3_652_058;

    /// <summary>The largest valid time-of-day tick count.</summary>
    public const long MaximumTimeOfDayTicks = TimeSpan.TicksPerDay - 1;

    /// <summary>The maximum canonical length of a <see cref="DateTime" />.</summary>
    public const int MaximumDateTimeLength = 28;

    /// <summary>The maximum canonical length of a <see cref="DateTimeOffset" />.</summary>
    public const int MaximumDateTimeOffsetLength = 33;

    /// <summary>The canonical length of a date.</summary>
    public const int MaximumDateLength = 10;

    /// <summary>The maximum canonical length of a time of day.</summary>
    public const int MaximumTimeLength = 16;

    /// <summary>The maximum canonical length of a <see cref="TimeSpan" />.</summary>
    public const int MaximumTimeSpanLength = 27;

    /// <summary>The canonical length of a GUID in lowercase <c>D</c> format.</summary>
    public const int MaximumGuidLength = 36;

    private const uint OneBillion = 1_000_000_000;

#if !NET10_0_OR_GREATER
    // The netstandard2.0 asset has no allocation-free decimal.GetBits overload. Its fallback relies
    // on the historical little-endian decimal layout used by .NET Framework, .NET, and Mono:
    // flags, high, low, middle. Keep the check non-throwing so unrelated formatters remain usable.
    private static readonly bool IsLegacyDecimalLayoutSupported = ValidateLegacyDecimalLayout();
#endif

    /// <summary>Attempts to format one UTF-16 code unit.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(char value, Span<char> destination, out int charsWritten)
    {
        if (destination.IsEmpty)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = value;
        charsWritten = 1;
        return true;
    }

    /// <summary>Attempts to format a signed 64-bit integer.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(long value, Span<char> destination, out int charsWritten) =>
        TryFormatInt64(value, destination, out charsWritten);

    /// <summary>Attempts to format an unsigned 64-bit integer.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(ulong value, Span<char> destination, out int charsWritten) =>
        TryFormatUInt64(value, destination, out charsWritten);

    /// <summary>Attempts to format a decimal while preserving its scale.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The netstandard2.0 asset is running on a platform whose decimal layout is unsupported.
    /// </exception>
    public static bool TryFormat(decimal value, Span<char> destination, out int charsWritten) =>
        TryFormatDecimal(value, destination, out charsWritten);

    /// <summary>
    /// Attempts to format a date and time. Local values are converted to UTC; UTC and unspecified
    /// values are unchanged.
    /// </summary>
    /// <remarks>Local conversion depends on the machine's local time zone.</remarks>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(DateTime value, Span<char> destination, out int charsWritten) =>
        TryFormatDateTime(value, destination, out charsWritten);

    /// <summary>Attempts to format a date and time with an offset.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(
        DateTimeOffset value,
        Span<char> destination,
        out int charsWritten
    ) =>
        TryFormatDateTimeOffset(value, destination, out charsWritten);

    /// <summary>Attempts to format an XML Schema duration.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(TimeSpan value, Span<char> destination, out int charsWritten) =>
        TryFormatTimeSpan(value, destination, out charsWritten);

    /// <summary>Attempts to format a GUID in lowercase <c>D</c> format.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormat(Guid value, Span<char> destination, out int charsWritten) =>
        TryFormatGuid(value, destination, out charsWritten);

    /// <summary>Attempts to format a zero-based Gregorian day number as a date.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dayNumber" /> is invalid.</exception>
    public static bool TryFormatDate(
        int dayNumber,
        Span<char> destination,
        out int charsWritten
    ) =>
        TryFormatDateNumber(dayNumber, destination, out charsWritten);

    /// <summary>Attempts to format a time-of-day tick count.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ticks" /> is invalid.</exception>
    public static bool TryFormatTime(
        long ticks,
        Span<char> destination,
        out int charsWritten
    ) =>
        TryFormatTimeOfDay(ticks, destination, out charsWritten);

    /// <summary>Attempts to format one UTF-16 code unit as replacement-fallback UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static unsafe bool TryFormatUtf8(
        char value,
        Span<byte> destination,
        out int bytesWritten
    )
    {
        var pointer = &value;
        return TryFormatUtf8(new ReadOnlySpan<char>(pointer, 1), destination, out bytesWritten);
    }

    /// <summary>Attempts to format a signed 64-bit integer as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(long value, Span<byte> destination, out int bytesWritten) =>
        TryFormatInt64(value, destination, out bytesWritten);

    /// <summary>Attempts to format an unsigned 64-bit integer as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(ulong value, Span<byte> destination, out int bytesWritten) =>
        TryFormatUInt64(value, destination, out bytesWritten);

    /// <summary>Attempts to format a decimal as UTF-8 while preserving its scale.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The netstandard2.0 asset is running on a platform whose decimal layout is unsupported.
    /// </exception>
    public static bool TryFormatUtf8(decimal value, Span<byte> destination, out int bytesWritten) =>
        TryFormatDecimal(value, destination, out bytesWritten);

    /// <summary>
    /// Attempts to format a date and time as UTF-8. Local values are converted to UTC; UTC and
    /// unspecified values are unchanged.
    /// </summary>
    /// <remarks>Local conversion depends on the machine's local time zone.</remarks>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(
        DateTime value,
        Span<byte> destination,
        out int bytesWritten
    ) =>
        TryFormatDateTime(value, destination, out bytesWritten);

    /// <summary>Attempts to format a date and time with an offset as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(
        DateTimeOffset value,
        Span<byte> destination,
        out int bytesWritten
    ) =>
        TryFormatDateTimeOffset(value, destination, out bytesWritten);

    /// <summary>Attempts to format an XML Schema duration as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(
        TimeSpan value,
        Span<byte> destination,
        out int bytesWritten
    ) =>
        TryFormatTimeSpan(value, destination, out bytesWritten);

    /// <summary>Attempts to format a GUID in lowercase <c>D</c> format as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    public static bool TryFormatUtf8(Guid value, Span<byte> destination, out int bytesWritten) =>
        TryFormatGuid(value, destination, out bytesWritten);

    /// <summary>Attempts to format a zero-based Gregorian day number as a UTF-8 date.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dayNumber" /> is invalid.</exception>
    public static bool TryFormatDateUtf8(
        int dayNumber,
        Span<byte> destination,
        out int bytesWritten
    ) =>
        TryFormatDateNumber(dayNumber, destination, out bytesWritten);

    /// <summary>Attempts to format a time-of-day tick count as UTF-8.</summary>
    /// <returns><see langword="true" /> on success; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ticks" /> is invalid.</exception>
    public static bool TryFormatTimeUtf8(
        long ticks,
        Span<byte> destination,
        out int bytesWritten
    ) =>
        TryFormatTimeOfDay(ticks, destination, out bytesWritten);

    /// <summary>
    /// Attempts to transcode UTF-16 text to UTF-8 with replacement fallback for malformed sequences.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> on success; otherwise <see langword="false" />. On failure,
    /// <paramref name="bytesWritten" /> is zero and the destination is unchanged.
    /// </returns>
    public static unsafe bool TryFormatUtf8(
        ReadOnlySpan<char> text,
        Span<byte> destination,
        out int bytesWritten
    )
    {
        if (text.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        var requiredLength = GetUtf8ByteCountWithReplacement(text);
        if (destination.Length < requiredLength)
        {
            bytesWritten = 0;
            return false;
        }

#if NET10_0_OR_GREATER
        Utf8.FromUtf16(
            text,
            destination,
            out _,
            out bytesWritten,
            replaceInvalidSequences: true,
            isFinalBlock: true
        );
        return true;
#else
        fixed (char* textPointer = text)
        {
            fixed (byte* destinationPointer = destination)
            {
                bytesWritten = TranscodeUtf16WithReplacement(
                    textPointer,
                    text.Length,
                    destinationPointer,
                    destination.Length
                );
                return true;
            }
        }
#endif
    }

    private static long GetUtf8ByteCountWithReplacement(ReadOnlySpan<char> text)
    {
        long count = 0;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character <= 0x7F)
            {
                count++;
            }
            else if (character <= 0x7FF)
            {
                count += 2;
            }
            else if (character is >= '\uD800' and <= '\uDBFF' &&
                     index + 1 < text.Length &&
                     text[index + 1] is >= '\uDC00' and <= '\uDFFF')
            {
                count += 4;
                index++;
            }
            else
            {
                count += 3;
            }
        }

        return count;
    }

#if !NET10_0_OR_GREATER
    private static unsafe int TranscodeUtf16WithReplacement(
        char* text,
        int textLength,
        byte* destination,
        int destinationLength
    )
    {
        var segmentStart = 0;
        var destinationIndex = 0;
        for (var index = 0; index < textLength; index++)
        {
            var scalar = (int) text[index];
            if (scalar is >= '\uD800' and <= '\uDBFF' &&
                index + 1 < textLength &&
                text[index + 1] is >= '\uDC00' and <= '\uDFFF')
            {
                index++;
                continue;
            }

            if (scalar is not (>= '\uD800' and <= '\uDFFF'))
            {
                continue;
            }

            destinationIndex += Encoding.UTF8.GetBytes(
                text + segmentStart,
                index - segmentStart,
                destination + destinationIndex,
                destinationLength - destinationIndex
            );
            destination[destinationIndex++] = 0xEF;
            destination[destinationIndex++] = 0xBF;
            destination[destinationIndex++] = 0xBD;
            segmentStart = index + 1;
        }

        destinationIndex += Encoding.UTF8.GetBytes(
            text + segmentStart,
            textLength - segmentStart,
            destination + destinationIndex,
            destinationLength - destinationIndex
        );
        return destinationIndex;
    }
#endif

    private static bool TryFormatInt64<TCodeUnit>(
        long value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        var isNegative = value < 0;
        var magnitude = isNegative ? unchecked((ulong) -value) : (ulong) value;
        var requiredLength = CountDigits(magnitude) + (isNegative ? 1 : 0);
        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        WriteUInt64Digits(magnitude, destination, requiredLength, isNegative ? 1 : 0);
        if (isNegative)
        {
            destination[0] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        }

        unitsWritten = requiredLength;
        return true;
    }

    private static bool TryFormatUInt64<TCodeUnit>(
        ulong value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        var requiredLength = CountDigits(value);
        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        WriteUInt64Digits(value, destination, requiredLength, 0);
        unitsWritten = requiredLength;
        return true;
    }

    private static bool TryFormatDecimal<TCodeUnit>(
        decimal value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        GetDecimalFields(value, out var low, out var middle, out var high, out var flags);
        var scale = (int) ((flags >> 16) & 0xFF);
        var isZero = (low | middle | high) == 0;
        var isNegative = !isZero && (flags & 0x80000000U) != 0;
        var digitCount = isZero ? 1 : CountDecimalDigits(low, middle, high);

        int requiredLength;
        if (scale == 0)
        {
            requiredLength = digitCount + (isNegative ? 1 : 0);
        }
        else if (scale >= digitCount)
        {
            requiredLength = scale + 2 + (isNegative ? 1 : 0);
        }
        else
        {
            requiredLength = digitCount + 1 + (isNegative ? 1 : 0);
        }

        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        var prefixLength = isNegative ? 1 : 0;
        if (isNegative)
        {
            destination[0] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        }

        int digitStart;
        var decimalPointIndex = -1;
        if (scale == 0)
        {
            digitStart = prefixLength;
        }
        else if (scale >= digitCount)
        {
            destination[prefixLength] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '0');
            destination[prefixLength + 1] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '.');
            digitStart = prefixLength + 2 + scale - digitCount;
            for (var index = prefixLength + 2; index < digitStart; index++)
            {
                destination[index] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '0');
            }
        }
        else
        {
            decimalPointIndex = prefixLength + digitCount - scale;
            destination[decimalPointIndex] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '.');
            digitStart = prefixLength;
        }

        WriteDecimalDigits(
            low,
            middle,
            high,
            destination,
            digitStart,
            digitCount,
            decimalPointIndex
        );
        unitsWritten = requiredLength;
        return true;
    }

    private static bool TryFormatDateTime<TCodeUnit>(
        DateTime value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        return TryFormatDateAndTime(
            value,
            value.Kind == DateTimeKind.Utc,
            default,
            hasOffset: false,
            destination,
            out unitsWritten
        );
    }

    private static bool TryFormatDateTimeOffset<TCodeUnit>(
        DateTimeOffset value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged =>
        TryFormatDateAndTime(
            value.DateTime,
            appendUtcDesignator: false,
            value.Offset,
            hasOffset: true,
            destination,
            out unitsWritten
        );

    private static bool TryFormatDateAndTime<TCodeUnit>(
        DateTime value,
        bool appendUtcDesignator,
        TimeSpan offset,
        bool hasOffset,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        var fraction = value.Ticks % TimeSpan.TicksPerSecond;
        var fractionDigits = CountFractionDigits(fraction);
        var requiredLength = 19 +
                             (fractionDigits == 0 ? 0 : fractionDigits + 1) +
                             (appendUtcDesignator ? 1 : 0) +
                             (hasOffset ? 6 : 0);
        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        WriteFourDigits((uint) value.Year, destination, 0);
        destination[4] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteTwoDigits((uint) value.Month, destination, 5);
        destination[7] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteTwoDigits((uint) value.Day, destination, 8);
        destination[10] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'T');
        WriteTwoDigits((uint) value.Hour, destination, 11);
        destination[13] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ':');
        WriteTwoDigits((uint) value.Minute, destination, 14);
        destination[16] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ':');
        WriteTwoDigits((uint) value.Second, destination, 17);

        var index = 19;
        if (fractionDigits != 0)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '.');
            WriteFraction(fraction, fractionDigits, destination, index);
            index += fractionDigits;
        }

        if (appendUtcDesignator)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'Z');
        }
        else if (hasOffset)
        {
            var totalMinutes = (int) (offset.Ticks / TimeSpan.TicksPerMinute);
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
                totalMinutes < 0 ? (byte) '-' : (byte) '+'
            );
            if (totalMinutes < 0)
            {
                totalMinutes = -totalMinutes;
            }

            WriteTwoDigits((uint) (totalMinutes / 60), destination, index);
            index += 2;
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ':');
            WriteTwoDigits((uint) (totalMinutes % 60), destination, index);
            index += 2;
        }

        unitsWritten = index;
        return true;
    }

    private static bool TryFormatDateNumber<TCodeUnit>(
        int dayNumber,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        if ((uint) dayNumber > MaximumDayNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(dayNumber));
        }

        if (destination.Length < MaximumDateLength)
        {
            unitsWritten = 0;
            return false;
        }

        var value = new DateTime((long) dayNumber * TimeSpan.TicksPerDay, DateTimeKind.Unspecified);
        WriteFourDigits((uint) value.Year, destination, 0);
        destination[4] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteTwoDigits((uint) value.Month, destination, 5);
        destination[7] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteTwoDigits((uint) value.Day, destination, 8);
        unitsWritten = MaximumDateLength;
        return true;
    }

    private static bool TryFormatTimeOfDay<TCodeUnit>(
        long ticks,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        if ((ulong) ticks > (ulong) MaximumTimeOfDayTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        var fraction = ticks % TimeSpan.TicksPerSecond;
        var fractionDigits = CountFractionDigits(fraction);
        var requiredLength = 8 + (fractionDigits == 0 ? 0 : fractionDigits + 1);
        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        var hours = ticks / TimeSpan.TicksPerHour;
        var minutes = ticks / TimeSpan.TicksPerMinute % 60;
        var seconds = ticks / TimeSpan.TicksPerSecond % 60;
        WriteTwoDigits((uint) hours, destination, 0);
        destination[2] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ':');
        WriteTwoDigits((uint) minutes, destination, 3);
        destination[5] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ':');
        WriteTwoDigits((uint) seconds, destination, 6);

        var index = 8;
        if (fractionDigits != 0)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '.');
            WriteFraction(fraction, fractionDigits, destination, index);
            index += fractionDigits;
        }

        unitsWritten = index;
        return true;
    }

    private static bool TryFormatTimeSpan<TCodeUnit>(
        TimeSpan value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        var ticks = value.Ticks;
        var isNegative = ticks < 0;
        var magnitude = isNegative ? unchecked((ulong) -ticks) : (ulong) ticks;
        var days = magnitude / (ulong) TimeSpan.TicksPerDay;
        var hours = magnitude / (ulong) TimeSpan.TicksPerHour % 24;
        var minutes = magnitude / (ulong) TimeSpan.TicksPerMinute % 60;
        var seconds = magnitude / (ulong) TimeSpan.TicksPerSecond % 60;
        var fraction = (long) (magnitude % (ulong) TimeSpan.TicksPerSecond);
        var fractionDigits = CountFractionDigits(fraction);
        var hasTime = hours != 0 || minutes != 0 || seconds != 0 || fraction != 0;

        var requiredLength = (isNegative ? 1 : 0) + 1;
        if (days != 0)
        {
            requiredLength += CountDigits(days) + 1;
        }

        if (hasTime)
        {
            requiredLength++;
            if (hours != 0)
            {
                requiredLength += CountDigits(hours) + 1;
            }

            if (minutes != 0)
            {
                requiredLength += CountDigits(minutes) + 1;
            }

            if (seconds != 0 || fraction != 0)
            {
                requiredLength += CountDigits(seconds) + 1;
                if (fractionDigits != 0)
                {
                    requiredLength += fractionDigits + 1;
                }
            }
        }
        else if (days == 0)
        {
            requiredLength += 3;
        }

        if (destination.Length < requiredLength)
        {
            unitsWritten = 0;
            return false;
        }

        var index = 0;
        if (isNegative)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        }

        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'P');
        if (days != 0)
        {
            WriteUnsignedComponent(days, destination, ref index);
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'D');
        }

        if (hasTime)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'T');
            if (hours != 0)
            {
                WriteUnsignedComponent(hours, destination, ref index);
                destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'H');
            }

            if (minutes != 0)
            {
                WriteUnsignedComponent(minutes, destination, ref index);
                destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'M');
            }

            if (seconds != 0 || fraction != 0)
            {
                WriteUnsignedComponent(seconds, destination, ref index);
                if (fractionDigits != 0)
                {
                    destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '.');
                    WriteFraction(fraction, fractionDigits, destination, index);
                    index += fractionDigits;
                }

                destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'S');
            }
        }
        else if (days == 0)
        {
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'T');
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '0');
            destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) 'S');
        }

        unitsWritten = index;
        return true;
    }

    private static bool TryFormatGuid<TCodeUnit>(
        Guid value,
        Span<TCodeUnit> destination,
        out int unitsWritten
    )
        where TCodeUnit : unmanaged
    {
        if (destination.Length < MaximumGuidLength)
        {
            unitsWritten = 0;
            return false;
        }

        ref var fields = ref Unsafe.As<Guid, GuidFields>(ref value);
        var index = 0;
        WriteHexByte((byte) ((uint) fields.A >> 24), destination, ref index);
        WriteHexByte((byte) ((uint) fields.A >> 16), destination, ref index);
        WriteHexByte((byte) ((uint) fields.A >> 8), destination, ref index);
        WriteHexByte((byte) fields.A, destination, ref index);
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteHexByte((byte) ((ushort) fields.B >> 8), destination, ref index);
        WriteHexByte((byte) fields.B, destination, ref index);
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteHexByte((byte) ((ushort) fields.C >> 8), destination, ref index);
        WriteHexByte((byte) fields.C, destination, ref index);
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteHexByte(fields.D, destination, ref index);
        WriteHexByte(fields.E, destination, ref index);
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) '-');
        WriteHexByte(fields.F, destination, ref index);
        WriteHexByte(fields.G, destination, ref index);
        WriteHexByte(fields.H, destination, ref index);
        WriteHexByte(fields.I, destination, ref index);
        WriteHexByte(fields.J, destination, ref index);
        WriteHexByte(fields.K, destination, ref index);
        unitsWritten = index;
        return true;
    }

    private static void WriteUInt64Digits<TCodeUnit>(
        ulong value,
        Span<TCodeUnit> destination,
        int end,
        int start
    )
        where TCodeUnit : unmanaged
    {
        var index = end;
        while (value >= OneBillion)
        {
            var remainder = (uint) (value % OneBillion);
            value /= OneBillion;
            WriteUInt32Digits(remainder, destination, ref index, 9);
        }

        WriteUInt32Digits((uint) value, destination, ref index, index - start);
    }

    private static void WriteUInt32Digits<TCodeUnit>(
        uint value,
        Span<TCodeUnit> destination,
        ref int end,
        int digits
    )
        where TCodeUnit : unmanaged
    {
        while (digits-- > 0)
        {
            var quotient = value / 10;
            destination[--end] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
                (byte) ('0' + value - quotient * 10)
            );
            value = quotient;
        }
    }

    private static void WriteDecimalDigits<TCodeUnit>(
        uint low,
        uint middle,
        uint high,
        Span<TCodeUnit> destination,
        int digitStart,
        int digitCount,
        int decimalPointIndex
    )
        where TCodeUnit : unmanaged
    {
        var index = digitStart + digitCount + (decimalPointIndex >= 0 ? 1 : 0);
        var remainingDigits = digitCount;
        do
        {
            var group = DivideDecimalByOneBillion(ref low, ref middle, ref high);
            var groupDigits = remainingDigits > 9 ? 9 : remainingDigits;
            for (var digit = 0; digit < groupDigits; digit++)
            {
                if (index - 1 == decimalPointIndex)
                {
                    index--;
                }

                var quotient = group / 10;
                destination[--index] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
                    (byte) ('0' + group - quotient * 10)
                );
                group = quotient;
            }

            remainingDigits -= groupDigits;
        }
        while (remainingDigits != 0);
    }

    private static int CountDecimalDigits(uint low, uint middle, uint high)
    {
        var groupCount = 0;
        uint mostSignificantGroup;
        do
        {
            mostSignificantGroup = DivideDecimalByOneBillion(ref low, ref middle, ref high);
            groupCount++;
        }
        while ((low | middle | high) != 0);

        return (groupCount - 1) * 9 + CountDigits(mostSignificantGroup);
    }

    private static uint DivideDecimalByOneBillion(ref uint low, ref uint middle, ref uint high)
    {
        var high64 = ((ulong) high << 32) + middle;
        var quotient64 = high64 / OneBillion;
        high = (uint) (quotient64 >> 32);
        middle = (uint) quotient64;

        var value = ((high64 - (uint) quotient64 * OneBillion) << 32) + low;
        var quotient = (uint) (value / OneBillion);
        low = quotient;
        return (uint) value - quotient * OneBillion;
    }

    private static void GetDecimalFields(
        decimal value,
        out uint low,
        out uint middle,
        out uint high,
        out uint flags
    )
    {
#if NET10_0_OR_GREATER
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        low = (uint) bits[0];
        middle = (uint) bits[1];
        high = (uint) bits[2];
        flags = (uint) bits[3];
#else
        if (!IsLegacyDecimalLayoutSupported)
        {
            throw new PlatformNotSupportedException(
                "Allocation-free decimal formatting requires a little-endian runtime with the historical decimal field layout."
            );
        }

        ref var layout = ref Unsafe.As<decimal, DecimalLayout>(ref value);
        low = layout.Low;
        middle = layout.Middle;
        high = layout.High;
        flags = layout.Flags;
#endif
    }

#if !NET10_0_OR_GREATER
    private static bool ValidateLegacyDecimalLayout()
    {
        var probe = new decimal(
            lo: 0x11111111,
            mid: 0x22222222,
            hi: 0x33333333,
            isNegative: true,
            scale: 5
        );
        ref var layout = ref Unsafe.As<decimal, DecimalLayout>(ref probe);
        return BitConverter.IsLittleEndian &&
               layout.Flags == 0x80050000U &&
               layout.High == 0x33333333U &&
               layout.Low == 0x11111111U &&
               layout.Middle == 0x22222222U;
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
#endif

    private static int CountDigits(ulong value)
    {
        var count = 1;
        while (value >= 10)
        {
            value /= 10;
            count++;
        }

        return count;
    }

    private static int CountFractionDigits(long fraction)
    {
        if (fraction == 0)
        {
            return 0;
        }

        var digits = 7;
        while (fraction % 10 == 0)
        {
            fraction /= 10;
            digits--;
        }

        return digits;
    }

    private static void WriteFraction<TCodeUnit>(
        long fraction,
        int digits,
        Span<TCodeUnit> destination,
        int start
    )
        where TCodeUnit : unmanaged
    {
        for (var index = 7; index > digits; index--)
        {
            fraction /= 10;
        }

        var end = start + digits;
        while (end > start)
        {
            var quotient = fraction / 10;
            destination[--end] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
                (byte) ('0' + fraction - quotient * 10)
            );
            fraction = quotient;
        }
    }

    private static void WriteUnsignedComponent<TCodeUnit>(
        ulong value,
        Span<TCodeUnit> destination,
        ref int index
    )
        where TCodeUnit : unmanaged
    {
        var digits = CountDigits(value);
        var end = index + digits;
        WriteUInt64Digits(value, destination, end, index);
        index = end;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTwoDigits<TCodeUnit>(
        uint value,
        Span<TCodeUnit> destination,
        int index
    )
        where TCodeUnit : unmanaged
    {
        var quotient = value / 10;
        destination[index] = CanonicalCodeUnit.FromAscii<TCodeUnit>((byte) ('0' + quotient));
        destination[index + 1] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
            (byte) ('0' + value - quotient * 10)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteFourDigits<TCodeUnit>(
        uint value,
        Span<TCodeUnit> destination,
        int index
    )
        where TCodeUnit : unmanaged
    {
        for (var offset = 3; offset >= 0; offset--)
        {
            var quotient = value / 10;
            destination[index + offset] = CanonicalCodeUnit.FromAscii<TCodeUnit>(
                (byte) ('0' + value - quotient * 10)
            );
            value = quotient;
        }
    }

    private static void WriteHexByte<TCodeUnit>(
        byte value,
        Span<TCodeUnit> destination,
        ref int index
    )
        where TCodeUnit : unmanaged
    {
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>(ToLowerHex(value >> 4));
        destination[index++] = CanonicalCodeUnit.FromAscii<TCodeUnit>(ToLowerHex(value & 0xF));
    }

    private static byte ToLowerHex(int value) =>
        (byte) (value < 10 ? '0' + value : 'a' + value - 10);

#pragma warning disable CS0649 // Fields are populated by reinterpreting Guid storage via Unsafe.As.
    private struct GuidFields
    {
        public int A;
        public short B;
        public short C;
        public byte D;
        public byte E;
        public byte F;
        public byte G;
        public byte H;
        public byte I;
        public byte J;
        public byte K;
    }
#pragma warning restore CS0649
}
