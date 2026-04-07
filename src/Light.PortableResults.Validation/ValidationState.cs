using System;
using System.Collections.Generic;
using Light.PortableResults.Validation.Caching;
using Light.PortableResults.Validation.Messaging;

namespace Light.PortableResults.Validation;

/// <summary>
/// Represents the mutable state of a validation operation, tracking validation errors,
/// shared run-level items, and configuration options.
/// </summary>
public sealed class ValidationState
{
    private const int InitialErrorCapacity = 10;

    private Error[]? _errors;
    private Error _firstError;
    private Dictionary<object, object?>? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationState" /> class.
    /// </summary>
    /// <param name="options">The validation context options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is null.</exception>
    public ValidationState(ValidationContextOptions options) =>
        Options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets the validation context options that control validation behavior.
    /// </summary>
    public ValidationContextOptions Options { get; }

    /// <summary>
    /// Gets the validation error templates used for generating validation errors.
    /// </summary>
    public ValidationErrorTemplates ErrorTemplates => Options.ErrorTemplates;

    /// <summary>
    /// Gets the shared cache for reusable validation error definitions.
    /// </summary>
    public IValidationErrorDefinitionCache ErrorDefinitionCache => Options.ErrorDefinitionCache;

    /// <summary>
    /// Gets the total number of validation errors that have been added.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether any validation errors have been added.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>
    /// Gets the accumulated validation errors. If there are no errors, an empty
    /// <see cref="Light.PortableResults.Errors" /> instance is returned.
    /// </summary>
    public Errors Errors =>
        ErrorCount switch
        {
            0 => default,
            1 => new Errors(_firstError),
            _ => new Errors(_errors!.AsMemory(0, ErrorCount))
        };

    /// <summary>
    /// Gets the object that tracks how many errors are currently present in the validation state.
    /// </summary>
    /// <returns>The object that tracks how many errors are currently present in the validation state.</returns>
    public ValidationCheckpoint CreateCheckpoint() => new (this, ErrorCount);

    /// <summary>
    /// Adds or replaces a shared validation item for the current run.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <param name="value">The value to store.</param>
    public void SetItem<T>(ValidationContextKey<T> key, T value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        (_items ??= new Dictionary<object, object?>())[key] = value;
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
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_items is not null && _items.TryGetValue(key, out var boxedValue))
        {
            if (boxedValue is null)
            {
                value = default!;
                return true;
            }

            if (boxedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }
        }

        value = default!;
        return false;
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
        if (TryGetItem(key, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"The required validation context item '{key}' was not found.");
    }

    /// <summary>
    /// Removes a shared validation item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="key">The typed key.</param>
    /// <returns><see langword="true" /> when the item existed; otherwise, <see langword="false" />.</returns>
    public bool RemoveItem<T>(ValidationContextKey<T> key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _items?.Remove(key) ?? false;
    }

    /// <summary>
    /// Adds a validation error to the state. The first error is stored inline,
    /// and subsequent errors trigger array allocation with automatic capacity growth.
    /// </summary>
    /// <param name="error">The error to add.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="error" /> is the default instance.</exception>
    public void AddError(Error error)
    {
        if (error.IsDefaultInstance)
        {
            throw new ArgumentException("The error must not be the default instance.", nameof(error));
        }

        switch (ErrorCount)
        {
            case 0:
                _firstError = error;
                ErrorCount = 1;
                return;
            case 1:
                var errors = new Error[InitialErrorCapacity];
                errors[0] = _firstError;
                errors[1] = error;
                _errors = errors;
                ErrorCount = 2;
                return;
            default:
                EnsureCapacity(ErrorCount + 1);
                _errors![ErrorCount] = error;
                ErrorCount++;
                return;
        }
    }

    /// <summary>
    /// Attempts to retrieve errors that were added since a specified starting error count.
    /// </summary>
    /// <param name="startingErrorCount">
    /// The starting error count to calculate the range of new errors from. Must be between zero and the current error
    /// count.
    /// </param>
    /// <param name="errors">
    /// When successful, contains the errors added since <paramref name="startingErrorCount" />. Contains the default value if no new
    /// errors were found.
    /// </param>
    /// <returns>
    /// <c>true</c> if there are new errors since <paramref name="startingErrorCount" />; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="startingErrorCount" /> is less than zero or greater than the current error
    /// count.
    /// </exception>
    public bool TryGetErrorsSince(int startingErrorCount, out Errors errors)
    {
        if (startingErrorCount < 0 || startingErrorCount > ErrorCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startingErrorCount),
                "The starting error count must be between zero and the current error count."
            );
        }

        var newErrorCount = ErrorCount - startingErrorCount;
        switch (newErrorCount)
        {
            case 0:
                errors = default;
                return false;
            case 1:
                errors = new Errors(GetErrorAt(startingErrorCount));
                return true;
            default:
                // Once a second error exists, the inline first error is copied to _errors[0], so the append-only
                // logical error count and the zero-based backing-array index stay aligned. A checkpoint created after
                // N existing errors therefore starts its slice at index N; startingErrorCount == 0 correctly means
                // that all currently stored array entries are new.
                errors = startingErrorCount == 0 ?
                    new Errors(_errors!.AsMemory(0, ErrorCount)) :
                    new Errors(_errors!.AsMemory(startingErrorCount, newErrorCount));
                return true;
        }
    }

    private Error GetErrorAt(int index) =>
        index switch
        {
            0 when ErrorCount == 1 => _firstError,
            _ => _errors![index]
        };

    private void EnsureCapacity(int requiredCount)
    {
        if (_errors is not null && _errors.Length >= requiredCount)
        {
            return;
        }

        var currentCapacity = _errors?.Length ?? 0;
        var newCapacity = currentCapacity == 0 ?
            InitialErrorCapacity :
            Math.Max(currentCapacity * 2, requiredCount);
        var newErrors = new Error[newCapacity];
        if (_errors is not null)
        {
            Array.Copy(_errors, newErrors, ErrorCount);
        }
        else if (ErrorCount > 0)
        {
            newErrors[0] = _firstError;
        }

        _errors = newErrors;
    }
}
