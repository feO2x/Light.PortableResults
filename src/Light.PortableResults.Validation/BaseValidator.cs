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
    /// Tries to create the automatic null-validation outcome for the specified source value.
    /// </summary>
    /// <typeparam name="TValidated">The type of the validated output.</typeparam>
    /// <param name="value">The source value to inspect.</param>
    /// <param name="context">The active validation context.</param>
    /// <param name="rawTarget">The raw caller expression for the source value.</param>
    /// <param name="displayName">The display name for the value.</param>
    /// <param name="outcome">The produced validation outcome when null checking fails.</param>
    /// <returns><see langword="true" /> when null handling produced an outcome; otherwise, <see langword="false" />.</returns>
    protected bool TryCreateAutomaticNullOutcome<TValidated>(
        [NotNullWhen(false)] TSource? value,
        ValidationContext context,
        string rawTarget,
        string displayName,
        out ValidationOutcome<TValidated> outcome
    )
    {
        if (!IsAutomaticNullCheckingEnabled || !context.Options.CreateAutomaticNullErrors || value is not null)
        {
            outcome = default;
            return false;
        }

        var target = context.GetAutomaticNullTarget(rawTarget);
        var error = context.CreateErrorForAutomaticNullCheck(target, displayName);
        outcome = new ValidationOutcome<TValidated>(default!, new Errors(error));
        return true;
    }
}
