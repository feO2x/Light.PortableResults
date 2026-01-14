using System;
using System.Threading.Tasks;

namespace Light.Results.FunctionalExtensions;

/// <summary>
/// Provides Switch extension methods for result types.
/// </summary>
public static class SwitchExtensions
{
    /// <summary>
    /// Executes the appropriate action based on the result state.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The action to execute on success.</param>
    /// <param name="onError">The action to execute on failure.</param>
    public static void Switch<T>(
        this Result<T> result,
        Action<T> onSuccess,
        Action<Errors> onError
    )
    {
        if (result.IsValid)
        {
            onSuccess(result.Value);
        }
        else
        {
            onError(result.Errors);
        }
    }

    /// <summary>
    /// Executes the appropriate action based on the result state (for void results).
    /// </summary>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The action to execute on success.</param>
    /// <param name="onError">The action to execute on failure.</param>
    public static void Switch(
        this Result result,
        Action onSuccess,
        Action<Errors> onError
    )
    {
        if (result.IsValid)
        {
            onSuccess();
        }
        else
        {
            onError(result.Errors);
        }
    }

    /// <summary>
    /// Executes the appropriate async action based on the result state.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The async action to execute on success.</param>
    /// <param name="onError">The async action to execute on failure.</param>
    /// <returns>A task representing the async operation.</returns>
    public static ValueTask SwitchAsync<T>(
        this Result<T> result,
        Func<T, ValueTask> onSuccess,
        Func<Errors, ValueTask> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.Errors);

    /// <summary>
    /// Executes the appropriate async action based on the result state (for void results).
    /// </summary>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The async action to execute on success.</param>
    /// <param name="onError">The async action to execute on failure.</param>
    /// <returns>A task representing the async operation.</returns>
    public static ValueTask SwitchAsync(
        this Result result,
        Func<ValueTask> onSuccess,
        Func<Errors, ValueTask> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.Errors);
}
