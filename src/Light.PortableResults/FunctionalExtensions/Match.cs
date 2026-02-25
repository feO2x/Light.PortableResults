using System;
using System.Threading.Tasks;

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides Match extension methods for result types.
/// </summary>
public static class MatchExtensions
{
    /// <summary>
    /// Matches the result to a value using the appropriate handler.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onError">The function to execute on failure.</param>
    /// <returns>The result of the appropriate handler.</returns>
    public static TOut Match<T, TOut>(
        this Result<T> result,
        Func<T, TOut> onSuccess,
        Func<Errors, TOut> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.Errors);

    /// <summary>
    /// Matches the result to a value using the appropriate handler (for void results).
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The function to execute on success.</param>
    /// <param name="onError">The function to execute on failure.</param>
    /// <returns>The result of the appropriate handler.</returns>
    public static TOut Match<TOut>(
        this Result result,
        Func<TOut> onSuccess,
        Func<Errors, TOut> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.Errors);

    /// <summary>
    /// Matches the result to a value using the appropriate async handler.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The async function to execute on success.</param>
    /// <param name="onError">The async function to execute on failure.</param>
    /// <returns>A task containing the result of the appropriate handler.</returns>
    public static ValueTask<TOut> MatchAsync<T, TOut>(
        this Result<T> result,
        Func<T, ValueTask<TOut>> onSuccess,
        Func<Errors, ValueTask<TOut>> onError
    ) =>
        result.IsValid ? onSuccess(result.Value) : onError(result.Errors);

    /// <summary>
    /// Matches the result to a value using the appropriate async handler (for void results).
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The async function to execute on success.</param>
    /// <param name="onError">The async function to execute on failure.</param>
    /// <returns>A task containing the result of the appropriate handler.</returns>
    public static ValueTask<TOut> MatchAsync<TOut>(
        this Result result,
        Func<ValueTask<TOut>> onSuccess,
        Func<Errors, ValueTask<TOut>> onError
    ) =>
        result.IsValid ? onSuccess() : onError(result.Errors);
}
