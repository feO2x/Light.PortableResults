namespace Light.PortableResults.Validation.Assertions;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale.
    /// </summary>
    public static Check<decimal> HasPrecisionAndScale(
        this Check<decimal> check,
        int precision,
        int scale,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var info = GetPrecisionScaleInfo(check.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked decimal exceeds the specified precision or scale.
    /// </summary>
    public static Check<decimal?> HasPrecisionAndScale(
        this Check<decimal?> check,
        int precision,
        int scale,
        bool ignoreTrailingZeros = false,
        bool shortCircuitOnError = false
    )
    {
        EnsurePrecisionScale(precision, scale);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = check.Value;
        if (!value.HasValue)
        {
            throw CreateNullValueException(nameof(HasPrecisionAndScale));
        }

        var info = GetPrecisionScaleInfo(value.Value, ignoreTrailingZeros);
        if (info.Digits <= precision && info.Scale <= scale)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.PrecisionScale(
            check.Context.ErrorDefinitionCache,
            precision,
            scale,
            ignoreTrailingZeros
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }
}
