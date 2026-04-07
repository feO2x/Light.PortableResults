using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked enum value is not defined by the enum type.
    /// </summary>
    public static Check<TEnum> IsInEnum<TEnum>(this Check<TEnum> check, bool shortCircuitOnError = false)
        where TEnum : struct, Enum
    {
        if (check.IsShortCircuited || Enum.IsDefined(typeof(TEnum), check.Value))
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.IsInEnum<TEnum>(), shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked enum value is not defined by the enum type.
    /// </summary>
    public static Check<TEnum?> IsInEnum<TEnum>(this Check<TEnum?> check, bool shortCircuitOnError = false)
        where TEnum : struct, Enum
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsInEnum));
        if (Enum.IsDefined(typeof(TEnum), value))
        {
            return check;
        }

        return AddBuiltInError(check, BuiltInValidationErrorDefinitions.IsInEnum<TEnum>(), shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not equal a defined enum member name.
    /// With the default string normalizer, <see langword="null" /> is normalized to <see cref="string.Empty" />
    /// before this assertion sees the value.
    /// </summary>
    public static Check<string?> IsEnumName<TEnum>(
        this Check<string?> check,
        bool ignoreCase = false,
        bool shortCircuitOnError = false
    )
        where TEnum : struct, Enum
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(IsEnumName));
        if (IsEnumNameDefined<TEnum>(value, ignoreCase))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.EnumName<TEnum>(
            check.Context.ErrorDefinitionCache,
            ignoreCase
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }
}
