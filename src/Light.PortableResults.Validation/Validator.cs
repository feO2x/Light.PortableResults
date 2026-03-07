using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Light.PortableResults.Validation;

/// <summary>
/// Base class for synchronous validators that validate and optionally normalize a value of the same type.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public abstract class Validator<T> : BaseValidator<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Validator{T}" />.
    /// </summary>
    /// <param name="validationContextFactory">The factory used to create validation contexts.</param>
    /// <param name="isAutomaticNullCheckingEnabled">
    /// Specifies whether the validator should automatically create a validation error for null source values.
    /// </param>
    protected Validator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    )
        : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified value with a fresh validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValidationOutcome<T> Validate(
        T? value,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => Validate(value, ValidationContextFactory.CreateValidationContext(), target, displayName);

    /// <summary>
    /// Validates the specified value with the provided validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValidationOutcome<T> Validate(
        T? value,
        ValidationContext context,
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

        var validatedValue = PerformValidation(context, value!);
        return new ValidationOutcome<T>(validatedValue, context.ToErrors());
    }

    /// <summary>
    /// Validates the value and materializes failures as a non-generic <see cref="Result" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> when validation failed; otherwise, <see langword="false" />.</returns>
    public bool CheckForErrors(
        T? value,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => CheckForErrors(value, ValidationContextFactory.CreateValidationContext(), out failure, target, displayName);

    /// <summary>
    /// Validates the value with the specified context and materializes failures as a non-generic <see cref="Result" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> when validation failed; otherwise, <see langword="false" />.</returns>
    public bool CheckForErrors(
        T? value,
        ValidationContext context,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        var outcome = Validate(value, context, target, displayName);
        if (outcome.IsValid)
        {
            failure = default;
            return false;
        }

        failure = outcome.ToFailureResult();
        return true;
    }

    /// <summary>
    /// Validates the specified value and returns the normalized value on success.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validatedValue">The validated value on success.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public bool TryValidate(
        T? value,
        [MaybeNullWhen(false)] out T validatedValue,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => TryValidate(
        value,
        ValidationContextFactory.CreateValidationContext(),
        out validatedValue,
        out failure,
        target,
        displayName
    );

    /// <summary>
    /// Validates the specified value with the provided context and returns the normalized value on success.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="validatedValue">The validated value on success.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public bool TryValidate(
        T? value,
        ValidationContext context,
        [MaybeNullWhen(false)] out T validatedValue,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        var outcome = Validate(value, context, target, displayName);
        if (outcome.IsValid)
        {
            validatedValue = outcome.Value;
            failure = default;
            return true;
        }

        validatedValue = default;
        failure = outcome.ToFailureResult();
        return false;
    }

    /// <summary>
    /// Performs the actual validation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The non-null value being validated.</param>
    /// <returns>The validated value.</returns>
    protected abstract T PerformValidation(ValidationContext context, T value);
}

/// <summary>
/// Base class for synchronous validators that validate one type and transform it into another validated output.
/// </summary>
/// <typeparam name="TSource">The source type to validate.</typeparam>
/// <typeparam name="TValidated">The validated output type.</typeparam>
public abstract class Validator<TSource, TValidated> : BaseValidator<TSource>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Validator{TSource, TValidated}" />.
    /// </summary>
    /// <param name="validationContextFactory">The factory used to create validation contexts.</param>
    /// <param name="isAutomaticNullCheckingEnabled">
    /// Specifies whether the validator should automatically create a validation error for null source values.
    /// </param>
    protected Validator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    )
        : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified source value and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValidationOutcome<TValidated> Validate(
        TSource? value,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => Validate(value, ValidationContextFactory.CreateValidationContext(), target, displayName);

    /// <summary>
    /// Validates the specified source value with the provided context and transforms it into a validated output.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation outcome.</returns>
    public ValidationOutcome<TValidated> Validate(
        TSource? value,
        ValidationContext context,
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

        var validatedValue = PerformValidation(context, value!);
        var errors = context.ToErrors();
        return errors.IsEmpty
            ? new ValidationOutcome<TValidated>(validatedValue)
            : new ValidationOutcome<TValidated>(default!, errors);
    }

    /// <summary>
    /// Validates the value and materializes failures as a non-generic <see cref="Result" />.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> when validation failed; otherwise, <see langword="false" />.</returns>
    public bool CheckForErrors(
        TSource? value,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => CheckForErrors(value, ValidationContextFactory.CreateValidationContext(), out failure, target, displayName);

    /// <summary>
    /// Validates the value with the specified context and materializes failures as a non-generic <see cref="Result" />.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> when validation failed; otherwise, <see langword="false" />.</returns>
    public bool CheckForErrors(
        TSource? value,
        ValidationContext context,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        var outcome = Validate(value, context, target, displayName);
        if (outcome.IsValid)
        {
            failure = default;
            return false;
        }

        failure = outcome.ToFailureResult();
        return true;
    }

    /// <summary>
    /// Validates the specified source value and returns the transformed validated output on success.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="validatedValue">The validated output on success.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public bool TryValidate(
        TSource? value,
        [MaybeNullWhen(false)] out TValidated validatedValue,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => TryValidate(
        value,
        ValidationContextFactory.CreateValidationContext(),
        out validatedValue,
        out failure,
        target,
        displayName
    );

    /// <summary>
    /// Validates the specified source value with the provided context and returns the transformed validated output on success.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="validatedValue">The validated output on success.</param>
    /// <param name="failure">The failure result when validation fails.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns><see langword="true" /> on success; otherwise, <see langword="false" />.</returns>
    public bool TryValidate(
        TSource? value,
        ValidationContext context,
        [MaybeNullWhen(false)] out TValidated validatedValue,
        out Result failure,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        var outcome = Validate(value, context, target, displayName);
        if (outcome.IsValid)
        {
            validatedValue = outcome.Value;
            failure = default;
            return true;
        }

        validatedValue = default;
        failure = outcome.ToFailureResult();
        return false;
    }

    /// <summary>
    /// Performs the actual validation and transformation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The non-null source value.</param>
    /// <returns>The transformed validated output.</returns>
    protected abstract TValidated PerformValidation(ValidationContext context, TSource value);
}
