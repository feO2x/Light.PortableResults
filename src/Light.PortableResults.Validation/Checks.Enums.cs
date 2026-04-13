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
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Uses <see cref="Enum.IsDefined" /> internally. Flags-combined values that are not themselves
    /// declared members of <typeparamref name="TEnum" /> are considered invalid.
    /// </remarks>
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
    /// Adds a validation error when the checked enum value is not defined by the enum type,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// Uses <see cref="Enum.IsDefined" /> internally. Flags-combined values that are not themselves
    /// declared members of <typeparamref name="TEnum" /> are considered invalid.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<TEnum> IsInEnum<TEnum>(
        this Check<TEnum> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TEnum : struct, Enum
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || Enum.IsDefined(typeof(TEnum), check.Value))
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.IsInEnum<TEnum>(),
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Adds a validation error when the checked enum value is not defined by the enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Uses <see cref="Enum.IsDefined" /> internally. Flags-combined values that are not themselves
    /// declared members of <typeparamref name="TEnum" /> are considered invalid.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked nullable value has no value. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
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
    /// Adds a validation error when the checked enum value is not defined by the enum type,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
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
    /// <remarks>
    /// Uses <see cref="Enum.IsDefined" /> internally. Flags-combined values that are not themselves
    /// declared members of <typeparamref name="TEnum" /> are considered invalid.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked nullable value has no value. Guard against this by calling
    /// <see cref="IsNotNull{T}(Check{T}, bool)" /> before this assertion.
    /// </exception>
    public static Check<TEnum?> IsInEnum<TEnum>(
        this Check<TEnum?> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
        where TEnum : struct, Enum
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredValue(check.Value, nameof(IsInEnum));
        if (Enum.IsDefined(typeof(TEnum), value))
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.IsInEnum<TEnum>(),
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Adds a validation error when the checked string does not equal a defined enum member name.
    /// With the default string normalizer, <see langword="null" /> is normalized to <see cref="string.Empty" />
    /// before this assertion sees the value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type whose member names are the set of valid values.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="ignoreCase">
    /// When <see langword="true" />, comparison uses ordinal case-insensitive matching; otherwise,
    /// ordinal case-sensitive matching is used. Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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

    /// <summary>
    /// Adds a validation error when the checked string does not equal a defined enum member name,
    /// applying the specified inline error overrides. With the default string normalizer,
    /// <see langword="null" /> is normalized to <see cref="string.Empty" /> before this assertion sees the value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type whose member names are the set of valid values.</typeparam>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="ignoreCase">
    /// When <see langword="true" />, comparison uses ordinal case-insensitive matching; otherwise,
    /// ordinal case-sensitive matching is used. Defaults to <see langword="false" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    public static Check<string?> IsEnumName<TEnum>(
        this Check<string?> check,
        ErrorOverrides overrides,
        bool ignoreCase = false,
        bool shortCircuitOnError = false
    )
        where TEnum : struct, Enum
    {
        EnsureErrorOverrides(overrides);
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
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }
}
