using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is <see langword="null" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped. Defaults to <see langword="true" /> because most downstream
    /// assertions require a non-<see langword="null" /> value and would throw
    /// <see cref="InvalidOperationException" /> otherwise.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    public static Check<T> IsNotNull<T>(this Check<T> check, bool shortCircuitOnError = true) =>
        check.IsShortCircuited || !check.IsValueNull ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotNull, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value is <see langword="null" />,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped. Defaults to <see langword="true" /> because most downstream
    /// assertions require a non-<see langword="null" /> value and would throw
    /// <see cref="InvalidOperationException" /> otherwise.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsNotNull<T>(
        this Check<T> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = true
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || !check.IsValueNull ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotNull,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked value is not <see langword="null" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped. Defaults to <see langword="true" /> because assertions that
    /// follow <c>IsNull</c> typically expect a <see langword="null" /> value.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    public static Check<T> IsNull<T>(this Check<T> check, bool shortCircuitOnError = true)
    {
        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.Null, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is not <see langword="null" />,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped. Defaults to <see langword="true" /> because assertions that
    /// follow <c>IsNull</c> typically expect a <see langword="null" /> value.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsNull<T>(
        this Check<T> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = true
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.Null,
            overrides,
            shortCircuitOnError
        );
    }
}
