namespace Light.PortableResults.Validation.Assertions;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The checked value type.</typeparam>
    /// <param name="check">The current check.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, later assertions are skipped after a failure. The default is <see langword="true" />.
    /// </param>
    /// <returns>The updated check.</returns>
    public static Check<T> IsNotNull<T>(this Check<T> check, bool shortCircuitOnError = true)
    {
        if (check.IsShortCircuited || !check.IsValueNull)
        {
            return check;
        }

        var updatedCheck = check.AddError(BuiltInValidationErrorDefinitions.NotNull, respectShortCircuit: false);
        return updatedCheck.ShortCircuitOnErrorIfRequested(shortCircuitOnError);
    }
}
