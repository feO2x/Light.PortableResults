using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides Map extension methods for result types.
/// </summary>
public static class MapExtensions
{
    /// <summary>
    /// Maps the value of this result to a new result using the specified function.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="map">The function to map the value.</param>
    /// <returns>A new result with the mapped value if valid; otherwise, a failed result with the same errors.</returns>
    public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> map) =>
        result.IsValid ?
            Result<TOut>.Ok(map(result.Value), result.Metadata) :
            Result<TOut>.Fail(result.Errors, result.Metadata);

    /// <summary>
    /// Maps the value of this result to a new result using the specified async function.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="map">The async function to map the value.</param>
    /// <returns>A task containing a new result with the mapped value if valid; otherwise, a failed result with the same errors.</returns>
    public static async ValueTask<Result<TOut>> MapAsync<T, TOut>(
        this Result<T> result,
        Func<T, ValueTask<TOut>> map
    ) =>
        result.IsValid ?
            Result<TOut>.Ok(await map(result.Value).ConfigureAwait(false), result.Metadata) :
            Result<TOut>.Fail(result.Errors, result.Metadata);
}
