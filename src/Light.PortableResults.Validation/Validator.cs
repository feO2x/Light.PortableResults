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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validationContextFactory" /> is null.</exception>
    protected Validator(
        IValidationContextFactory validationContextFactory,
        bool isAutomaticNullCheckingEnabled = true
    ) : base(validationContextFactory, isAutomaticNullCheckingEnabled) { }

    /// <summary>
    /// Validates the specified value with a fresh validation context.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="target">The raw caller expression for the value.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The validation result.</returns>
    public Result<T> Validate(
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
    /// <returns>The validation result.</returns>
    public Result<T> Validate(
        T? value,
        ValidationContext context,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => FinalizeValidation(context, ValidateValue(value, context, target, displayName));

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
        var result = Validate(value, context, target, displayName);
        if (result.IsValid)
        {
            failure = default;
            return false;
        }

        failure = Result.Fail(result.Errors);
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
        var result = Validate(value, context, target, displayName);
        if (result.TryGetValue(out validatedValue))
        {
            failure = default;
            return true;
        }

        failure = Result.Fail(result.Errors);
        validatedValue = default;
        return false;
    }

    internal ValidatedValue<T> ValidateValue(
        T? value,
        ValidationContext context,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        ValidateCallArguments(context, target);
        displayName ??= target;
        if (TryHandleAutomaticNull(value, context, target, displayName))
        {
            return ValidatedValue<T>.NoValue;
        }

        return PerformValidation(context, value);
    }

    /// <summary>
    /// Performs the actual validation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The value being validated.</param>
    /// <returns>
    /// A successful <see cref="ValidatedValue{T}" /> when validation produced a non-null value; otherwise,
    /// <see cref="ValidatedValue{T}.NoValue" /> when the shared <see cref="ValidationContext" /> already contains errors.
    /// </returns>
    protected abstract ValidatedValue<T> PerformValidation(ValidationContext context, T value);
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
    /// <returns>The validation result.</returns>
    public Result<TValidated> Validate(
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
    /// <returns>The validation result.</returns>
    public Result<TValidated> Validate(
        TSource? value,
        ValidationContext context,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    ) => FinalizeValidation(context, ValidateValue(value, context, target, displayName));

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
    ) =>
        CheckForErrors(value, ValidationContextFactory.CreateValidationContext(), out failure, target, displayName);

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
        var result = Validate(value, context, target, displayName);
        if (result.IsValid)
        {
            failure = default;
            return false;
        }

        failure = Result.Fail(result.Errors);
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
        var result = Validate(value, context, target, displayName);
        if (result.TryGetValue(out validatedValue))
        {
            failure = default;
            return true;
        }

        failure = Result.Fail(result.Errors);
        validatedValue = default;
        return false;
    }

    internal ValidatedValue<TValidated> ValidateValue(
        TSource? value,
        ValidationContext context,
        [CallerArgumentExpression("value")] string target = "",
        string? displayName = null
    )
    {
        ValidateCallArguments(context, target);
        displayName ??= target;
        if (TryHandleAutomaticNull(value, context, target, displayName))
        {
            return ValidatedValue<TValidated>.NoValue;
        }

        return PerformValidation(context, value);
    }

    /// <summary>
    /// Performs the actual validation and transformation logic.
    /// </summary>
    /// <param name="context">The active validation context.</param>
    /// <param name="value">The source value being validated.</param>
    /// <returns>
    /// A successful <see cref="ValidatedValue{TValidated}" /> when validation produced a non-null output; otherwise,
    /// <see cref="ValidatedValue{TValidated}.NoValue" /> when the shared <see cref="ValidationContext" /> already contains errors.
    /// </returns>
    protected abstract ValidatedValue<TValidated> PerformValidation(ValidationContext context, TSource value);
}
