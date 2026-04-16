using System;
using System.Collections;
using System.Numerics;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    private static Check<T> AddBuiltInError<T>(
        Check<T> check,
        ValidationErrorDefinition definition,
        bool shortCircuitOnError
    )
    {
        var updatedCheck = check.AddError(definition, respectShortCircuit: false);
        return updatedCheck.ShortCircuitOnErrorIfRequested(shortCircuitOnError);
    }

    private static Check<T> AddBuiltInErrorWithOverrides<T>(
        Check<T> check,
        ValidationErrorDefinition definition,
        ErrorOverrides overrides,
        bool shortCircuitOnError
    )
    {
        EnsureErrorOverrides(overrides);

        var updatedCheck = overrides.Message is null ?
            check.AddError(
                definition,
                code: overrides.Code,
                metadata: overrides.Metadata,
                category: overrides.Category,
                respectShortCircuit: false
            ) :
            check.AddError(
                overrides.Message,
                code: overrides.Code ?? definition.Code,
                metadata: overrides.Metadata ?? definition.Metadata,
                target: definition.Target,
                category: overrides.Category ?? definition.Category,
                respectShortCircuit: false
            );

        return updatedCheck.ShortCircuitOnErrorIfRequested(shortCircuitOnError);
    }

    private static void EnsureErrorOverrides(ErrorOverrides overrides)
    {
        if (overrides.Message is null &&
            overrides.Code is null &&
            overrides.Category is null &&
            overrides.Metadata is null
           )
        {
            throw new ArgumentException(
                "At least one override must be specified. Use the overload without overrides when no override is needed.",
                nameof(overrides)
            );
        }

        if (overrides.Message is not null && string.IsNullOrWhiteSpace(overrides.Message))
        {
            throw new ArgumentException(
                "The override message must not be empty or whitespace.",
                nameof(overrides)
            );
        }
    }

    private static InvalidOperationException CreateNullValueException(string assertionName) =>
        new (
            $"{assertionName} requires a non-null value. Enable automatic null checking or guard explicitly " +
            "with IsNotNull() before calling this assertion."
        );

    private static InvalidOperationException CreateNullStringException(string assertionName) =>
        new (
            $"{assertionName} requires a non-null string value. Enable automatic null checking, use string " +
            "normalization, or guard explicitly with IsNotNull() before calling this assertion."
        );

    private static InvalidOperationException CreateNullCollectionException(string assertionName) =>
        new (
            $"{assertionName} requires a non-null collection. Enable automatic null checking or guard explicitly " +
            "with IsNotNull() before calling this assertion."
        );

    private static string GetRequiredString(string? value, string assertionName) =>
        value ?? throw CreateNullStringException(assertionName);

    private static T GetRequiredValue<T>(T value, string assertionName) =>
        value is null ? throw CreateNullValueException(assertionName) : value;

    private static IEnumerable GetRequiredCollection(IEnumerable? value, string assertionName) =>
        value ?? throw CreateNullCollectionException(assertionName);

    private static int GetCollectionCount(IEnumerable collection)
    {
        if (collection is string text)
        {
            return text.Length;
        }

        if (collection is ICollection nonGenericCollection)
        {
            return nonGenericCollection.Count;
        }

        var count = 0;
        var enumerator = collection.GetEnumerator();
        using (enumerator as IDisposable)
        {
            while (enumerator.MoveNext())
            {
                count++;
            }
        }

        return count;
    }

    private static bool LooksLikeEmail(string value)
    {
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex != value.LastIndexOf('@') || atIndex == value.Length - 1)
        {
            return false;
        }

        return true;
    }

    private static bool ContainsOnlyDigitsCore(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsOnlyLettersAndDigitsCore(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsLetterOrDigit(value[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureLengthBoundary(int length, string paramName)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "The value must be zero or greater.");
        }
    }

    private static void EnsureRangeBoundaries<T>(T lowerBoundary, T upperBoundary, string lowerName, string upperName)
    {
        if (lowerBoundary is null)
        {
            throw new ArgumentNullException(lowerName);
        }

        if (upperBoundary is null)
        {
            throw new ArgumentNullException(upperName);
        }
    }

    private static void EnsureMinMax(int minValue, int maxValue, string minName, string maxName)
    {
        EnsureLengthBoundary(minValue, minName);
        EnsureLengthBoundary(maxValue, maxName);
        if (maxValue < minValue)
        {
            throw new ArgumentException(
                $"'{maxName}' must be greater than or equal to '{minName}'.",
                maxName
            );
        }
    }

    private static void EnsurePrecisionScale(int precision, int scale)
    {
        if (precision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be greater than zero.");
        }

        if (scale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be zero or greater.");
        }

        if (scale > precision)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must not be greater than precision.");
        }
    }

    private static PrecisionScaleInfo GetPrecisionScaleInfo(decimal value, bool ignoreTrailingZeros)
    {
        var bits = decimal.GetBits(Math.Abs(value));
        var scale = (bits[3] >> 16) & 0x7F;
        var unscaledValue =
            ((BigInteger) (uint) bits[2] << 64) |
            ((BigInteger) (uint) bits[1] << 32) |
            (uint) bits[0];

        if (ignoreTrailingZeros && unscaledValue.IsZero)
        {
            return new PrecisionScaleInfo(digits: 1, scale: 0);
        }

        if (ignoreTrailingZeros)
        {
            while (scale > 0 && !unscaledValue.IsZero)
            {
                var quotient = BigInteger.DivRem(unscaledValue, 10, out var remainder);
                if (remainder != BigInteger.Zero)
                {
                    break;
                }

                unscaledValue = quotient;
                scale--;
            }
        }

        var digits = 0;
        if (unscaledValue.IsZero)
        {
            digits = 1;
        }
        else
        {
            while (unscaledValue > BigInteger.Zero)
            {
                unscaledValue /= 10;
                digits++;
            }
        }

        return new PrecisionScaleInfo(digits, scale);
    }

    private static bool IsEnumNameDefined<TEnum>(string value, bool ignoreCase)
        where TEnum : struct, Enum
    {
        var comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        foreach (var name in EnumNameCache<TEnum>.Names)
        {
            if (comparer.Equals(name, value))
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct PrecisionScaleInfo
    {
        public PrecisionScaleInfo(int digits, int scale)
        {
            Digits = digits;
            Scale = scale;
        }

        public int Digits { get; }
        public int Scale { get; }
    }

    private static class EnumNameCache<TEnum>
        where TEnum : struct, Enum
    {
        public static string[] Names { get; } = Enum.GetNames(typeof(TEnum));
    }
}
