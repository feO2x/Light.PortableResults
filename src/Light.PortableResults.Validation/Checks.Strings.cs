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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    [ValidationRule(ValidationErrorCodes.NotNullOrWhiteSpace)]
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
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minLength">The minimum number of characters required. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minLength" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.MinLength)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.MinLength, nameof(minLength))]
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minLength">The minimum number of characters required. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minLength" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxLength">The maximum number of characters allowed. Must be zero or greater.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength" /> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.MaxLength)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.MaxLength, nameof(maxLength))]
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="maxLength">The maximum number of characters allowed. Must be zero or greater.</param>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minLength">
    /// The minimum character count (inclusive). Must be zero or greater.
    /// </param>
    /// <param name="maxLength">
    /// The maximum character count (inclusive). Must be greater than or equal to
    /// <paramref name="minLength" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minLength" /> or <paramref name="maxLength" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="maxLength" /> is less than <paramref name="minLength" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.LengthInRange)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.MinLength, nameof(minLength))]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.MaxLength, nameof(maxLength))]
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="minLength">
    /// The minimum character count (inclusive). Must be zero or greater.
    /// </param>
    /// <param name="maxLength">
    /// The maximum character count (inclusive). Must be greater than or equal to
    /// <paramref name="minLength" />.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minLength" /> or <paramref name="maxLength" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="maxLength" /> is less than <paramref name="minLength" />, or when
    /// <paramref name="overrides" /> has no field set, or when <see cref="ErrorOverrides.Message" /> is
    /// non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="regex">
    /// The compiled regular expression the string must match. Must not be <see langword="null" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="regex" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="regex">
    /// The compiled regular expression the string must match. Must not be <see langword="null" />.
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="regex" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="pattern">
    /// The regular expression pattern the string must match. Must not be <see langword="null" />.
    /// The compiled <see cref="Regex" /> is cached inside the validation context for the lifetime of the
    /// validation operation.
    /// </param>
    /// <param name="options">
    /// Options applied when compiling and matching the pattern;
    /// defaults to <see cref="RegexOptions.None" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pattern" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.Pattern)]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.Pattern, nameof(pattern))]
    [ValidationRuleMetadata(ValidationErrorMetadataKeys.RegexOptions, nameof(options))]
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="pattern">
    /// The regular expression pattern the string must match. Must not be <see langword="null" />.
    /// The compiled <see cref="Regex" /> is cached inside the validation context for the lifetime of the
    /// validation operation.
    /// </param>
    /// <param name="overrides">
    /// Inline overrides for the built-in error details. Pass a plain <see cref="string" /> to replace only
    /// the message, or supply a full <see cref="ErrorOverrides" /> to also override the code, category, or
    /// metadata. At least one field must be set.
    /// </param>
    /// <param name="options">
    /// Options applied when compiling and matching the pattern;
    /// defaults to <see cref="RegexOptions.None" />.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pattern" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Uses a lightweight heuristic: the string must contain exactly one <c>@</c> sign that is neither
    /// the first nor the last character. This is intentionally permissive — it rejects obviously invalid
    /// addresses without performing full RFC-5321 validation.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.Email)]
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
    /// Uses a lightweight heuristic: the string must contain exactly one <c>@</c> sign that is neither
    /// the first nor the last character. This is intentionally permissive — it rejects obviously invalid
    /// addresses without performing full RFC-5321 validation.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Every character is tested with <see cref="char.IsDigit(char)" />. An empty string always fails.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.DigitsOnly)]
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
    /// Every character is tested with <see cref="char.IsDigit(char)" />. An empty string always fails.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Every character is tested with <see cref="char.IsLetterOrDigit(char)" />. An empty string always fails.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
    [ValidationRule(ValidationErrorCodes.LettersAndDigitsOnly)]
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
    /// Every character is tested with <see cref="char.IsLetterOrDigit(char)" />. An empty string always fails.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the checked string is <see langword="null" />. With the default string normalizer,
    /// <see langword="null" /> is converted to <see cref="string.Empty" /> before this assertion,
    /// so this only occurs when using a no-op normalizer.
    /// </exception>
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
