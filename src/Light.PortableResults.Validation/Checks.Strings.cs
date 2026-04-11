using System;
using System.Text.RegularExpressions;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked string is <see langword="null" />, empty, or whitespace.
    /// With the default string normalizer, <see langword="null" /> is normalized to <see cref="string.Empty" />
    /// before this assertion sees the value.
    /// </summary>
    public static Check<string> IsNotNullOrWhiteSpace(this Check<string> check, bool shortCircuitOnError = false)
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = check.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return AddBuiltInError(check, BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace, shortCircuitOnError);
        }

        return check.WithValue(value);
    }

    /// <summary>
    /// Adds a validation error when the checked string is <see langword="null" />, empty, or whitespace,
    /// applying the specified inline error overrides. With the default string normalizer,
    /// <see langword="null" /> is normalized to <see cref="string.Empty" /> before this assertion sees the value.
    /// </summary>
    public static Check<string> IsNotNullOrWhiteSpace(
        this Check<string> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = check.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace,
                overrides,
                shortCircuitOnError
            );
        }

        return check.WithValue(value);
    }

    /// <summary>
    /// Adds a validation error when the checked string is shorter than the specified minimum length.
    /// </summary>
    public static Check<string> HasMinLength(
        this Check<string> check,
        int minLength,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minLength, nameof(minLength));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMinLength));
        if (value.Length >= minLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinLength(check.Context.ErrorDefinitionCache, minLength);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string is shorter than the specified minimum length,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> HasMinLength(
        this Check<string> check,
        int minLength,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(minLength, nameof(minLength));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMinLength));
        if (value.Length >= minLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MinLength(check.Context.ErrorDefinitionCache, minLength);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string exceeds the specified maximum length.
    /// </summary>
    public static Check<string?> HasMaxLength(
        this Check<string?> check,
        int maxLength,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxLength, nameof(maxLength));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMaxLength));
        if (value.Length <= maxLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxLength(check.Context.ErrorDefinitionCache, maxLength);
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string exceeds the specified maximum length,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string?> HasMaxLength(
        this Check<string?> check,
        int maxLength,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureLengthBoundary(maxLength, nameof(maxLength));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasMaxLength));
        if (value.Length <= maxLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.MaxLength(check.Context.ErrorDefinitionCache, maxLength);
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length lies outside the inclusive range.
    /// </summary>
    public static Check<string> HasLengthIn(
        this Check<string> check,
        int minLength,
        int maxLength,
        bool shortCircuitOnError = false
    )
    {
        EnsureMinMax(minLength, maxLength, nameof(minLength), nameof(maxLength));
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasLengthIn));
        if (value.Length >= minLength && value.Length <= maxLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.LengthIn(
            check.Context.ErrorDefinitionCache,
            minLength,
            maxLength
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string length lies outside the inclusive range,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> HasLengthIn(
        this Check<string> check,
        int minLength,
        int maxLength,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureMinMax(minLength, maxLength, nameof(minLength), nameof(maxLength));
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(HasLengthIn));
        if (value.Length >= minLength && value.Length <= maxLength)
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.LengthIn(
            check.Context.ErrorDefinitionCache,
            minLength,
            maxLength
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not match the specified regular expression.
    /// </summary>
    public static Check<string> Matches(this Check<string> check, Regex regex, bool shortCircuitOnError = false)
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex));
        }

        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(Matches));
        if (regex.IsMatch(value))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Matches(
            check.Context.ErrorDefinitionCache,
            regex.ToString(),
            regex.Options
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not match the specified regular expression,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> Matches(
        this Check<string> check,
        Regex regex,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        if (regex is null)
        {
            throw new ArgumentNullException(nameof(regex));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(Matches));
        if (regex.IsMatch(value))
        {
            return check;
        }

        var definition = BuiltInValidationErrorDefinitions.Matches(
            check.Context.ErrorDefinitionCache,
            regex.ToString(),
            regex.Options
        );
        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not match the specified regular expression pattern.
    /// </summary>
    public static Check<string> Matches(
        this Check<string> check,
        string pattern,
        RegexOptions options = RegexOptions.None,
        bool shortCircuitOnError = false
    )
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(Matches));
        var definition =
            BuiltInValidationErrorDefinitions.Matches(check.Context.ErrorDefinitionCache, pattern, options);
        if (definition.Regex.IsMatch(value))
        {
            return check;
        }

        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not match the specified regular expression pattern,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> Matches(
        this Check<string> check,
        string pattern,
        ErrorOverrides overrides,
        RegexOptions options = RegexOptions.None,
        bool shortCircuitOnError = false
    )
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(Matches));
        var definition =
            BuiltInValidationErrorDefinitions.Matches(check.Context.ErrorDefinitionCache, pattern, options);
        if (definition.Regex.IsMatch(value))
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(check, definition, overrides, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not look like an email address.
    /// </summary>
    public static Check<string> IsEmail(this Check<string> check, bool shortCircuitOnError = false)
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(IsEmail));
        return LooksLikeEmail(value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Email, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string does not look like an email address,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> IsEmail(
        this Check<string> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(IsEmail));
        return LooksLikeEmail(value) ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Email,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked string is empty or contains a non-digit character.
    /// </summary>
    public static Check<string> ContainsOnlyDigits(this Check<string> check, bool shortCircuitOnError = false)
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(ContainsOnlyDigits));
        return ContainsOnlyDigitsCore(value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.DigitsOnly, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string is empty or contains a non-digit character,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> ContainsOnlyDigits(
        this Check<string> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(ContainsOnlyDigits));
        return ContainsOnlyDigitsCore(value) ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.DigitsOnly,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked string is empty or contains characters other than letters or digits.
    /// </summary>
    public static Check<string> ContainsOnlyLettersAndDigits(this Check<string> check, bool shortCircuitOnError = false)
    {
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(ContainsOnlyLettersAndDigits));
        return ContainsOnlyLettersAndDigitsCore(value) ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.LettersAndDigitsOnly, shortCircuitOnError);
    }

    /// <summary>
    /// Adds a validation error when the checked string is empty or contains characters other than letters or digits,
    /// applying the specified inline error overrides.
    /// </summary>
    public static Check<string> ContainsOnlyLettersAndDigits(
        this Check<string> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var value = GetRequiredString(check.Value, nameof(ContainsOnlyLettersAndDigits));
        return ContainsOnlyLettersAndDigitsCore(value) ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.LettersAndDigitsOnly,
                overrides,
                shortCircuitOnError
            );
    }
}
