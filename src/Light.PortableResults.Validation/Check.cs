using System;
using System.Diagnostics.CodeAnalysis;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the state required to validate a single value with minimal overhead.
/// </summary>
/// <typeparam name="T">The type of the value being validated.</typeparam>
public readonly struct Check<T>
{
    private readonly string? _resolvedAbsoluteTarget;

    /// <summary>
    /// Initializes a new instance of <see cref="Check{T}" />.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <param name="target">The target descriptor.</param>
    /// <param name="displayName">
    /// The human-readable display name, or <see langword="null" /> to derive it from the resolved target at
    /// message-formatting time.
    /// </param>
    /// <param name="value">The value being validated.</param>
    /// <param name="resolvedAbsoluteTarget">
    /// The already-resolved absolute target, or <see langword="null" /> when it has not been materialized yet.
    /// </param>
    /// <param name="isShortCircuited">Indicates whether further checks should be skipped.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="context" /> is the default instance.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is the default instance.</exception>
    public Check(
        ValidationContext context,
        ValidationTarget target,
        string? displayName,
        T value,
        string? resolvedAbsoluteTarget,
        bool isShortCircuited
    )
    {
        context.ThrowIfDefault();
        if (target.IsDefault)
        {
            throw new ArgumentException("The validation target must not be the default instance.", nameof(target));
        }

        Context = context;
        TargetDescriptor = target;
        DisplayName = displayName;
        Value = value;
        _resolvedAbsoluteTarget = resolvedAbsoluteTarget;
        IsShortCircuited = isShortCircuited;
    }

    /// <summary>
    /// Gets the validation context.
    /// </summary>
    public ValidationContext Context { get; }

    /// <summary>
    /// Gets the explicit target descriptor supplied when the check was created.
    /// </summary>
    public ValidationTarget TargetDescriptor { get; }

    /// <summary>
    /// Gets the original target input or the resolved absolute target once it has been materialized.
    /// </summary>
    public string Target => _resolvedAbsoluteTarget ?? TargetDescriptor.Input;

    /// <summary>
    /// Gets the display name for the current value, or <see langword="null" /> when the display name should be
    /// derived from the resolved target at message-formatting time.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets a value indicating whether subsequent checks should be skipped.
    /// </summary>
    public bool IsShortCircuited { get; }

    /// <summary>
    /// Gets a value indicating whether the current value is <see langword="null" />.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsValueNull => Value is null;

    /// <summary>
    /// Creates a new check with a different value.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <returns>The updated check.</returns>
    public Check<TValue> WithValue<TValue>(TValue value) =>
        new (Context, TargetDescriptor, DisplayName, value, _resolvedAbsoluteTarget, IsShortCircuited);

    /// <summary>
    /// Creates a new check with a different display name.
    /// </summary>
    /// <param name="displayName">The display name to use.</param>
    /// <returns>The updated check.</returns>
    public Check<T> WithDisplayName(string displayName) =>
        new (
            Context,
            TargetDescriptor,
            displayName ?? throw new ArgumentNullException(nameof(displayName)),
            Value,
            _resolvedAbsoluteTarget,
            IsShortCircuited
        );

    /// <summary>
    /// Creates a new short-circuited check.
    /// </summary>
    /// <returns>The updated check.</returns>
    public Check<T> ShortCircuit() =>
        new (Context, TargetDescriptor, DisplayName, Value, _resolvedAbsoluteTarget, true);

    /// <summary>
    /// Creates a new short-circuited check if requested.
    /// </summary>
    /// <param name="shortCircuitOnError">When <see langword="true" />, the check is short-circuited after a failure.</param>
    /// <returns>The updated check.</returns>
    public Check<T> ShortCircuitOnErrorIfRequested(bool shortCircuitOnError) =>
        shortCircuitOnError ? ShortCircuit() : this;

    /// <summary>
    /// Resolves the target to an absolute path if necessary.
    /// </summary>
    /// <returns>The check with a resolved absolute target.</returns>
    public Check<T> NormalizeTargetIfNecessary()
    {
        if (_resolvedAbsoluteTarget is not null)
        {
            return this;
        }

        var resolvedTarget = Context.ResolveTarget(TargetDescriptor);
        return new Check<T>(Context, TargetDescriptor, DisplayName, Value, resolvedTarget, IsShortCircuited);
    }

    /// <summary>
    /// Creates a child validation context for the current check target.
    /// </summary>
    /// <returns>The child validation context.</returns>
    public ValidationContext CreateChildContext() =>
        Context.ForAbsolute(GetResolvedAbsoluteTarget(), isNormalized: true);

    /// <summary>
    /// Creates a child validation context for the specified member below the current check target.
    /// </summary>
    /// <param name="memberName">The member segment to append.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="memberName" /> is already normalized; otherwise,
    /// <see langword="false" />.
    /// </param>
    /// <returns>The child validation context.</returns>
    public ValidationContext CreateChildContextForMember(string memberName, bool isNormalized = false) =>
        CreateChildContext().ForRelative(memberName, isNormalized);

    /// <summary>
    /// Creates a child validation context for the specified collection index below the current check target.
    /// </summary>
    /// <param name="index">The zero-based collection index.</param>
    /// <returns>The child validation context.</returns>
    public ValidationContext CreateChildContextForIndex(int index) =>
        CreateChildContext().ForIndex(index);

    /// <summary>
    /// Creates the readonly message context for this check.
    /// </summary>
    /// <returns>The readonly message context.</returns>
    public ValidationErrorMessageContext<T> CreateMessageContext()
    {
        var normalizedCheck = NormalizeTargetIfNecessary();
        return normalizedCheck.CreateMessageContextCore();
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
            error = error with { Target = normalizedCheck.GetResolvedAbsoluteTarget() };
        }

        normalizedCheck.Context.AddError(error);
        return normalizedCheck;
    }

    /// <summary>
    /// Adds a validation error from the specified reusable definition and optional override details.
    /// </summary>
    /// <param name="definition">The reusable definition.</param>
    /// <param name="code">The optional override for the definition's default code.</param>
    /// <param name="metadata">The optional override for the definition's default metadata.</param>
    /// <param name="target">
    /// The optional override target. Relative targets are composed with the current validation scope, absolute targets
    /// are used unchanged, and caller-expression targets are normalized before composition.
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
        ValidationTarget? target = null,
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

        var cache = Context.ErrorTemplates.MessageCache;
        return cache is not null && definition.TryGetStableMessageProvider(Context.AsReadOnly(), out var provider) ?
            AddErrorWithMessageCaching(definition, code, metadata, target, category, provider, cache) :
            AddErrorWithoutMessageCaching(definition, code, metadata, target, category);
    }

    private Check<T> AddErrorWithoutMessageCaching(
        ValidationErrorDefinition definition,
        string? code,
        MetadataObject? metadata,
        ValidationTarget? target,
        ErrorCategory? category
    )
    {
        var normalizedCheck = NormalizeTargetIfNecessary();
        var messageContext = normalizedCheck.CreateMessageContextCore();
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

    private Check<T> AddErrorWithMessageCaching(
        ValidationErrorDefinition definition,
        string? code,
        MetadataObject? metadata,
        ValidationTarget? target,
        ErrorCategory? category,
        object provider,
        IValidationErrorMessageCache cache
    )
    {
        var key = new ValidationErrorMessageCacheKey(
            provider,
            TargetDescriptor,
            Context.TargetPrefix,
            DisplayName,
            Context.Options.CultureInfo
        );
        if (cache.TryGet(key, out var entry))
        {
            var resolvedTarget = target is null && definition.Target is null ?
                entry.ResolvedTarget :
                ResolveOverrideOrDefinitionTarget(entry.ResolvedTarget, definition.Target, target);
            Context.AddError(
                entry.Message.ToError(
                    code ?? definition.Code,
                    resolvedTarget,
                    category ?? definition.Category,
                    metadata ?? definition.Metadata
                )
            );
            return new Check<T>(
                Context,
                TargetDescriptor,
                DisplayName,
                Value,
                entry.ResolvedTarget,
                IsShortCircuited
            );
        }

        var normalizedCheck = NormalizeTargetIfNecessary();
        var messageContext = normalizedCheck.CreateMessageContextCore();
        var message = definition.ProvideMessage(in messageContext);
        cache.Store(
            key,
            new CachedValidationErrorMessage(message, normalizedCheck.GetResolvedAbsoluteTarget())
        );
        var defTarget = ResolveDefinitionTarget(normalizedCheck, definition.Target, target);
        normalizedCheck.Context.AddError(
            message.ToError(
                code ?? definition.Code,
                defTarget,
                category ?? definition.Category,
                metadata ?? definition.Metadata
            )
        );
        return normalizedCheck;
    }

    /// <summary>
    /// Adds a validation error with the specified message and optional details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="metadata">The optional metadata.</param>
    /// <param name="target">
    /// The optional explicit target. Relative targets are composed with the current validation scope, absolute targets
    /// are used unchanged, and caller-expression targets are normalized before composition.
    /// </param>
    /// <param name="category">The error category.</param>
    /// <param name="respectShortCircuit">
    /// When <see langword="true" />, the error is skipped for short-circuited checks. The default is <see langword="true" />.
    /// </param>
    /// <returns>The current check.</returns>
    public Check<T> AddError(
        string message,
        string? code = null,
        MetadataObject? metadata = null,
        ValidationTarget? target = null,
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
            normalizedCheck.GetResolvedAbsoluteTarget() :
            normalizedCheck.Context.ResolveTarget(target.Value);
        normalizedCheck.Context.AddError(
            new ValidationErrorMessage(message).ToError(code, resolvedTarget, category, metadata)
        );
        return normalizedCheck;
    }

    /// <summary>
    /// Implicitly converts the check to its value.
    /// </summary>
    /// <param name="check">The check to convert.</param>
    public static implicit operator T(Check<T> check) => check.Value;

    internal ValidationTarget GetResolvedAbsoluteTargetDescriptor() =>
        ValidationTarget.Absolute(GetResolvedAbsoluteTarget(), isNormalized: true);

    private ValidationErrorMessageContext<T> CreateMessageContextCore() =>
        Context.CreateAbsoluteMessageContext(
            Value,
            _resolvedAbsoluteTarget!,
            DisplayName ?? _resolvedAbsoluteTarget ?? TargetDescriptor.Input
        );

    private static string ResolveDefinitionTarget(
        Check<T> normalizedCheck,
        ValidationTarget? definitionTarget,
        ValidationTarget? overrideTarget
    )
    {
        if (overrideTarget is not null)
        {
            return normalizedCheck.Context.ResolveTarget(overrideTarget.Value);
        }

        return definitionTarget is not null ?
            normalizedCheck.Context.ResolveTarget(definitionTarget.Value) :
            normalizedCheck.GetResolvedAbsoluteTarget();
    }

    private string ResolveOverrideOrDefinitionTarget(
        string cachedResolvedTarget,
        ValidationTarget? definitionTarget,
        ValidationTarget? overrideTarget
    )
    {
        if (overrideTarget is not null)
        {
            return Context.ResolveTarget(overrideTarget.Value);
        }

        return definitionTarget is not null ?
            Context.ResolveTarget(definitionTarget.Value) :
            cachedResolvedTarget;
    }

    private string GetResolvedAbsoluteTarget() => _resolvedAbsoluteTarget ?? Context.ResolveTarget(TargetDescriptor);
}
