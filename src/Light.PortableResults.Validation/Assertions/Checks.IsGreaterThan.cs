using System;
using System.Collections.Generic;

namespace Light.PortableResults.Validation.Assertions;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is not greater than the specified boundary.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <param name="check">The current check.</param>
    /// <param name="comparativeValue">The boundary value.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, later assertions are skipped after a failure. The default is <see langword="false" />.
    /// </param>
    /// <returns>The updated check.</returns>
    public static Check<T> IsGreaterThan<T>(
        this Check<T> check,
        T comparativeValue,
        bool shortCircuitOnError = false
    )
    {
        if (comparativeValue is null)
        {
            throw new ArgumentNullException(nameof(comparativeValue));
        }

        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        if (Comparer<T>.Default.Compare(check.Value, comparativeValue) > 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.GreaterThan(
            check.Context.ErrorDefinitionCache,
            comparativeValue
        );
        var updatedCheck = check.AddError(definition, respectShortCircuit: false);
        return updatedCheck.ShortCircuitOnErrorIfRequested(shortCircuitOnError);
    }
}
