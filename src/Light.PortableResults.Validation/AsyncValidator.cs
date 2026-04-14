using System;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.Validation.Targeting;

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
    ) : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified value with a fresh validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public ValueTask<Result<T>> ValidateAsync(
        T? value,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        ValidateAsync(
            value,
            ValidationContextFactory.CreateValidationContext(),
            cancellationToken,
            ValidationTarget.Relative(string.Empty, isNormalized: true),
            displayName
        );

    /// <summary>
    /// Validates the specified value with a fresh validation context and explicit target semantics.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public ValueTask<Result<T>> ValidateAsync(
        T? value,
        ValidationTarget target,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        ValidateAsync(
            value,
            ValidationContextFactory.CreateValidationContext(),
            cancellationToken,
            target,
            displayName
        );

    /// <summary>
    /// Validates the specified value with the provided validation context.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>The validation result.</returns>
    public async ValueTask<Result<T>> ValidateAsync(
        T? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        FinalizeValidation(
            context,
            await ValidateChildValueAsync(
                    value,
                    context,
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName
                )
               .ConfigureAwait(false)
        );

    /// <summary>
    /// Validates the specified value with the provided validation context and explicit target semantics.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>The validation result.</returns>
    public async ValueTask<Result<T>> ValidateAsync(
        T? value,
        ValidationContext context,
        CancellationToken cancellationToken,
        ValidationTarget target,
        string? displayName = null
    ) =>
        FinalizeValidation(
            context,
            await ValidateChildValueAsync(value, context, cancellationToken, target, displayName).ConfigureAwait(false)
        );

    /// <summary>
    /// Validates the specified value with the provided validation context.
    /// This method is used to validate child values, thus producing a <see cref="ValidatedValue{T}" /> instead
    /// of a <see cref="Result{T}" />. Call ValidateAsync to run the complete validation pipeline.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>A value task containing the validated and potentially normalized value.</returns>
    public ValueTask<ValidatedValue<T>> ValidateChildValueAsync(
        T? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) => ValidateChildValueAsync(
        value,
        context,
        cancellationToken,
        ValidationTarget.Relative(string.Empty, isNormalized: true),
        displayName
    );

    /// <summary>
    /// Validates the specified value with the provided validation context and explicit target semantics.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>A value task containing the validated and potentially normalized value.</returns>
    public async ValueTask<ValidatedValue<T>> ValidateChildValueAsync(
        T? value,
        ValidationContext context,
        CancellationToken cancellationToken,
        ValidationTarget target,
        string? displayName = null
    )
    {
        ValidateCallArguments(context, target);
        displayName ??= target.Input;
        if (TryHandleAutomaticNull(value, context, target, displayName))
        {
            return ValidatedValue<T>.NoValue;
        }

        var checkpoint = context.CreateCheckpoint();
        return await PerformValidationAsync(context, checkpoint, value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the actual asynchronous validation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="checkpoint">
    /// This object can track whether errors were added to the validation context during this PerformValidationAsync call.
    /// Use it to create <see cref="ValidatedValue{T}" /> instances more easily.
    /// </param>
    /// <param name="value">The value being validated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A successful <see cref="ValidatedValue{T}" /> when validation produced a non-null value; otherwise,
    /// <see cref="ValidatedValue{T}.NoValue" /> when new errors were added since <paramref name="checkpoint" /> was
    /// created.
    /// </returns>
    protected abstract ValueTask<ValidatedValue<T>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationContextFactory" /> is null.</exception>
    protected AsyncValidator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    ) : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified source value with a fresh validation context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public ValueTask<Result<TValidated>> ValidateAsync(
        TSource? value,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        ValidateAsync(
            value,
            ValidationContextFactory.CreateValidationContext(),
            cancellationToken,
            ValidationTarget.Relative(string.Empty, isNormalized: true),
            displayName
        );

    /// <summary>
    /// Validates the specified source value with a fresh validation context and explicit target semantics.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public ValueTask<Result<TValidated>> ValidateAsync(
        TSource? value,
        ValidationTarget target,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        ValidateAsync(
            value,
            ValidationContextFactory.CreateValidationContext(),
            cancellationToken,
            target,
            displayName
        );

    /// <summary>
    /// Validates the specified source value with the provided validation context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public async ValueTask<Result<TValidated>> ValidateAsync(
        TSource? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) =>
        FinalizeValidation(
            context,
            await ValidateChildValueAsync(
                    value,
                    context,
                    cancellationToken,
                    ValidationTarget.Relative(string.Empty, isNormalized: true),
                    displayName
                )
               .ConfigureAwait(false)
        );

    /// <summary>
    /// Validates the specified source value with the provided validation context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public async ValueTask<Result<TValidated>> ValidateAsync(
        TSource? value,
        ValidationContext context,
        CancellationToken cancellationToken,
        ValidationTarget target,
        string? displayName = null
    ) =>
        FinalizeValidation(
            context,
            await ValidateChildValueAsync(value, context, cancellationToken, target, displayName).ConfigureAwait(false)
        );

    /// <summary>
    /// Validates the specified value with the provided validation context.
    /// This method is used to validate child values, thus producing a <see cref="ValidatedValue{T}" /> instead
    /// of a <see cref="Result{T}" />. Call ValidateAsync to run the complete validation pipeline.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>A value task containing the validated and potentially normalized value.</returns>
    public ValueTask<ValidatedValue<TValidated>> ValidateChildValueAsync(
        TSource? value,
        ValidationContext context,
        CancellationToken cancellationToken = default,
        string? displayName = null
    ) => ValidateChildValueAsync(
        value,
        context,
        cancellationToken,
        ValidationTarget.Relative(string.Empty, isNormalized: true),
        displayName
    );

    /// <summary>
    /// Validates the specified value with the provided validation context and explicit target semantics.
    /// </summary>
    /// <param name="value">The value to be validated.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The optional token to cancel the asynchronous operation.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="displayName">The optional display name which is used in formatted error messages.</param>
    /// <returns>A value task containing the validated and potentially normalized value.</returns>
    public async ValueTask<ValidatedValue<TValidated>> ValidateChildValueAsync(
        TSource? value,
        ValidationContext context,
        CancellationToken cancellationToken,
        ValidationTarget target,
        string? displayName = null
    )
    {
        ValidateCallArguments(context, target);
        displayName ??= target.Input;
        if (TryHandleAutomaticNull(value, context, target, displayName))
        {
            return ValidatedValue<TValidated>.NoValue;
        }

        var checkpoint = context.CreateCheckpoint();
        return await PerformValidationAsync(context, checkpoint, value, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the actual asynchronous validation and transformation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="checkpoint">
    /// This object can track whether errors were added to the validation context during this PerformValidationAsync call.
    /// Use it to create <see cref="ValidatedValue{T}" /> instances more easily.
    /// </param>
    /// <param name="value">The source value being validated.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A successful <see cref="ValidatedValue{TValidated}" /> when validation produced a non-null output; otherwise,
    /// <see cref="ValidatedValue{TValidated}.NoValue" /> when new errors were added since
    /// <paramref name="checkpoint" /> was created.
    /// </returns>
    protected abstract ValueTask<ValidatedValue<TValidated>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        TSource value,
        CancellationToken cancellationToken
    );
}
