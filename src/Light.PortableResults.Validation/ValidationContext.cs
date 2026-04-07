using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Normalization;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation;

/// <summary>
/// Tracks validation failures for a single validation run and exposes low-overhead helpers to create checks and
/// materialize flat validation errors.
/// </summary>
public readonly struct ValidationContext
{
    private readonly ValidationState? _state;
    private readonly string? _targetPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationContext" /> struct.
    /// </summary>
    /// <param name="state">The validation state that tracks accumulated errors.</param>
    /// <param name="targetPrefix">The prefix to prepend to error targets within this context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state" /> or <paramref name="targetPrefix" /> is null.</exception>
    public ValidationContext(ValidationState state, string targetPrefix)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _targetPrefix = targetPrefix ?? throw new ArgumentNullException(nameof(targetPrefix));
    }

    /// <summary>
    /// Gets a value indicating whether this instance is the uninitialized default value.
    /// </summary>
    public bool IsDefault => _state is null;

    /// <summary>
    /// Gets the options applied by this context.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public ValidationContextOptions Options => State.Options;

    /// <summary>
    /// Gets the validation error templates used by this context.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public ValidationErrorTemplates ErrorTemplates => State.ErrorTemplates;

    /// <summary>
    /// Gets the shared cache for reusable validation error definitions.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public IValidationErrorDefinitionCache ErrorDefinitionCache => State.ErrorDefinitionCache;

    /// <summary>
    /// Gets the accumulated validation errors. If there are no errors, an empty
    /// <see cref="Light.PortableResults.Errors" /> instance is returned.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public Errors Errors => State.Errors;

    /// <summary>
    /// Gets the target prefix that is prepended to errors created within this context.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public string TargetPrefix
    {
        get
        {
            EnsureInitialized();
            return _targetPrefix;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this context has accumulated validation failures.
    /// </summary>
    public bool HasErrors => State.HasErrors;

    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    private ValidationState State
    {
        get
        {
            EnsureInitialized();
            return _state;
        }
    }

    /// <summary>
    /// Creates a readonly view for the current validation run and scope.
    /// </summary>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public ReadOnlyValidationContext AsReadOnly()
    {
        EnsureInitialized();
        return new ReadOnlyValidationContext(_state, _targetPrefix);
    }

    /// <summary>
    /// Stores a shared validation item for the current run.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The value to store.</param>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public void SetItem<T>(ValidationContextKey<T> key, T value) => State.SetItem(key, value);

    /// <summary>
    /// Tries to read a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The stored value when present.</param>
    /// <returns><see langword="true" /> when the item exists; otherwise, <see langword="false" />.</returns>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public bool TryGetItem<T>(ValidationContextKey<T> key, out T value) => State.TryGetItem(key, out value);

    /// <summary>
    /// Gets a shared validation item or throws when it is missing.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the item does not exist.</exception>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public T GetRequiredItem<T>(ValidationContextKey<T> key) => State.GetRequiredItem(key);

    /// <summary>
    /// Removes a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns><see langword="true" /> when the item existed; otherwise, <see langword="false" />.</returns>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public bool RemoveItem<T>(ValidationContextKey<T> key) => State.RemoveItem(key);

    /// <summary>
    /// Creates a checkpoint that can detect errors added after this call.
    /// </summary>
    /// <returns>The created checkpoint.</returns>
    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    public ValidationCheckpoint CreateCheckpoint() => State.CreateCheckpoint();

    /// <summary>
    /// Creates a scoped validation context for the specified child value.
    /// </summary>
    /// <typeparam name="T">The type of the child value.</typeparam>
    /// <param name="child">The child value whose caller expression identifies the scope prefix.</param>
    /// <param name="target">The raw caller expression for the child value.</param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext For<T>(T child, [CallerArgumentExpression("child")] string target = "") =>
        ForCallerExpression(target);

    /// <summary>
    /// Creates a scoped validation context from a caller-expression-style target.
    /// </summary>
    /// <param name="target">The caller-expression-style target.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="target" /> is already normalized for caller-expression semantics.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForCallerExpression(string target, bool isNormalized = false) =>
        ForTarget(ValidationTarget.CallerExpression(target, isNormalized));

    /// <summary>
    /// Creates a scoped validation context for the specified member path segment.
    /// </summary>
    /// <param name="memberName">The member name or path segment to append.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="memberName" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForMember(string memberName, bool isNormalized = false) =>
        ForRelative(memberName, isNormalized);

    /// <summary>
    /// Creates a scoped validation context for a target path relative to the current validation scope.
    /// </summary>
    /// <param name="target">The relative target path.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="target" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForRelative(string target, bool isNormalized = false) =>
        ForTarget(ValidationTarget.Relative(target, isNormalized));

    /// <summary>
    /// Creates a scoped validation context from an absolute target path.
    /// </summary>
    /// <param name="target">The absolute target path.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="target" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForAbsolute(string target, bool isNormalized = false) =>
        ForTarget(ValidationTarget.Absolute(target, isNormalized));

    /// <summary>
    /// Creates a scoped validation context from an explicit validation target descriptor.
    /// </summary>
    /// <param name="target">The target descriptor.</param>
    /// <returns>The scoped validation context.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is the default instance.</exception>
    public ValidationContext ForTarget(ValidationTarget target)
    {
        var resolvedTarget = ResolveTarget(target);
        return string.Equals(resolvedTarget, _targetPrefix, StringComparison.Ordinal) ?
            this :
            new ValidationContext(State, resolvedTarget);
    }

    /// <summary>
    /// Creates a scoped validation context for the specified collection index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForIndex(int index)
    {
        EnsureInitialized();
        return new ValidationContext(State, ValidationTargets.AppendIndex(_targetPrefix, index));
    }

    /// <summary>
    /// Creates a check for the specified value and caller-expression target.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="valueNormalizer">
    /// Overrides the context-wide normalization behavior for this specific check when set.
    /// </param>
    /// <param name="target">The caller-expression-style target.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The created check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target" /> is null.</exception>
    public Check<T> Check<T>(
        T value,
        IValueNormalizer? valueNormalizer = null,
        [CallerArgumentExpression("value")] string? target = null,
        string? displayName = null
    )
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        return Check(
            value,
            ValidationTarget.CallerExpression(target),
            valueNormalizer,
            displayName
        );
    }

    /// <summary>
    /// Creates a check for the specified value and explicit validation target descriptor.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="target">The explicit target descriptor.</param>
    /// <param name="valueNormalizer">
    /// Overrides the context-wide normalization behavior for this specific check when set.
    /// </param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The created check.</returns>
    public Check<T> Check<T>(
        T value,
        ValidationTarget target,
        IValueNormalizer? valueNormalizer = null,
        string? displayName = null
    )
    {
        EnsureInitialized();
        var validatedTarget = EnsureTarget(target, nameof(target));
        value = (valueNormalizer ?? Options.ValueNormalizer).Normalize(value);
        return new Check<T>(
            this,
            validatedTarget,
            displayName,
            value,
            resolvedAbsoluteTarget: null,
            isShortCircuited: false
        );
    }

    /// <summary>
    /// Adds the specified validation error to this context unchanged.
    /// </summary>
    /// <param name="error">
    /// The fully initialized error to add. If a target is desired, it must already be set on the error instance.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    public void AddError(Error error)
    {
        EnsureInitialized();
        if (error.IsDefaultInstance)
        {
            throw new ArgumentException("The error must not be the default instance.", nameof(error));
        }

        State.AddError(error);
    }

    /// <summary>
    /// Creates and adds a validation error from the specified message descriptor.
    /// </summary>
    /// <param name="message">The generated validation error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="target">
    /// The optional validation target. Relative targets are composed with the current scope, absolute targets are used
    /// unchanged, and caller-expression targets are normalized before composition.
    /// </param>
    /// <param name="metadata">The optional error metadata.</param>
    public void AddError(
        ValidationErrorMessage message,
        string? code = null,
        ValidationTarget? target = null,
        MetadataObject? metadata = null
    )
    {
        EnsureInitialized();
        var resolvedTarget = target is null ? _targetPrefix : ResolveTarget(target.Value);
        AddError(message.ToError(code, resolvedTarget, ErrorCategory.Validation, metadata));
    }

    /// <summary>
    /// Creates and adds a validation error with the specified message and optional details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="target">
    /// The optional validation target. Relative targets are composed with the current scope, absolute targets are used
    /// unchanged, and caller-expression targets are normalized before composition.
    /// </param>
    /// <param name="metadata">The optional error metadata.</param>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message" /> is null.</exception>
    public void AddError(
        string message,
        string? code = null,
        ValidationTarget? target = null,
        MetadataObject? metadata = null
    )
    {
        EnsureInitialized();
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        AddError(new ValidationErrorMessage(message), code, target, metadata);
    }

    /// <summary>
    /// Normalizes the specified validation target according to its explicit semantics.
    /// </summary>
    /// <param name="target">The target descriptor.</param>
    /// <returns>The normalized target.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is the default instance.</exception>
    public string NormalizeTarget(ValidationTarget target)
    {
        var validatedTarget = EnsureTarget(target, nameof(target));
        return validatedTarget.IsNormalized ?
            validatedTarget.Input :
            Options.TargetNormalizer.Normalize(validatedTarget.Input, validatedTarget.Semantics);
    }

    /// <summary>
    /// Resolves the specified validation target to an absolute flat validation path.
    /// </summary>
    /// <param name="target">The target descriptor.</param>
    /// <returns>The resolved absolute target.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is the default instance.</exception>
    public string ResolveTarget(ValidationTarget target)
    {
        var normalizedTarget = NormalizeTarget(target);
        return target.Semantics == ValidationTargetSemantics.Absolute ?
            normalizedTarget :
            ValidationTargets.Compose(TargetPrefix, normalizedTarget);
    }

    /// <summary>
    /// Tries to create the automatic null-validation error for the specified target and display name.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="value">The validated value.</param>
    /// <param name="target">The target that identifies the validated value.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="error">The created error when one should be produced.</param>
    /// <returns><see langword="true" /> when an error was created; otherwise, <see langword="false" />.</returns>
    public bool TryCreateAutomaticNullError<T>(T value, ValidationTarget target, string displayName, out Error error)
    {
        if (displayName is null)
        {
            throw new ArgumentNullException(nameof(displayName));
        }

        var resolvedTarget = GetAutomaticNullTarget(target);
        var messageContext = CreateAbsoluteMessageContext(value, resolvedTarget, displayName);
        return Options.AutomaticNullErrorProvider.TryCreateError(in messageContext, out error);
    }

    /// <summary>
    /// Tries to materialize the accumulated errors.
    /// </summary>
    /// <param name="errors">The accumulated errors when present.</param>
    /// <returns><see langword="true" /> when errors are present; otherwise, <see langword="false" />.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    public bool TryGetErrors(out Errors errors)
    {
        errors = State.Errors;
        return !errors.IsEmpty;
    }

    /// <summary>
    /// Materializes the accumulated errors as a failure <see cref="Result" />.
    /// </summary>
    /// <returns>The failure result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context contains no validation failures.</exception>
    public Result ToFailureResult()
    {
        if (!TryGetErrors(out var errors))
        {
            throw new InvalidOperationException(
                "Cannot create a failure result when no validation errors are present"
            );
        }

        return Result.Fail(errors);
    }

    /// <summary>
    /// Gets the absolute target path to use when the validated value is <see langword="null" />.
    /// </summary>
    /// <param name="target">The target descriptor for the validated value.</param>
    /// <returns>The absolute target path.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target" /> is the default instance.</exception>
    public string GetAutomaticNullTarget(ValidationTarget target)
    {
        EnsureInitialized();
        var validatedTarget = EnsureTarget(target, nameof(target));
        if (validatedTarget.Semantics == ValidationTargetSemantics.CallerExpression &&
            ValidationTargets.IsSimpleIdentifier(NormalizeTarget(validatedTarget))
           )
        {
            return _targetPrefix;
        }

        return ResolveTarget(validatedTarget);
    }

    /// <summary>
    /// Ensures that the validation context is initialized and not the default instance.
    /// </summary>
    public void ThrowIfDefault() => EnsureInitialized();

    internal ValidationErrorMessageContext<T> CreateAbsoluteMessageContext<T>(
        T value,
        string target,
        string displayName
    ) =>
        new (AsReadOnly(), displayName, target, value);

    private static ValidationTarget EnsureTarget(ValidationTarget target, string paramName) =>
        target.IsDefault ?
            throw new ArgumentException("The validation target must not be the default instance.", paramName) :
            target;

    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    private void EnsureInitialized()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The validation context must not be the default instance");
        }

#pragma warning disable CS8774 // When _state is not null, _targetPrefix cannot be null, see constructor
    }
#pragma warning restore CS8774
}
