using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Light.PortableResults.Validation;

/// <summary>
/// Base class for asynchronous validators that validate and optionally normalize a value of the same type.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public abstract class AsyncValidator<T> : BaseValidator<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncValidator{T}" />.
    /// </summary>
    /// <param name="validationContextFactory">The factory used to create validation contexts.</param>
    /// <param name="isAutomaticNullCheckingEnabled">
    /// Specifies whether the validator should automatically create a validation error for null source values.
    /// </param>
    protected AsyncValidator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    )
        : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified value with a fresh validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValueTask<ValidationOutcome<T>> ValidateAsync(
        T? value,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => ValidateAsync(value, ValidationContextFactory.CreateValidationContext(), cancellationToken, target, displayName);

    /// <summary>
    /// Validates the specified value with the provided validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public async ValueTask<ValidationOutcome<T>> ValidateAsync(
        T? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        displayName ??= target;
        if (TryCreateAutomaticNullOutcome<T>(value, context, target, displayName, out var nullOutcome))
        {
            return nullOutcome;
        }

        var validatedValue = await PerformValidationAsync(context, value!, cancellationToken).ConfigureAwait(false);
        return new ValidationOutcome<T>(validatedValue, context.ToErrors());
    }

    /// <summary>
    /// Performs the actual asynchronous validation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The non-null value being validated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value.</returns>
    protected abstract ValueTask<T> PerformValidationAsync(
        ValidationContext context,
        T value,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// Base class for asynchronous validators that validate one type and transform it into another validated output.
/// </summary>
/// <typeparam name="TSource">The source type to validate.</typeparam>
/// <typeparam name="TValidated">The validated output type.</typeparam>
public abstract class AsyncValidator<TSource, TValidated> : BaseValidator<TSource>
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncValidator{TSource, TValidated}" />.
    /// </summary>
    /// <param name="validationContextFactory">The factory used to create validation contexts.</param>
    /// <param name="isAutomaticNullCheckingEnabled">
    /// Specifies whether the validator should automatically create a validation error for null source values.
    /// </param>
    protected AsyncValidator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    )
        : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified source value with a fresh validation context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValueTask<ValidationOutcome<TValidated>> ValidateAsync(
        TSource? value,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => ValidateAsync(value, ValidationContextFactory.CreateValidationContext(), cancellationToken, target, displayName);

    /// <summary>
    /// Validates the specified source value with the provided validation context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public async ValueTask<ValidationOutcome<TValidated>> ValidateAsync(
        TSource? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        displayName ??= target;
        if (TryCreateAutomaticNullOutcome<TValidated>(value, context, target, displayName, out var nullOutcome))
        {
            return nullOutcome;
        }

        var validatedValue = await PerformValidationAsync(context, value!, cancellationToken).ConfigureAwait(false);
        var errors = context.ToErrors();
        return errors.IsEmpty
            ? new ValidationOutcome<TValidated>(validatedValue)
            : new ValidationOutcome<TValidated>(default!, errors);
    }

    /// <summary>
    /// Performs the actual asynchronous validation and transformation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The non-null source value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transformed validated output.</returns>
    protected abstract ValueTask<TValidated> PerformValidationAsync(
        ValidationContext context,
        TSource value,
        CancellationToken cancellationToken
    );
}
