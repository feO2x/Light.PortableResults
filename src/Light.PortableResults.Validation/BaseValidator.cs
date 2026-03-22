using System;
using System.Diagnostics.CodeAnalysis;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides common members for synchronous and asynchronous validators.
/// </summary>
/// <typeparam name="TSource">The type of the source value to validate.</typeparam>
public abstract class BaseValidator<TSource>
{
    /// <summary>
    /// Initializes a new instance of <see cref="BaseValidator{TSource}" />.
    /// </summary>
    /// <param name="validationContextFactory">The factory used to create validation contexts.</param>
    /// <param name="isAutomaticNullCheckingEnabled">
    /// Specifies whether the validator should automatically create a validation error for null source values.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationContextFactory" /> is null.</exception>
    protected BaseValidator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    )
    {
        ValidationContextFactory = validationContextFactory ??
                                   throw new ArgumentNullException(nameof(validationContextFactory));
        IsAutomaticNullCheckingEnabled = isAutomaticNullCheckingEnabled;
    }

    /// <summary>
    /// Gets the factory used to create validation contexts.
    /// </summary>
    protected IValidationContextFactory ValidationContextFactory { get; }

    /// <summary>
    /// Gets a value indicating whether automatic null checking is enabled.
    /// </summary>
    public bool IsAutomaticNullCheckingEnabled { get; }

    /// <summary>
    /// Tries to create the automatic null-validation error for the specified source value.
    /// </summary>
    /// <param name="value">The source value to inspect.</param>
    /// <param name="context">The active validation context.</param>
    /// <param name="rawTarget">The raw caller expression for the source value.</param>
    /// <param name="displayName">The display name for the value.</param>
    /// <returns><see langword="true" /> when an automatic null-validation error was added; otherwise, <see langword="false" />.</returns>
    protected bool TryHandleAutomaticNull(
        [NotNullWhen(false)] TSource? value,
        ValidationContext context,
        string rawTarget,
        string displayName
    )
    {
        if (!IsAutomaticNullCheckingEnabled || value is not null)
        {
            return false;
        }

        var target = context.GetAutomaticNullTarget(rawTarget);
        if (!context.TryCreateAutomaticNullError(value, target, displayName, out var error))
        {
            return false;
        }

        context.AddError(error);
        return true;
    }

    /// <summary>
    /// Validates the specified validation call arguments.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="target">The raw caller expression target.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="context" /> is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target" /> is null.</exception>
    protected static void ValidateCallArguments(ValidationContext context, string target)
    {
        if (context.IsDefault)
        {
            throw new ArgumentException("The validation context must not be the default instance.", nameof(context));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
    }

    /// <summary>
    /// Materializes the final public result from the shared validation context and validated value.
    /// </summary>
    /// <typeparam name="TValidated">The validated output type.</typeparam>
    /// <param name="context">The active validation context.</param>
    /// <param name="validatedValue">The validated value produced by the validator implementation.</param>
    /// <returns>The final validation result.</returns>
    protected static Result<TValidated> FinalizeValidation<TValidated>(
        ValidationContext context,
        ValidatedValue<TValidated> validatedValue
    )
    {
        if (context.TryGetErrors(out var errors))
        {
            return Result<TValidated>.Fail(errors);
        }

        if (validatedValue.TryGetValue(out var value))
        {
            return Result<TValidated>.Ok(value);
        }

        throw new InvalidOperationException(
            "The validator completed without validation errors but did not produce a validated value."
        );
    }
}
