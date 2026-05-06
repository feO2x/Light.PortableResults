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
    /// Adds a validation error when the checked value is not greater than the specified boundary.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The exclusive lower boundary; the checked value must be strictly greater than this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.GreaterThan, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
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
    /// Adds a validation error when the checked value is not greater than the specified boundary,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The exclusive lower boundary; the checked value must be strictly greater than this.
    /// Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsGreaterThan<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is less than the specified boundary.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The inclusive lower boundary; the checked value must be greater than or equal to this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.GreaterThanOrEqualTo, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
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
    /// Adds a validation error when the checked value is less than the specified boundary,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The inclusive lower boundary; the checked value must be greater than or equal to this.
    /// Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsGreaterThanOrEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is not less than the specified boundary.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The exclusive upper boundary; the checked value must be strictly less than this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.LessThan, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
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
    /// Adds a validation error when the checked value is not less than the specified boundary,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The exclusive upper boundary; the checked value must be strictly less than this.
    /// Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsLessThan<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is greater than the specified boundary.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The inclusive upper boundary; the checked value must be less than or equal to this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.LessThanOrEqualTo, ValidationRuleMetadataShape.TypedComparison)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.ComparativeValue, nameof(comparativeValue))]
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
    /// Adds a validation error when the checked value is greater than the specified boundary,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="comparativeValue">
    /// The inclusive upper boundary; the checked value must be less than or equal to this.
    /// Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="comparativeValue" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsLessThanOrEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(comparativeValue, comparativeValue, nameof(comparativeValue), nameof(comparativeValue));
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies outside the inclusive range.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The inclusive lower boundary of the range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The inclusive upper boundary of the range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.InRange, ValidationRuleMetadataShape.TypedRange)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.LowerBoundary, nameof(lowerBoundary))]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.UpperBoundary, nameof(upperBoundary))]
    public static Check<T> IsInRange<T>(
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

        var value = GetRequiredValue(check.Value, nameof(IsInRange));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) >= 0 && comparer.Compare(value, upperBoundary) <= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsInRange(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies outside the inclusive range,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The inclusive lower boundary of the range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The inclusive upper boundary of the range. Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsInRange<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsInRange));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) >= 0 && comparer.Compare(value, upperBoundary) <= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsInRange(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies within the inclusive range.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The inclusive lower boundary of the forbidden range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The inclusive upper boundary of the forbidden range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.NotInRange, ValidationRuleMetadataShape.TypedRange)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.LowerBoundary, nameof(lowerBoundary))]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.UpperBoundary, nameof(upperBoundary))]
    public static Check<T> IsNotInRange<T>(
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

        var value = GetRequiredValue(check.Value, nameof(IsNotInRange));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) < 0 || comparer.Compare(value, upperBoundary) > 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsNotInRange(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies within the inclusive range,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The inclusive lower boundary of the forbidden range. Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The inclusive upper boundary of the forbidden range. Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsNotInRange<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsNotInRange));
        var comparer = Comparer<T>.Default;
        if (comparer.Compare(value, lowerBoundary) < 0 || comparer.Compare(value, upperBoundary) > 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsNotInRange(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value lies outside the exclusive range.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The exclusive lower boundary of the range; the checked value must be strictly greater than this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The exclusive upper boundary of the range; the checked value must be strictly less than this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.ExclusiveRange, ValidationRuleMetadataShape.TypedRange)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.LowerBoundary, nameof(lowerBoundary))]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.UpperBoundary, nameof(upperBoundary))]
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

    /// <summary>
    /// Adds a validation error when the checked value lies outside the exclusive range,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="lowerBoundary">
    /// The exclusive lower boundary of the range; the checked value must be strictly greater than this.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="upperBoundary">
    /// The exclusive upper boundary of the range; the checked value must be strictly less than this.
    /// Must not be <see langword="null" />.
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
    /// <remarks>Uses <see cref="Comparer{T}.Default" /> for the comparison.</remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="lowerBoundary" /> or <paramref name="upperBoundary" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked value is <see langword="null" />. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<T> IsInExclusiveRange<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureRangeBoundaries(lowerBoundary, upperBoundary, nameof(lowerBoundary), nameof(upperBoundary));
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }
}
