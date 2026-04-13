using System;
using System.Diagnostics.CodeAnalysis;

namespace Light.PortableResults.Validation;

/// <summary>
/// Captures the local error boundary for one validator or helper invocation.
/// </summary>
public readonly struct ValidationCheckpoint
{
    private readonly ValidationState? _state;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationCheckpoint" />.
    /// </summary>
    /// <param name="state">The validation state at the time of checkpoint creation.</param>
    /// <param name="startingErrorCount">The number of errors present in the validation context at the time of checkpoint creation.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="state" /> is null.</exception>
    public ValidationCheckpoint(ValidationState state, int startingErrorCount)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        StartingErrorCount = startingErrorCount;
    }

    /// <summary>
    /// Gets the number of errors that were present in the validation context when this checkpoint was created.
    /// </summary>
    public int StartingErrorCount { get; }

    /// <summary>
    /// Gets a value indicating whether new errors were added after this checkpoint was created.
    /// </summary>
    public bool HasNewErrors => NewErrorCount > 0;

    /// <summary>
    /// Gets the number of errors that were added after this checkpoint was created.
    /// </summary>
    public int NewErrorCount => State.ErrorCount - StartingErrorCount;

    /// <summary>
    /// Tries to get the errors that were added after this checkpoint was created.
    /// </summary>
    /// <param name="errors">The newly added errors when present.</param>
    /// <returns><see langword="true" /> when new errors were added; otherwise, <see langword="false" />.</returns>
    public bool TryGetNewErrors(out Errors errors)
    {
        return State.TryGetErrorsSince(StartingErrorCount, out errors);
    }

    /// <summary>
    /// Creates a validated value that succeeds only when no new errors were added since this checkpoint.
    /// </summary>
    /// <typeparam name="T">The validated value type.</typeparam>
    /// <param name="value">The validated value.</param>
    /// <returns>
    /// <see cref="ValidatedValue{T}.NoValue" /> when new errors were added; otherwise,
    /// <see cref="ValidatedValue.Success{T}(T)" />.
    /// </returns>
    public ValidatedValue<T> ToValidatedValue<T>(T value) =>
        HasNewErrors ? ValidatedValue<T>.NoValue : ValidatedValue.Success(value);

    private ValidationState State
    {
        get
        {
            EnsureInitialized();
            return _state;
        }
    }

    [MemberNotNull(nameof(_state))]
    private void EnsureInitialized()
    {
        if (_state is null)
        {
            throw new InvalidOperationException("The validation checkpoint must not be the default instance.");
        }
    }
}
