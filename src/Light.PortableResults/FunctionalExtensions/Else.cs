using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides Else extension methods for result types.
/// </summary>
public static class ElseExtensions
{
    /// <summary>
    /// Provides a fallback value if this result is invalid.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide a fallback for.</param>
    /// <param name="fallback">The fallback value to use on failure.</param>
    /// <returns>The original value if valid; otherwise, the fallback value.</returns>
    public static T Else<T>(this Result<T> result, T fallback) =>
        result.IsValid ? result.Value : fallback;

    /// <summary>
    /// Provides a fallback value using a factory function if this result is invalid.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide a fallback for.</param>
    /// <param name="fallbackFactory">The function to create the fallback value on failure.</param>
    /// <returns>The original value if valid; otherwise, the result of the fallback factory.</returns>
    public static T Else<T>(this Result<T> result, Func<Errors, T> fallbackFactory) =>
        result.IsValid ? result.Value : fallbackFactory(result.Errors);

    /// <summary>
    /// Provides a fallback value using an async factory function if this result is invalid.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide a fallback for.</param>
    /// <param name="fallbackFactory">The async function to create the fallback value on failure.</param>
    /// <returns>A task containing the original value if valid; otherwise, the result of the fallback factory.</returns>
    public static ValueTask<T> ElseAsync<T>(
        this Result<T> result,
        Func<Errors, ValueTask<T>> fallbackFactory
    ) =>
        result.IsValid ? new ValueTask<T>(result.Value) : fallbackFactory(result.Errors);
}
