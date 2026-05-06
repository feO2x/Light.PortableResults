using System;
using System.Collections.Generic;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value does not equal the specified expected value.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must equal.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="EqualityComparer{T}.Default" /> for the comparison.</remarks>
    [ValidationRule(ValidationErrorCodes.EqualTo, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
    public static Check<T> IsEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    ) =>
        check.IsEqualTo(comparativeValue, EqualityComparer<T>.Default, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value does not equal the specified expected value,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must equal.</param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="EqualityComparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    ) =>
        check.IsEqualTo(comparativeValue, EqualityComparer<T>.Default, overrides, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value does not equal the specified expected value.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must equal.</param>
    /// <param name="equalityComparer">
    /// The comparer used to determine equality. Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="equalityComparer" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> IsEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        IEqualityComparer<T> equalityComparer,
        bool shortCircuitOnError = false
    )
    {
        if (equalityComparer is null)
        {
            throw new ArgumentNullException(nameof(equalityComparer));
        }

        if (check.IsShortCircuited || equalityComparer.Equals(check.Value, comparativeValue))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.EqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value does not equal the specified expected value,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must equal.</param>
    /// <param name="equalityComparer">
    /// The comparer used to determine equality. Must not be <see langword="null" />.
    /// </param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="equalityComparer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        IEqualityComparer<T> equalityComparer,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        if (equalityComparer is null)
        {
            throw new ArgumentNullException(nameof(equalityComparer));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || equalityComparer.Equals(check.Value, comparativeValue))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.EqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value equals the specified disallowed value.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must not equal.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="EqualityComparer{T}.Default" /> for the comparison.</remarks>
    [ValidationRule(ValidationErrorCodes.NotEqualTo, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
    public static Check<T> IsNotEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    ) =>
        check.IsNotEqualTo(comparativeValue, EqualityComparer<T>.Default, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value equals the specified disallowed value,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must not equal.</param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="EqualityComparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsNotEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    ) =>
        check.IsNotEqualTo(comparativeValue, EqualityComparer<T>.Default, overrides, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value equals the specified disallowed value.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must not equal.</param>
    /// <param name="equalityComparer">
    /// The comparer used to determine equality. Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="equalityComparer" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> IsNotEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        IEqualityComparer<T> equalityComparer,
        bool shortCircuitOnError = false
    )
    {
        if (equalityComparer is null)
        {
            throw new ArgumentNullException(nameof(equalityComparer));
        }

        if (check.IsShortCircuited || !equalityComparer.Equals(check.Value, comparativeValue))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.NotEqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value equals the specified disallowed value,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">The value the checked value must not equal.</param>
    /// <param name="equalityComparer">
    /// The comparer used to determine equality. Must not be <see langword="null" />.
    /// </param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="equalityComparer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> IsNotEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        IEqualityComparer<T> equalityComparer,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        if (equalityComparer is null)
        {
            throw new ArgumentNullException(nameof(equalityComparer));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || !equalityComparer.Equals(check.Value, comparativeValue))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.NotEqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }
}
