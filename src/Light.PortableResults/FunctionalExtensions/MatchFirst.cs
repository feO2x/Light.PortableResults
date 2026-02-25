using System;
using System.Threading.Tasks;

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides MatchFirst extension methods for result types.
/// </summary>
public static class MatchFirstExtensions
{
    /// <summary>
    /// Matches the result to a value using the appropriate handler.
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onError">The function to execute on failure (receives first error only).</param>
    /// <returns>The result of the appropriate handler.</returns>
    public static TOut MatchFirst<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<Error, TOut> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.FirstError);

    /// <summary>
    /// Matches the result to a value using the appropriate handler (for void results).
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onError">The function to execute on failure (receives first error only).</param>
    /// <returns>The result of the appropriate handler.</returns>
    public static TOut MatchFirst<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Error, TOut> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.FirstError);

    /// <summary>
    /// Matches the result to a value using the appropriate async handler.
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The async function to execute on success.</param>
    /// <param name="onError">The async function to execute on failure (receives first error only).</param>
    /// <returns>A task containing the result of the appropriate handler.</returns>
    public static ValueTask<TOut> MatchFirstAsync<T, TOut>(
        this Result<T> result,
        Func<T, ValueTask<TOut>> onSuccess,
        Func<Error, ValueTask<TOut>> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.FirstError);

    /// <summary>
    /// Matches the result to a value using the appropriate async handler (for void results).
    /// The error handler receives only the first error.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The async function to execute on success.</param>
    /// <param name="onError">The async function to execute on failure (receives first error only).</param>
    /// <returns>A task containing the result of the appropriate handler.</returns>
    public static ValueTask<TOut> MatchFirstAsync<TOut>(
        this Result result,
        Func<ValueTask<TOut>> onSuccess,
        Func<Error, ValueTask<TOut>> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.FirstError);
}
