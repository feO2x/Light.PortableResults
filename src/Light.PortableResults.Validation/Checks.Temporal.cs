using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked date and time is not represented in UTC.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from: <see cref="DateTime.UtcNow" />, <see cref="DateTime.SpecifyKind" />, a custom JSON
    /// converter, and non-JSON transports can all produce <see cref="DateTimeKind.Utc" />. If the validation
    /// context applies a value normalizer that rewrites the <see cref="DateTime" />, this assertion describes
    /// the normalized value.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter, this means the payload
    /// has to carry a trailing <c>Z</c>: a numeric offset — including <c>+00:00</c>, which denotes the same
    /// instant as <c>Z</c> — is converted to server-local time and deserializes as
    /// <see cref="DateTimeKind.Local" />, so it is rejected. That is guidance about the JSON wire format, not
    /// part of this assertion's contract.
    /// </para>
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.Utc)]
    [ValidationRuleMessage("{displayName} must be represented in UTC")]
    public static Check<DateTime> IsUtc(this Check<DateTime> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value.Kind == DateTimeKind.Utc ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Utc, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked date and time is not represented in UTC, applying the
    /// specified inline error overrides.
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
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from: <see cref="DateTime.UtcNow" />, <see cref="DateTime.SpecifyKind" />, a custom JSON
    /// converter, and non-JSON transports can all produce <see cref="DateTimeKind.Utc" />. If the validation
    /// context applies a value normalizer that rewrites the <see cref="DateTime" />, this assertion describes
    /// the normalized value.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter, this means the payload
    /// has to carry a trailing <c>Z</c>: a numeric offset — including <c>+00:00</c>, which denotes the same
    /// instant as <c>Z</c> — is converted to server-local time and deserializes as
    /// <see cref="DateTimeKind.Local" />, so it is rejected. That is guidance about the JSON wire format, not
    /// part of this assertion's contract.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<DateTime> IsUtc(
        this Check<DateTime> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.Kind == DateTimeKind.Utc ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Utc,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked date and time is not a local date and time.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from. If the validation context applies a value normalizer that rewrites the
    /// <see cref="DateTime" />, this assertion describes the normalized value.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter,
    /// <see cref="DateTimeKind.Local" /> is what a numeric UTC offset deserializes to — including
    /// <c>+00:00</c> — after conversion to the server's time zone. The resulting wall-clock value therefore
    /// depends on how the server is configured, which is rarely what a portable API wants.
    /// </para>
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.Local)]
    [ValidationRuleMessage("{displayName} must be a local date and time")]
    public static Check<DateTime> IsLocal(this Check<DateTime> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value.Kind == DateTimeKind.Local ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Local, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked date and time is not a local date and time, applying the
    /// specified inline error overrides.
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
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from. If the validation context applies a value normalizer that rewrites the
    /// <see cref="DateTime" />, this assertion describes the normalized value.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter,
    /// <see cref="DateTimeKind.Local" /> is what a numeric UTC offset deserializes to — including
    /// <c>+00:00</c> — after conversion to the server's time zone. The resulting wall-clock value therefore
    /// depends on how the server is configured, which is rarely what a portable API wants.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<DateTime> IsLocal(
        this Check<DateTime> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.Kind == DateTimeKind.Local ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Local,
                overrides,
                shortCircuitOnError
            );
    }

    /// <summary>
    /// Adds a validation error when the checked date and time specifies a time zone.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from. If the validation context applies a value normalizer that rewrites the
    /// <see cref="DateTime" />, this assertion describes the normalized value. Note that
    /// <c>default(DateTime)</c> is <see cref="DateTimeKind.Unspecified" /> and therefore passes: combine this
    /// assertion with a range or equality check when a missing value must be rejected.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter, this means the payload
    /// must carry neither a trailing <c>Z</c> nor a numeric offset — a wall-clock timestamp such as
    /// <c>2026-08-02T10:00:00</c> whose zone the client and server agree on out of band.
    /// </para>
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.Unspecified)]
    [ValidationRuleMessage("{displayName} must not specify a time zone")]
    public static Check<DateTime> IsUnspecified(this Check<DateTime> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value.Kind == DateTimeKind.Unspecified ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.Unspecified, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked date and time specifies a time zone, applying the specified
    /// inline error overrides.
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
    /// <para>
    /// The contract is the <see cref="DateTime.Kind" /> of the value this check sees, independent of where that
    /// value came from. If the validation context applies a value normalizer that rewrites the
    /// <see cref="DateTime" />, this assertion describes the normalized value. Note that
    /// <c>default(DateTime)</c> is <see cref="DateTimeKind.Unspecified" /> and therefore passes: combine this
    /// assertion with a range or equality check when a missing value must be rejected.
    /// </para>
    /// <para>
    /// When values arrive as JSON through the default <c>System.Text.Json</c> converter, this means the payload
    /// must carry neither a trailing <c>Z</c> nor a numeric offset — a wall-clock timestamp such as
    /// <c>2026-08-02T10:00:00</c> whose zone the client and server agree on out of band.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<DateTime> IsUnspecified(
        this Check<DateTime> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.Kind == DateTimeKind.Unspecified ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.Unspecified,
                overrides,
                shortCircuitOnError
            );
    }
}
