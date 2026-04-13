using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="precision">
    /// The maximum total number of significant digits. Must be greater than zero.
    /// </param>
    /// <param name="scale">
    /// The maximum number of digits after the decimal point. Must be zero or greater and must not
    /// exceed <paramref name="precision" />.
    /// </param>
    /// <param name="ignoreTrailingZeros">
    /// When <see langword="true" />, trailing zeros after the decimal point are stripped before
    /// comparison; e.g., <c>1.200</c> is evaluated as <c>1.2</c>.
    /// Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="precision" /> is less than 1, <paramref name="scale" /> is negative,
    /// or <paramref name="scale" /> exceeds <paramref name="precision" />.
    /// </exception>
    public static Check<decimal> HasPrecisionAndScale(
        this Check<decimal> check,
        int precision,
        int scale,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var info = GetPrecisionScaleInfo(check.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="precision">
    /// The maximum total number of significant digits. Must be greater than zero.
    /// </param>
    /// <param name="scale">
    /// The maximum number of digits after the decimal point. Must be zero or greater and must not
    /// exceed <paramref name="precision" />.
    /// </param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="ignoreTrailingZeros">
    /// When <see langword="true" />, trailing zeros after the decimal point are stripped before
    /// comparison; e.g., <c>1.200</c> is evaluated as <c>1.2</c>.
    /// Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="precision" /> is less than 1, <paramref name="scale" /> is negative,
    /// or <paramref name="scale" /> exceeds <paramref name="precision" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <example>
    /// <code>
    /// context.Check(dto.Amount).HasPrecisionAndScale(12, 2, "Amount format is invalid");
    /// </code>
    /// </example>
    public static Check<decimal> HasPrecisionAndScale(
        this Check<decimal> check,
        int precision,
        int scale,
        ErrorOverrides overrides,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var info = GetPrecisionScaleInfo(check.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="precision">
    /// The maximum total number of significant digits. Must be greater than zero.
    /// </param>
    /// <param name="scale">
    /// The maximum number of digits after the decimal point. Must be zero or greater and must not
    /// exceed <paramref name="precision" />.
    /// </param>
    /// <param name="ignoreTrailingZeros">
    /// When <see langword="true" />, trailing zeros after the decimal point are stripped before
    /// comparison; e.g., <c>1.200</c> is evaluated as <c>1.2</c>.
    /// Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="precision" /> is less than 1, <paramref name="scale" /> is negative,
    /// or <paramref name="scale" /> exceeds <paramref name="precision" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked nullable decimal has no value. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<decimal?> HasPrecisionAndScale(
        this Check<decimal?> check,
        int precision,
        int scale,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = check.Value;
        if (!value.HasValue)
        {
            throw CreateNullValueException(nameof(HasPrecisionAndScale));
        }

        var info = GetPrecisionScaleInfo(value.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="precision">
    /// The maximum total number of significant digits. Must be greater than zero.
    /// </param>
    /// <param name="scale">
    /// The maximum number of digits after the decimal point. Must be zero or greater and must not
    /// exceed <paramref name="precision" />.
    /// </param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="ignoreTrailingZeros">
    /// When <see langword="true" />, trailing zeros after the decimal point are stripped before
    /// comparison; e.g., <c>1.200</c> is evaluated as <c>1.2</c>.
    /// Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="precision" /> is less than 1, <paramref name="scale" /> is negative,
    /// or <paramref name="scale" /> exceeds <paramref name="precision" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked nullable decimal has no value. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<decimal?> HasPrecisionAndScale(
        this Check<decimal?> check,
        int precision,
        int scale,
        ErrorOverrides overrides,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = check.Value;
        if (!value.HasValue)
        {
            throw CreateNullValueException(nameof(HasPrecisionAndScale));
        }

        var info = GetPrecisionScaleInfo(value.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }
}
