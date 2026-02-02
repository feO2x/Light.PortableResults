using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.Results.FunctionalExtensions;

/// <summary>
/// Provides TapError extension methods for result types.
/// </summary>
public static class TapErrorExtensions
{
    /// <summary>
    /// Executes the specified action on the errors if this result is invalid.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="result">The result to tap.</param>
    /// <param name="action">The action to execute on failure.</param>
    /// <returns>The original result.</returns>
    public static TResult TapError<TResult>(this TResult result, Action<Errors> action)
        where TResult : struct, IResultObject
    {
        if (!result.IsValid)
        {
            action(result.Errors);
        }

        return result;
    }

    /// <summary>
    /// Executes the specified async action on the errors if this result is invalid.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="result">The result to tap.</param>
    /// <param name="action">The async action to execute on failure.</param>
    /// <returns>A task containing the original result.</returns>
    public static async ValueTask<TResult> TapErrorAsync<TResult>(
        this TResult result,
        Func<Errors, ValueTask> action
    )
        where TResult : struct, IResultObject
    {
        if (!result.IsValid)
        {
            await action(result.Errors).ConfigureAwait(false);
        }

        return result;
    }
}
