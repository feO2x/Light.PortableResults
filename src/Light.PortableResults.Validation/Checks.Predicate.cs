using System;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides assertions for <see cref="Check{T}" /> instances.
/// </summary>
public static partial class Checks
{
    /// <summary>
    /// Evaluates the predicate for the current value and adds one validation error when it returns
    /// <see langword="false" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="predicate">
    /// The condition the value must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="definition">
    /// The error definition used when the predicate fails. When <see langword="null" />, the built-in
    /// predicate definition is used.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> Must<T>(
        this Check<T> check,
        Func<T, bool> predicate,
        ValidationErrorDefinition? definition = null,
        bool shortCircuitOnError = false
    )
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (check.IsShortCircuited || predicate(check.Value))
        {
            return check;
        }

        return AddBuiltInError(check, definition ?? BuiltInValidationErrorDefinitions.Predicate, shortCircuitOnError);
    }

    /// <summary>
    /// Evaluates the predicate for the current value and adds one validation error backed by the built-in predicate
    /// definition when it returns <see langword="false" />, applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="predicate">
    /// The condition the value must satisfy. Must not be <see langword="null" />.
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
    /// Thrown when <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> Must<T>(
        this Check<T> check,
        Func<T, bool> predicate,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited || predicate(check.Value))
        {
            return check;
        }

        return AddBuiltInErrorWithOverrides(
            check,
            BuiltInValidationErrorDefinitions.Predicate,
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Evaluates the predicate for the current value and the scoped readonly validation context, then adds one
    /// validation error when it returns <see langword="false" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="predicate">
    /// The condition to evaluate. Receives a scoped read-only view of the validation context, enabling
    /// cross-field checks (e.g., inspecting previously collected errors) without mutating the context.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <param name="definition">
    /// The error definition used when the predicate fails. When <see langword="null" />, the built-in
    /// predicate definition is used.
    /// </param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// The target is normalized to its absolute path before the predicate is invoked, so the read-only
    /// context reflects the current fully-qualified target.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> Must<T>(
        this Check<T> check,
        Func<ReadOnlyValidationContext, T, bool> predicate,
        ValidationErrorDefinition? definition = null,
        bool shortCircuitOnError = false
    )
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (check.IsShortCircuited)
        {
            return check;
        }

        var normalizedCheck = check.NormalizeTargetIfNecessary();
        var context = normalizedCheck.CreateChildContext().AsReadOnly();
        if (predicate(context, normalizedCheck.Value))
        {
            return normalizedCheck;
        }

        return AddBuiltInError(
            normalizedCheck,
            definition ?? BuiltInValidationErrorDefinitions.Predicate,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Evaluates the predicate for the current value and the scoped readonly validation context, then adds one
    /// validation error backed by the built-in predicate definition when it returns <see langword="false" />,
    /// applying the specified inline error overrides.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="predicate">
    /// The condition to evaluate. Receives a scoped read-only view of the validation context, enabling
    /// cross-field checks (e.g., inspecting previously collected errors) without mutating the context.
    /// Must not be <see langword="null" />.
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
    /// <remarks>
    /// The target is normalized to its absolute path before the predicate is invoked, so the read-only
    /// context reflects the current fully-qualified target.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="predicate" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="overrides" /> has no field set, or when
    /// <see cref="ErrorOverrides.Message" /> is non-<see langword="null" /> but empty or whitespace.
    /// </exception>
    public static Check<T> Must<T>(
        this Check<T> check,
        Func<ReadOnlyValidationContext, T, bool> predicate,
        ErrorOverrides overrides,
        bool shortCircuitOnError = false
    )
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        EnsureErrorOverrides(overrides);
        if (check.IsShortCircuited)
        {
            return check;
        }

        var normalizedCheck = check.NormalizeTargetIfNecessary();
        var context = normalizedCheck.CreateChildContext().AsReadOnly();
        if (predicate(context, normalizedCheck.Value))
        {
            return normalizedCheck;
        }

        return AddBuiltInErrorWithOverrides(
            normalizedCheck,
            BuiltInValidationErrorDefinitions.Predicate,
            overrides,
            shortCircuitOnError
        );
    }

    /// <summary>
    /// Evaluates the predicate for the current value and adds one validation error created from the supplied
    /// template when it returns <see langword="false" />.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="predicate">
    /// The condition the value must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="template">
    /// The message template used to produce the error message. Must not be <see langword="null" />.
    /// </param>
    /// <param name="code">
    /// An optional error code. When <see langword="null" />, the built-in predicate code is used.
    /// </param>
    /// <param name="metadata">Optional structured metadata attached to the error.</param>
    /// <param name="shortCircuitOnError">
    /// When <see langword="true" />, marks the check as short-circuited after a failure so that subsequent
    /// assertions in the chain are skipped; defaults to <see langword="false" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="predicate" /> or <paramref name="template" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> Must<T>(
        this Check<T> check,
        Func<T, bool> predicate,
        IValidationErrorMessageTemplate template,
        string? code = null,
        MetadataObject? metadata = null,
        bool shortCircuitOnError = false
    )
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        if (check.IsShortCircuited || predicate(check.Value))
        {
            return check;
        }

        var definition = new TemplateValidationErrorDefinition(
            template,
            code ?? BuiltInValidationErrorDefinitions.Predicate.Code,
            metadata
        );
        return AddBuiltInError(check, definition, shortCircuitOnError);
    }

    /// <summary>
    /// Executes imperative custom validation logic against the current scoped validation context.
    /// The delegate may add zero, one, or many validation errors directly.
    /// </summary>
    /// <param name="check">The check carrying the value and validation context.</param>
    /// <param name="customValidation">
    /// The delegate that performs the validation. Receives a scoped <see cref="ValidationContext" />
    /// and the current value; it may call <c>context.AddError</c> zero or more times.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <returns>The current check for fluent chaining.</returns>
    /// <remarks>
    /// Unlike the <c>Must</c> overloads, <c>Custom</c> does not accept a <c>shortCircuitOnError</c>
    /// parameter because the delegate has full control over error emission. The delegate is not invoked
    /// at all when the check is already short-circuited.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="customValidation" /> is <see langword="null" />.
    /// </exception>
    public static Check<T> Custom<T>(this Check<T> check, Action<ValidationContext, T> customValidation)
    {
        if (customValidation is null)
        {
            throw new ArgumentNullException(nameof(customValidation));
        }

        if (check.IsShortCircuited)
        {
            return check;
        }

        var normalizedCheck = check.NormalizeTargetIfNecessary();
        customValidation(normalizedCheck.CreateChildContext(), normalizedCheck.Value);
        return normalizedCheck;
    }
}
