using System;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the state required to validate a single value with minimal overhead.
/// </summary>
/// <typeparam name="T">The type of the value being validated.</typeparam>
public readonly struct Check<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="Check{T}" />.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="target">The raw or normalized target.</param>
    /// <param name="displayName">The human-readable display name.</param>
    /// <param name="value">The value being validated.</param>
    /// <param name="isTargetNormalized">Indicates whether <paramref name="target" /> is already normalized.</param>
    /// <param name="isShortCircuited">Indicates whether further checks should be skipped.</param>
    public Check(
        ValidationContext context,
        string target,
        string displayName,
        T value,
        bool isTargetNormalized,
        bool isShortCircuited
    )
    {
        context.ThrowIfDefault();
        Context = context;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Value = value;
        IsTargetNormalized = isTargetNormalized;
        IsShortCircuited = isShortCircuited;
    }

    /// <summary>
    /// Gets the validation context.
    /// </summary>
    public ValidationContext Context { get; }

    /// <summary>
    /// Gets the raw or normalized target path.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the display name for the current value.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets a value indicating whether the target path has already been normalized.
    /// </summary>
    public bool IsTargetNormalized { get; }

    /// <summary>
    /// Gets a value indicating whether subsequent checks should be skipped.
    /// </summary>
    public bool IsShortCircuited { get; }

    /// <summary>
    /// Gets a value indicating whether the current value is <see langword="null" />.
    /// </summary>
    public bool IsValueNull => Value is null;

    /// <summary>
    /// Creates a new check with a different value.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <returns>The updated check.</returns>
    public Check<T> WithValue(T value) =>
        new (Context, Target, DisplayName, value, IsTargetNormalized, IsShortCircuited);

    /// <summary>
    /// Creates a new check with a different display name.
    /// </summary>
    /// <param name="displayName">The display name to use.</param>
    /// <returns>The updated check.</returns>
    public Check<T> WithDisplayName(string displayName) =>
        new (
            Context,
            Target,
            displayName ?? throw new ArgumentNullException(nameof(displayName)),
            Value,
            IsTargetNormalized,
            IsShortCircuited
        );

    /// <summary>
    /// Creates a new short-circuited check.
    /// </summary>
    /// <returns>The updated check.</returns>
    public Check<T> ShortCircuit() => new (Context, Target, DisplayName, Value, IsTargetNormalized, true);

    /// <summary>
    /// Creates a new short-circuited check if requested.
    /// </summary>
    /// <param name="shortCircuitOnError">When <see langword="true" />, the check is short-circuited after a failure.</param>
    /// <returns>The updated check.</returns>
    public Check<T> ShortCircuitOnErrorIfRequested(bool shortCircuitOnError) =>
        shortCircuitOnError ? ShortCircuit() : this;

    /// <summary>
    /// Normalizes the target if necessary.
    /// </summary>
    /// <returns>The normalized check.</returns>
    public Check<T> NormalizeTargetIfNecessary()
    {
        if (IsTargetNormalized)
        {
            return this;
        }

        var normalizedTarget = Context.ComposeTarget(Target, isTargetNormalized: false);
        var displayName = string.Equals(DisplayName, Target, StringComparison.Ordinal) ? normalizedTarget : DisplayName;
        return new Check<T>(Context, normalizedTarget, displayName, Value, isTargetNormalized: true, IsShortCircuited);
    }

    /// <summary>
    /// Creates the readonly message context for this check.
    /// </summary>
    /// <returns>The readonly message context.</returns>
    public ValidationErrorMessageContext<T> CreateMessageContext()
    {
        var normalizedCheck = NormalizeTargetIfNecessary();
        return normalizedCheck.Context.CreateAbsoluteMessageContext(
            normalizedCheck.Value,
            normalizedCheck.Target,
            normalizedCheck.DisplayName
        );
    }

    /// <summary>
    /// Adds the specified error to the context.
    /// </summary>
    /// <param name="error">The error to add.</param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(Error error, bool respectShortCircuit = true)
    {
        if (respectShortCircuit && IsShortCircuited)
        {
            return this;
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        if (error.Target is null)
        {
            error = error with { Target = normalizedCheck.Target };
        }

        normalizedCheck.Context.AddError(error);
        return normalizedCheck;
    }

    /// <summary>
    /// Adds a validation error with the specified generated message and optional details.
    /// </summary>
    /// <param name="message">The generated message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="target">
    /// The optional explicit normalized target, composed relative to the current context.
    /// </param>
    /// <param name="category">The error category.</param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(
        ValidationErrorMessage message,
        string? code = null,
        MetadataObject? metadata = null,
        string? target = null,
        ErrorCategory category = ErrorCategory.Validation,
        bool respectShortCircuit = true
    )
    {
        if (respectShortCircuit && IsShortCircuited)
        {
            return this;
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        var resolvedTarget = target is null ?
            normalizedCheck.Target :
            normalizedCheck.Context.ComposeTarget(target, isTargetNormalized: true);
        normalizedCheck.Context.AddError(
            message.ToError(code, resolvedTarget, category, metadata)
        );
        return normalizedCheck;
    }

    /// <summary>
    /// Adds a validation error from the specified reusable definition and optional override details.
    /// </summary>
    /// <param name="definition">The reusable definition.</param>
    /// <param name="code">The optional override for the definition's default code.</param>
    /// <param name="metadata">The optional override for the definition's default metadata.</param>
    /// <param name="target">
    /// The optional override normalized target, composed relative to the current context.
    /// </param>
    /// <param name="category">The optional override for the definition's default category.</param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(
        ValidationErrorDefinition definition,
        string? code = null,
        MetadataObject? metadata = null,
        string? target = null,
        ErrorCategory? category = null,
        bool respectShortCircuit = true
    )
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (respectShortCircuit && IsShortCircuited)
        {
            return this;
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        var messageContext = normalizedCheck.CreateMessageContext();
        var message = definition.ProvideMessage(in messageContext);
        var resolvedTarget = ResolveDefinitionTarget(normalizedCheck, definition.Target, target);
        normalizedCheck.Context.AddError(
            message.ToError(
                code ?? definition.Code,
                resolvedTarget,
                category ?? definition.Category,
                metadata ?? definition.Metadata
            )
        );
        return normalizedCheck;
    }

    /// <summary>
    /// Adds a validation error with the specified message template and optional details.
    /// </summary>
    /// <param name="template">The message template.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="target">
    /// The optional explicit normalized target, composed relative to the current context.
    /// </param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(
        IValidationErrorMessageTemplate template,
        string? code = null,
        MetadataObject? metadata = null,
        string? target = null,
        bool respectShortCircuit = true
    )
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        if (respectShortCircuit && IsShortCircuited)
        {
            return this;
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        var messageContext = normalizedCheck.CreateMessageContext();
        var message = template.ProvideMessage(in messageContext);
        return normalizedCheck.AddError(
            message,
            code,
            metadata,
            target,
            respectShortCircuit: false
        );
    }

    /// <summary>
    /// Adds a validation error with the specified message template, typed parameter, and optional details.
    /// </summary>
    /// <typeparam name="TParameter">The parameter type.</typeparam>
    /// <param name="template">The message template.</param>
    /// <param name="parameter">The additional typed parameter.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="target">
    /// The optional explicit normalized target, composed relative to the current context.
    /// </param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError<TParameter>(
        IValidationErrorMessageTemplate<TParameter> template,
        TParameter parameter,
        string? code = null,
        MetadataObject? metadata = null,
        string? target = null,
        bool respectShortCircuit = true
    )
    {
        if (template is null)
        {
            throw new ArgumentNullException(nameof(template));
        }

        if (respectShortCircuit && IsShortCircuited)
        {
            return this;
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        var messageContext = normalizedCheck.CreateMessageContext();
        var message = template.ProvideMessage(in messageContext, parameter);
        return normalizedCheck.AddError(
            message,
            code,
            metadata,
            target,
            respectShortCircuit: false
        );
    }

    /// <summary>
    /// Adds a validation error with the specified message and optional details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="target">
    /// The optional explicit normalized target, composed relative to the current context.
    /// </param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(
        string message,
        string? code = null,
        MetadataObject? metadata = null,
        string? target = null,
        bool respectShortCircuit = true
    ) =>
        AddError(
            new ValidationErrorMessage(message),
            code,
            metadata,
            target,
            ErrorCategory.Validation,
            respectShortCircuit
        );

    /// <summary>
    /// Implicitly converts the check to its value.
    /// </summary>
    /// <param name="check">The check to convert.</param>
    public static implicit operator T(Check<T> check) => check.Value;

    private static string? ResolveDefinitionTarget(
        Check<T> normalizedCheck,
        string? definitionTarget,
        string? overrideTarget
    )
    {
        if (overrideTarget is not null)
        {
            return normalizedCheck.Context.ComposeTarget(overrideTarget, isTargetNormalized: true);
        }

        return definitionTarget ?? normalizedCheck.Target;
    }
}
