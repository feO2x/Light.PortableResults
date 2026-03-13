using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Light.PortableResults.Metadata;

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
    public ValidationContextOptions Options => State.Options;

    /// <summary>
    /// Gets the validation error templates used by this context.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates => State.ErrorTemplates;

    /// <summary>
    /// Gets the target prefix that is prepended to errors created within this context.
    /// </summary>
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

    private ValidationState State =>
        _state ?? throw new InvalidOperationException("The validation context must not be the default instance");

    /// <summary>
    /// Creates a readonly view for the current validation run and scope.
    /// </summary>
    public ReadOnlyValidationContext AsReadOnly()
    {
        EnsureInitialized();
        return new ReadOnlyValidationContext(State, _targetPrefix);
    }

    /// <summary>
    /// Stores a shared validation item for the current run.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The value to store.</param>
    public void SetItem<T>(ValidationContextKey<T> key, T value)
    {
        EnsureInitialized();
        State.SetItem(key, value);
    }

    /// <summary>
    /// Tries to read a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The stored value when present.</param>
    /// <returns><see langword="true" /> when the item exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetItem<T>(ValidationContextKey<T> key, out T value)
    {
        EnsureInitialized();
        return State.TryGetItem(key, out value);
    }

    /// <summary>
    /// Gets a shared validation item or throws when it is missing.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the item does not exist.</exception>
    public T GetRequiredItem<T>(ValidationContextKey<T> key)
    {
        EnsureInitialized();
        return State.GetRequiredItem(key);
    }

    /// <summary>
    /// Removes a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns><see langword="true" /> when the item existed; otherwise, <see langword="false" />.</returns>
    public bool RemoveItem<T>(ValidationContextKey<T> key)
    {
        EnsureInitialized();
        return State.RemoveItem(key);
    }

    /// <summary>
    /// Creates a scoped validation context for the specified child value.
    /// </summary>
    /// <typeparam name="T">The type of the child value.</typeparam>
    /// <param name="child">The child value whose caller expression identifies the scope prefix.</param>
    /// <param name="target">The raw caller expression for the child value.</param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext For<T>(T child, [CallerArgumentExpression("child")] string target = "") =>
        WithPrefix(target);

    /// <summary>
    /// Creates a scoped validation context for the specified member path segment.
    /// </summary>
    /// <param name="memberName">The member name or path segment to append.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="memberName" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext ForMember(string memberName, bool isNormalized = false) =>
        WithPrefix(memberName, isNormalized);

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
    /// Creates a scoped validation context with the specified target prefix.
    /// </summary>
    /// <param name="prefix">The prefix to append to this scope.</param>
    /// <param name="isNormalized">
    /// <see langword="true" /> when <paramref name="prefix" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The scoped validation context.</returns>
    public ValidationContext WithPrefix(string prefix, bool isNormalized = false)
    {
        EnsureInitialized();
        if (prefix is null)
        {
            throw new ArgumentNullException(nameof(prefix));
        }

        if (prefix.Length == 0)
        {
            return this;
        }

        var normalizedPrefix = isNormalized ? prefix : NormalizeTarget(prefix);
        var composedPrefix = ValidationTargets.Compose(_targetPrefix, normalizedPrefix);
        return new ValidationContext(State, composedPrefix);
    }

    /// <summary>
    /// Creates a check for the specified value and raw caller expression target.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="stringValueNormalizer">
    /// Overrides the context-wide string normalization behavior for this specific check when set.
    /// </param>
    /// <param name="target">The raw target expression.</param>
    /// <param name="displayName">The optional display name.</param>
    /// <returns>The created check.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target" /> is null.</exception>
    public Check<T> Check<T>(
        T value,
        IStringValueNormalizer? stringValueNormalizer = null,
        [CallerArgumentExpression("value")] string? target = null,
        string? displayName = null
    )
    {
        EnsureInitialized();
        // ReSharper disable once JoinNullCheckWithUsage -- false positive, for display name, we use the ??= operator.
        // This would mean that the null check for target is only executed when displayName is null.
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        displayName ??= target;
        value = NormalizeValueIfNecessary(value, stringValueNormalizer ?? Options.StringValueNormalizer);
        return new Check<T>(this, target, displayName, value, isTargetNormalized: false, isShortCircuited: false);
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
    /// <param name="target">The optional target path.</param>
    /// <param name="metadata">The optional error metadata.</param>
    public void AddError(
        ValidationErrorMessage message,
        string? code = null,
        string? target = null,
        MetadataObject? metadata = null
    )
    {
        EnsureInitialized();
        var resolvedTarget = target is null ? _targetPrefix : ComposeTarget(target, isTargetNormalized: true);
        AddError(message.ToError(code, resolvedTarget, ErrorCategory.Validation, metadata));
    }

    /// <summary>
    /// Creates and adds a validation error with the specified message and optional details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="code">The optional error code.</param>
    /// <param name="target">The optional target path.</param>
    /// <param name="metadata">The optional error metadata.</param>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message" /> is null.</exception>
    public void AddError(string message, string? code = null, string? target = null, MetadataObject? metadata = null)
    {
        EnsureInitialized();
        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        AddError(new ValidationErrorMessage(message), code, target, metadata);
    }

    /// <summary>
    /// Normalizes a raw target expression using the configured target normalizer.
    /// </summary>
    /// <param name="rawTarget">The raw target expression.</param>
    /// <returns>The normalized target.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    public string NormalizeTarget(string rawTarget)
    {
        EnsureInitialized();
        return Options.TargetNormalizer.Normalize(rawTarget);
    }

    /// <summary>
    /// Normalizes a string value using the configured string normalization behavior.
    /// </summary>
    /// <param name="value">The string value to normalize.</param>
    /// <returns>The normalized string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    public string? NormalizeStringValue(string? value)
    {
        EnsureInitialized();
        return Options.StringValueNormalizer.Normalize(value);
    }

    /// <summary>
    /// Tries to create the automatic null-validation error for the specified target and display name.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="value">The validated value.</param>
    /// <param name="target">The normalized validation target.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="error">The created error when one should be produced.</param>
    /// <returns><see langword="true" /> when an error was created; otherwise, <see langword="false" />.</returns>
    public bool TryCreateAutomaticNullError<T>(T value, string target, string displayName, out Error error)
    {
        EnsureInitialized();
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (displayName is null)
        {
            throw new ArgumentNullException(nameof(displayName));
        }

        var messageContext = CreateAbsoluteMessageContext(value, target, displayName);
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
        EnsureInitialized();
        return State.TryBuildErrors(out errors);
    }

    /// <summary>
    /// Materializes the accumulated errors.
    /// </summary>
    /// <returns>The accumulated errors or the empty <see cref="Errors" /> value on success.</returns>
    public Errors ToErrors() => TryGetErrors(out var errors) ? errors : default;

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
    /// Gets the target path for the automatic null check.
    /// </summary>
    /// <param name="rawTarget">The non-normalized target </param>
    /// <returns>The normalized flat target.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    public string GetAutomaticNullTarget(string rawTarget)
    {
        EnsureInitialized();
        if (_targetPrefix.Length > 0)
        {
            if (ValidationTargets.IsSimpleIdentifier(rawTarget))
            {
                return _targetPrefix;
            }

            var normalizedTarget = NormalizeTarget(rawTarget);
            if (string.Equals(normalizedTarget, _targetPrefix, StringComparison.Ordinal))
            {
                return _targetPrefix;
            }
        }

        if (_targetPrefix.Length == 0 && ValidationTargets.IsSimpleIdentifier(rawTarget))
        {
            return string.Empty;
        }

        return ComposeTarget(rawTarget, isTargetNormalized: false);
    }

    /// <summary>
    /// Composes a target path by combining the context's target prefix with the specified target.
    /// </summary>
    /// <param name="target">The target to compose with the prefix.</param>
    /// <param name="isTargetNormalized">
    /// <see langword="true" /> when <paramref name="target" /> is already normalized; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>The composed target path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this context is the default instance.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target" /> is null.</exception>
    public string ComposeTarget(string target, bool isTargetNormalized)
    {
        EnsureInitialized();
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var normalizedTarget = isTargetNormalized ? target : NormalizeTarget(target);
        return ValidationTargets.Compose(_targetPrefix, normalizedTarget);
    }

    /// <summary>
    /// Ensures that the validation context is initialized and not the default instance.
    /// </summary>
    public void ThrowIfDefault() => EnsureInitialized();

    internal ValidationErrorMessageContext<T> CreateAbsoluteMessageContext<T>(
        T value,
        string target,
        string displayName
    )
    {
        EnsureInitialized();
        return new ValidationErrorMessageContext<T>(AsReadOnly(), displayName, target, value);
    }

    private static T NormalizeValueIfNecessary<T>(T value, IStringValueNormalizer normalizer)
    {
        // We have two different if blocks for strings here. The first one uses typeof(T) == typeof(string) so that
        // the JIT will eliminate the first branch at compile time via generic specialization,
        // making the second branch effectively dead code for T == string. However, T could be resolved to object and
        // this is what the second if block handles.
        if (typeof(T) == typeof(string))
        {
            var normalizedString = normalizer.Normalize((string?) (object?) value);
            return (T) (object?) normalizedString!;
        }

        if (value is string stringValue)
        {
            return (T) (object?) normalizer.Normalize(stringValue)!;
        }

        return value;
    }

    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    private void EnsureInitialized()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The validation context must not be the default instance");
        }

#pragma warning disable CS8774
    }
#pragma warning restore CS8774
}
