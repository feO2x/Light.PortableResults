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
    public static Check<string?> IsEmpty(this Check<string?> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked string is neither <see langword="null" /> nor empty,
    /// applying the specified inline error overrides. Whitespace-only strings are not considered empty.
    /// </summary>
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
    public static Check<string?> IsNotEmpty(this Check<string?> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || !string.IsNullOrEmpty(check.Value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked string is <see langword="null" /> or empty,
    /// applying the specified inline error overrides. Whitespace-only strings are not considered empty.
    /// </summary>
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
    public static Check<Guid> IsEmpty(this Check<Guid> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value == Guid.Empty ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Empty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked GUID is not <see cref="Guid.Empty" />,
    /// applying the specified inline error overrides.
    /// </summary>
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
    public static Check<Guid> IsNotEmpty(this Check<Guid> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value != Guid.Empty ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotEmpty, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked GUID is <see cref="Guid.Empty" />,
    /// applying the specified inline error overrides.
    /// </summary>
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
