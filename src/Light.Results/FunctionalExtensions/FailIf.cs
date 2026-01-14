using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.Results.FunctionalExtensions;

/// <summary>
/// Provides FailIf extension methods for result types.
/// </summary>
public static class FailIfExtensions
{
    /// <summary>
    /// Converts this result to a failure if the predicate returns true for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate on the value.</param>
    /// <param name="error">The error to use if the predicate returns true.</param>
    /// <returns>A failed result if the predicate returns true; otherwise, the original result.</returns>
    public static Result<T> FailIf<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate(result.Value) ? Result<T>.Fail(error, result.Metadata) : result;
    }

    /// <summary>
    /// Converts this result to a failure if the predicate returns true for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate on the value.</param>
    /// <param name="errorFactory">The function to create the error if the predicate returns true.</param>
    /// <returns>A failed result if the predicate returns true; otherwise, the original result.</returns>
    public static Result<T> FailIf<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Func<T, Error> errorFactory
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate(result.Value) ? Result<T>.Fail(errorFactory(result.Value), result.Metadata) : result;
    }

    /// <summary>
    /// Converts this result to a failure if the predicate returns true.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="error">The error to use if the predicate returns true.</param>
    /// <returns>A failed result if the predicate returns true; otherwise, the original result.</returns>
    public static Result FailIf(
        this Result result,
        Func<bool> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate() ? Result.Fail(error, result.Metadata) : result;
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns true for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate on the value.</param>
    /// <param name="error">The error to use if the predicate returns true.</param>
    /// <returns>A task containing a failed result if the predicate returns true; otherwise, the original result.</returns>
    public static async ValueTask<Result<T>> FailIfAsync<T>(
        this Result<T> result,
        Func<T, ValueTask<bool>> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return await predicate(result.Value).ConfigureAwait(false) ? Result<T>.Fail(error, result.Metadata) : result;
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns true for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate on the value.</param>
    /// <param name="errorFactory">The function to create the error if the predicate returns true.</param>
    /// <returns>A task containing a failed result if the predicate returns true; otherwise, the original result.</returns>
    public static async ValueTask<Result<T>> FailIfAsync<T>(
        this Result<T> result,
        Func<T, ValueTask<bool>> predicate,
        Func<T, Error> errorFactory
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return await predicate(result.Value).ConfigureAwait(false) ?
            Result<T>.Fail(errorFactory(result.Value), result.Metadata) :
            result;
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns true.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate.</param>
    /// <param name="error">The error to use if the predicate returns true.</param>
    /// <returns>A task containing a failed result if the predicate returns true; otherwise, the original result.</returns>
    public static async ValueTask<Result> FailIfAsync(
        this Result result,
        Func<ValueTask<bool>> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return await predicate().ConfigureAwait(false) ? Result.Fail(error, result.Metadata) : result;
    }
}
