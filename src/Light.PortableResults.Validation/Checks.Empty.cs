using System;
using System.Collections;
using System.Collections.Immutable;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked string is neither <see langword="null" /> nor empty.
    /// Whitespace-only strings are not considered empty.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Both <see langword="null" /> and <see cref="string.Empty" /> pass without error. If whitespace
    /// should also be rejected, use <see cref="IsNotNullOrWhiteSpace(Check{string}, bool)" /> instead.
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.Empty)]
    public static Check<string?> IsEmpty(this Check<string?> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked string is neither <see langword="null" /> nor empty,
    /// applying the specified inline error overrides. Whitespace-only strings are not considered empty.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// Both <see langword="null" /> and <see cref="string.Empty" /> pass without error. If whitespace
    /// should also be rejected, use <see cref="IsNotNullOrWhiteSpace(Check{string}, bool)" /> instead.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<string?> IsEmpty(
        this Check<string?> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Empty,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked string is <see langword="null" /> or empty.
    /// Whitespace-only strings are not considered empty.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// A <see langword="null" /> value triggers a validation error (treated as empty). Whitespace-only
    /// strings pass without error; use
    /// <see cref="IsNotNullOrWhiteSpace(Check{string}, bool)" /> to also reject whitespace.
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.NotEmpty)]
    public static Check<string?> IsNotEmpty(this Check<string?> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || !string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked string is <see langword="null" /> or empty,
    /// applying the specified inline error overrides. Whitespace-only strings are not considered empty.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// A <see langword="null" /> value triggers a validation error (treated as empty). Whitespace-only
    /// strings pass without error; use
    /// <see cref="IsNotNullOrWhiteSpace(Check{string}, bool)" /> to also reject whitespace.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<string?> IsNotEmpty(
        this Check<string?> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || !string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotEmpty,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked GUID is not <see cref="Guid.Empty" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    [ValidationRule(ValidationErrorCodes.Empty)]
    public static Check<Guid> IsEmpty(this Check<Guid> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value == Guid.Empty ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked GUID is not <see cref="Guid.Empty" />,
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
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<Guid> IsEmpty(
        this Check<Guid> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value == Guid.Empty ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Empty,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked GUID is <see cref="Guid.Empty" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    [ValidationRule(ValidationErrorCodes.NotEmpty)]
    public static Check<Guid> IsNotEmpty(this Check<Guid> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value != Guid.Empty ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked GUID is <see cref="Guid.Empty" />,
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
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<Guid> IsNotEmpty(
        this Check<Guid> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value != Guid.Empty ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotEmpty,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked collection is not <see langword="null" /> and has one or more items.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// A <see langword="null" /> collection is treated as empty and passes without error.
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.Empty)]
    public static Check<TCollection> IsEmpty<TCollection>(
        this Check<TCollection> check,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = check.Value;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract -- caller might have NRTs disabled
        if (collection is null || GetCollectionCount(collection) == 0)
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked collection is not <see langword="null" /> and has one or more items,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// A <see langword="null" /> collection is treated as empty and passes without error.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<TCollection> IsEmpty<TCollection>(
        this Check<TCollection> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = check.Value;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract -- caller might have NRTs disabled
        if (collection is null || GetCollectionCount(collection) == 0)
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.Empty,
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Adds a validation error when the checked collection is <see langword="null" /> or has no items.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// A <see langword="null" /> collection triggers a validation error (treated as absent/empty).
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.NotEmpty)]
    public static Check<TCollection> IsNotEmpty<TCollection>(
        this Check<TCollection> check,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = check.Value;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (collection is not null && GetCollectionCount(collection) > 0)
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked collection is <see langword="null" /> or has no items,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// A <see langword="null" /> collection triggers a validation error (treated as absent/empty).
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<TCollection> IsNotEmpty<TCollection>(
        this Check<TCollection> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = check.Value;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (collection is not null && GetCollectionCount(collection) > 0)
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.NotEmpty,
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Adds a validation error when the checked immutable array is not empty.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    [ValidationRule(ValidationErrorCodes.Empty)]
    public static Check<ImmutableArray<TItem>> IsEmpty<TItem>(
        this Check<ImmutableArray<TItem>> check,
        bool shortCircuitOnError = false
    ) =>
        check.IsShortCircuited || check.Value.Length == 0 ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked immutable array is not empty,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<ImmutableArray<TItem>> IsEmpty<TItem>(
        this Check<ImmutableArray<TItem>> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.Length == 0 ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Empty,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked immutable array is empty.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    [ValidationRule(ValidationErrorCodes.NotEmpty)]
    public static Check<ImmutableArray<TItem>> IsNotEmpty<TItem>(
        this Check<ImmutableArray<TItem>> check,
        bool shortCircuitOnError = false
    ) =>
        check.IsShortCircuited || check.Value.Length > 0 ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked immutable array is empty,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<ImmutableArray<TItem>> IsNotEmpty<TItem>(
        this Check<ImmutableArray<TItem>> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.Length > 0 ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotEmpty,
                overrides,
                shortCircuitOnError
            );
    }
}
