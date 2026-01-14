using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToExtensionBlock

namespace Light.Results.FunctionalExtensions;

/// <summary>
/// Provides Ensure extension methods for result types.
/// </summary>
public static class EnsureExtensions
{
    /// <summary>
    /// Converts this result to a failure if the predicate returns false for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate on the value.</param>
    /// <param name="error">The error to use if the predicate returns false.</param>
    /// <returns>A failed result if the predicate returns false; otherwise, the original result.</returns>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate(result.Value) ? result : Result<T>.Fail(error, result.Metadata);
    }

    /// <summary>
    /// Converts this result to a failure if the predicate returns false for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate on the value.</param>
    /// <param name="errorFactory">The function to create the error if the predicate returns false.</param>
    /// <returns>A failed result if the predicate returns false; otherwise, the original result.</returns>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Func<T, Error> errorFactory
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate(result.Value) ? result : Result<T>.Fail(errorFactory(result.Value), result.Metadata);
    }

    /// <summary>
    /// Converts this result to a failure if the predicate returns false.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="error">The error to use if the predicate returns false.</param>
    /// <returns>A failed result if the predicate returns false; otherwise, the original result.</returns>
    public static Result Ensure(
        this Result result,
        Func<bool> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return predicate() ? result : Result.Fail(error, result.Metadata);
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns false for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate on the value.</param>
    /// <param name="error">The error to use if the predicate returns false.</param>
    /// <returns>A task containing a failed result if the predicate returns false; otherwise, the original result.</returns>
    public static async ValueTask<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, ValueTask<bool>> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return await predicate(result.Value).ConfigureAwait(false) ? result : Result<T>.Fail(error, result.Metadata);
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns false for the value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate on the value.</param>
    /// <param name="errorFactory">The function to create the error if the predicate returns false.</param>
    /// <returns>A task containing a failed result if the predicate returns false; otherwise, the original result.</returns>
    public static async ValueTask<Result<T>> EnsureAsync<T>(
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
            result :
            Result<T>.Fail(errorFactory(result.Value), result.Metadata);
    }

    /// <summary>
    /// Converts this result to a failure if the async predicate returns false.
    /// </summary>
    /// <param name="result">The result to check.</param>
    /// <param name="predicate">The async predicate to evaluate.</param>
    /// <param name="error">The error to use if the predicate returns false.</param>
    /// <returns>A task containing a failed result if the predicate returns false; otherwise, the original result.</returns>
    public static async ValueTask<Result> EnsureAsync(
        this Result result,
        Func<ValueTask<bool>> predicate,
        Error error
    )
    {
        if (!result.IsValid)
        {
            return result;
        }

        return await predicate().ConfigureAwait(false) ? result : Result.Fail(error, result.Metadata);
    }
}
