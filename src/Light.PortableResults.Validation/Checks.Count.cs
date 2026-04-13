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
    /// Adds a validation error when the checked string length is not equal to the specified count.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of characters required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasCount(
        this Check<string?> check,
        int expectedCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasCount));
        if (value.Length == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length is not equal to the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of characters required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasCount(
        this Check<string?> check,
        int expectedCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasCount));
        if (value.Length == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length is less than the specified count.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of characters required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasMinCount(
        this Check<string?> check,
        int minCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMinCount));
        if (value.Length >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length is less than the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of characters required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasMinCount(
        this Check<string?> check,
        int minCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMinCount));
        if (value.Length >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length exceeds the specified count.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of characters allowed. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasMaxCount(
        this Check<string?> check,
        int maxCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMaxCount));
        if (value.Length <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length exceeds the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of characters allowed. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> HasMaxCount(
        this Check<string?> check,
        int maxCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMaxCount));
        if (value.Length <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count is not equal to the specified count.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of elements required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasCount<TCollection>(
        this Check<TCollection> check,
        int expectedCount,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasCount));
        if (GetCollectionCount(collection) == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count is not equal to the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of elements required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasCount<TCollection>(
        this Check<TCollection> check,
        int expectedCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasCount));
        if (GetCollectionCount(collection) == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count is less than the specified count.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of elements required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasMinCount<TCollection>(
        this Check<TCollection> check,
        int minCount,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasMinCount));
        if (GetCollectionCount(collection) >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count is less than the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of elements required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasMinCount<TCollection>(
        this Check<TCollection> check,
        int minCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasMinCount));
        if (GetCollectionCount(collection) >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count exceeds the specified count.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of elements allowed. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasMaxCount<TCollection>(
        this Check<TCollection> check,
        int maxCount,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasMaxCount));
        if (GetCollectionCount(collection) <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enumerable count exceeds the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TCollection">The enumerable collection type.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of elements allowed. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked collection is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TCollection> HasMaxCount<TCollection>(
        this Check<TCollection> check,
        int maxCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TCollection : IEnumerable
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var collection = GetRequiredCollection(check.Value, nameof(HasMaxCount));
        if (GetCollectionCount(collection) <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count is not equal to the specified count.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of elements required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int expectedCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        if (check.IsShortCircuited || check.Value.Length == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count is not equal to the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="expectedCount">The exact number of elements required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="expectedCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int expectedCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(expectedCount, nameof(expectedCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || check.Value.Length == expectedCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Count(check.Context.ErrorDefinitionCache, expectedCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count is less than the specified count.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of elements required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasMinCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int minCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        if (check.IsShortCircuited || check.Value.Length >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count is less than the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minCount">The minimum number of elements required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasMinCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int minCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minCount, nameof(minCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || check.Value.Length >= minCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinCount(check.Context.ErrorDefinitionCache, minCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count exceeds the specified count.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of elements allowed. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasMaxCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int maxCount,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        if (check.IsShortCircuited || check.Value.Length <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked immutable-array count exceeds the specified count,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TItem">The element type of the immutable array.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxCount">The maximum number of elements allowed. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxCount" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<ImmutableArray<TItem>> HasMaxCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int maxCount,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxCount, nameof(maxCount));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || check.Value.Length <= maxCount)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxCount(check.Context.ErrorDefinitionCache, maxCount);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }
}
