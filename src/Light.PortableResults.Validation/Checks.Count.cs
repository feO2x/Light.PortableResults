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
    public static Check<string?> HasCount(
        this Check<string?> check,
        int expectedCount,
        ValidationErrorOverrides overrides,
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
    public static Check<string?> HasMinCount(
        this Check<string?> check,
        int minCount,
        ValidationErrorOverrides overrides,
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
    public static Check<string?> HasMaxCount(
        this Check<string?> check,
        int maxCount,
        ValidationErrorOverrides overrides,
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
    public static Check<TCollection> HasCount<TCollection>(
        this Check<TCollection> check,
        int expectedCount,
        ValidationErrorOverrides overrides,
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
    public static Check<TCollection> HasMinCount<TCollection>(
        this Check<TCollection> check,
        int minCount,
        ValidationErrorOverrides overrides,
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
    public static Check<TCollection> HasMaxCount<TCollection>(
        this Check<TCollection> check,
        int maxCount,
        ValidationErrorOverrides overrides,
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
    public static Check<ImmutableArray<TItem>> HasCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int expectedCount,
        ValidationErrorOverrides overrides,
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
    public static Check<ImmutableArray<TItem>> HasMinCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int minCount,
        ValidationErrorOverrides overrides,
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
    public static Check<ImmutableArray<TItem>> HasMaxCount<TItem>(
        this Check<ImmutableArray<TItem>> check,
        int maxCount,
        ValidationErrorOverrides overrides,
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
