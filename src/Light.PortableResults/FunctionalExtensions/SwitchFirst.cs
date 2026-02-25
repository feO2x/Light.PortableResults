using System;
using System.Threading.Tasks;

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides SwitchFirst extension methods for result types.
/// </summary>
public static class SwitchFirstExtensions
{
    /// <summary>
    /// Executes the appropriate action based on the result state.
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The action to execute on success.</param>
    /// <param name="onError">The action to execute on failure (receives first error only).</param>
    public static void SwitchFirst<T>(
        this Result<T> result,
        Action<T> onSuccess,
        Action<Error> onError
    )
    {
        if (result.IsValid)
        {
            onSuccess(result.Value);
        }
        else
        {
            onError(result.FirstError);
        }
    }

    /// <summary>
    /// Executes the appropriate action based on the result state (for void results).
    /// The error handler receives only the first error.
    /// </summary>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The action to execute on success.</param>
    /// <param name="onError">The action to execute on failure (receives first error only).</param>
    public static void SwitchFirst(
        this Result result,
        Action onSuccess,
        Action<Error> onError
    )
    {
        if (result.IsValid)
        {
            onSuccess();
        }
        else
        {
            onError(result.FirstError);
        }
    }

    /// <summary>
    /// Executes the appropriate async action based on the result state.
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The async action to execute on success.</param>
    /// <param name="onError">The async action to execute on failure (receives first error only).</param>
    /// <returns>A task representing the async operation.</returns>
    public static ValueTask SwitchFirstAsync<T>(
        this Result<T> result,
        Func<T, ValueTask> onSuccess,
        Func<Error, ValueTask> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.FirstError);

    /// <summary>
    /// Executes the appropriate async action based on the result state (for void results).
    /// The error handler receives only the first error.
    /// </summary>
    /// <param name="result">The result to switch on.</param>
    /// <param name="onSuccess">The async action to execute on success.</param>
    /// <param name="onError">The async action to execute on failure (receives first error only).</param>
    /// <returns>A task representing the async operation.</returns>
    public static ValueTask SwitchFirstAsync(
        this Result result,
        Func<ValueTask> onSuccess,
        Func<Error, ValueTask> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.FirstError);
}
