using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked value is <see langword="null" />.
    /// </summary>
    public static Check<T> IsNotNull<T>(this Check<T> check, bool shortCircuitOnError = true) =>
        check.IsShortCircuited || !check.IsValueNull ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotNull, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked value is <see langword="null" />,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<T> IsNotNull<T>(
        this Check<T> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = true
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || !check.IsValueNull ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotNull,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked value is not <see langword="null" />.
    /// </summary>
    public static Check<T> IsNull<T>(this Check<T> check, bool shortCircuitOnError = true)
    {
        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.Null, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked value is not <see langword="null" />,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<T> IsNull<T>(
        this Check<T> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = true
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || check.IsValueNull)
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.Null,
            overrides,
            shortCircuitOnError
        );
    }
}
