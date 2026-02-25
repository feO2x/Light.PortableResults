using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides Tap extension methods for result types.
/// </summary>
public static class TapExtensions
{
    /// <summary>
    /// Executes the specified action on the value if this result is valid.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap.</param>
    /// <param name="action">The action to execute on success.</param>
    /// <returns>The original result.</returns>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsValid)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes the specified async action on the value if this result is valid.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap.</param>
    /// <param name="action">The async action to execute on success.</param>
    /// <returns>A task containing the original result.</returns>
    public static async ValueTask<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, ValueTask> action
    )
    {
        if (result.IsValid)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }
}
