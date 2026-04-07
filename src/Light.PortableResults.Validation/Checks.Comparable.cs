using System.Collections.Generic;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is not greater than the specified boundary.
    /// </summary>
    public static Check<T> IsGreaterThan<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsGreaterThan));
        if (Comparer<T>.Default.Compare(value, comparativeValue) > 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.GreaterThan(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is less than the specified boundary.
    /// </summary>
    public static Check<T> IsGreaterThanOrEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsGreaterThanOrEqualTo));
        if (Comparer<T>.Default.Compare(value, comparativeValue) >= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is not less than the specified boundary.
    /// </summary>
    public static Check<T> IsLessThan<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsLessThan));
        if (Comparer<T>.Default.Compare(value, comparativeValue) < 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.LessThan(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is greater than the specified boundary.
    /// </summary>
    public static Check<T> IsLessThanOrEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsLessThanOrEqualTo));
        if (Comparer<T>.Default.Compare(value, comparativeValue) <= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.LessThanOrEqualTo(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies outside the inclusive range.
    /// </summary>
    public static Check<T> IsIn<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsIn));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) >= 0 && comparer.Compare(value, upperBoundary) <= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsIn(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies within the inclusive range.
    /// </summary>
    public static Check<T> IsNotIn<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsNotIn));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) < 0 || comparer.Compare(value, upperBoundary) > 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsNotIn(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies outside the exclusive range.
    /// </summary>
    public static Check<T> IsInExclusiveRange<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsInExclusiveRange));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) > 0 && comparer.Compare(value, upperBoundary) < 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsInExclusiveRange(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }
}
