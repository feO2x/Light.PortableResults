using System;
using Light.PortableResults.Validation.Definitions;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Adds a validation error when the checked GUID is not a version 7 UUID as defined by RFC 9562 (section 5.7).
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// The value passes only when its version field is <c>7</c> and its two most significant variant bits are
    /// <c>10</c>. <see cref="Guid.Empty" /> and version 4 GUIDs created by <see cref="Guid.NewGuid" /> therefore
    /// fail. Use <see cref="GuidExtensions.IsUuidV7(Guid)" /> to check the same invariant outside a check chain.
    /// </remarks>
    [ValidationRule(ValidationErrorCodes.UuidV7)]
    [ValidationRuleMessage("{displayName} must be a version 7 UUID")]
    public static Check<Guid> IsUuidV7(this Check<Guid> check, bool shortCircuitOnError = false) =>
        check.IsShortCircuited || check.Value.IsUuidV7() ?
            check :
            AddBuiltInError(check, BuiltInValidationErrorDefinitions.UuidV7, shortCircuitOnError);

    /// <summary>
    /// Adds a validation error when the checked GUID is not a version 7 UUID as defined by RFC 9562
    /// (section 5.7), applying the specified inline error overrides.
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
    /// The value passes only when its version field is <c>7</c> and its two most significant variant bits are
    /// <c>10</c>. <see cref="Guid.Empty" /> and version 4 GUIDs created by <see cref="Guid.NewGuid" /> therefore
    /// fail. Use <see cref="GuidExtensions.IsUuidV7(Guid)" /> to check the same invariant outside a check chain.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<Guid> IsUuidV7(
        this Check<Guid> check,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        EnsureErrorOverrides(overrides);
        return check.IsShortCircuited || check.Value.IsUuidV7() ?
            check :
            AddBuiltInErrorWithOverrides(
                check,
                BuiltInValidationErrorDefinitions.UuidV7,
                overrides,
                shortCircuitOnError
            );
    }
}
