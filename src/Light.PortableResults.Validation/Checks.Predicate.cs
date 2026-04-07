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
    /// Evaluates the predicate for the current value and the scoped readonly validation context, then adds one
    /// validation error when it returns <see langword="false" />.
    /// </summary>
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
    /// Evaluates the predicate for the current value and adds one validation error created from the supplied
    /// template when it returns <see langword="false" />.
    /// </summary>
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
