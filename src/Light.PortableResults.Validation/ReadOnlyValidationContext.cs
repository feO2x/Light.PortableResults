using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides readonly access to validation-run configuration and shared context data.
/// </summary>
public readonly struct ReadOnlyValidationContext
{
    private readonly ValidationState? _state;
    private readonly string? _targetPrefix;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyValidationContext" />.
    /// </summary>
    /// <param name="state">The validation state that tracks accumulated errors.</param>
    /// <param name="targetPrefix">The prefix to prepend to error targets within this context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state" /> or <paramref name="targetPrefix" /> is null.</exception>
    public ReadOnlyValidationContext(ValidationState state, string targetPrefix)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _targetPrefix = targetPrefix ?? throw new ArgumentNullException(nameof(targetPrefix));
    }

    /// <summary>
    /// Gets a value indicating whether this instance is the uninitialized default value.
    /// </summary>
    public bool IsDefault => _state is null;

    /// <summary>
    /// Gets the active target prefix.
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
    /// Gets the validation context options.
    /// </summary>
    public ValidationContextOptions Options => State.Options;

    /// <summary>
    /// Gets the validation error templates.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates => State.ErrorTemplates;

    /// <summary>
    /// Gets the shared cache for reusable validation error definitions.
    /// </summary>
    public IValidationErrorDefinitionCache ErrorDefinitionCache => State.ErrorDefinitionCache;

    private ValidationState State
    {
        get
        {
            EnsureInitialized();
            return _state;
        }
    }

    /// <summary>
    /// Tries to read a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The stored value when present.</param>
    /// <returns><see langword="true" /> when the item exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetItem<T>(ValidationContextKey<T> key, out T value) => State.TryGetItem(key, out value);

    /// <summary>
    /// Gets a shared validation item or throws when it is missing.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns>The stored value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key" /> is <see langword="null" />.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the item does not exist.</exception>
    public T GetRequiredItem<T>(ValidationContextKey<T> key) => State.GetRequiredItem(key);

    [MemberNotNull(nameof(_state), nameof(_targetPrefix))]
    private void EnsureInitialized()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The validation context must not be the default instance");
        }

#pragma warning disable CS8774 // When _state is not null, _target prefix cannot be null, see constructor
    }
#pragma warning restore CS8774
}
