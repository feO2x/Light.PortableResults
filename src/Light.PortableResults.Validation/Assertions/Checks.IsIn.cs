using System;
using System.Collections.Generic;

namespace Light.PortableResults.Validation.Assertions;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is outside the inclusive range defined by the specified boundaries.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <param name="check">The current check.</param>
    /// <param name="lowerBoundary">The inclusive lower boundary.</param>
    /// <param name="upperBoundary">The inclusive upper boundary.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, later assertions are skipped after a failure. The default is <see langword="false" />.
    /// </param>
    /// <returns>The updated check.</returns>
    public static Check<T> IsIn<T>(
        this Check<T> check,
        T lowerBoundary,
        T upperBoundary,
        bool shortCircuitOnError = false
    )
    {
        if (lowerBoundary is null)
        {
            throw new ArgumentNullException(nameof(lowerBoundary));
        }

        if (upperBoundary is null)
        {
            throw new ArgumentNullException(nameof(upperBoundary));
        }

        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        var comparer = Comparer<T>.Default;
        if (comparer.Compare(check.Value, lowerBoundary) >= 0 && comparer.Compare(check.Value, upperBoundary) <= 0)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.IsIn(
            check.Context.ErrorDefinitionCache,
            lowerBoundary,
            upperBoundary
        );
        var updatedCheck = check.AddError(definition, respectShortCircuit: false);
        return updatedCheck.ShortCircuitOnErrorIfRequested(shortCircuitOnError);
    }
}
