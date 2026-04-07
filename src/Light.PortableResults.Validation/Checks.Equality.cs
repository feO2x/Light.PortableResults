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
    public static Check<T> IsEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    ) =>
        check.IsEqualTo(comparativeValue, EqualityComparer<T>.Default, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value does not equal the specified expected value.
    /// </summary>
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
    /// Adds a validation error when the checked value equals the specified disallowed value.
    /// </summary>
    public static Check<T> IsNotEqualTo<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    ) =>
        check.IsNotEqualTo(comparativeValue, EqualityComparer<T>.Default, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value equals the specified disallowed value.
    /// </summary>
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
}
