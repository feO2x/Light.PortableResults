using System;
using System.Threading.Tasks;

namespace Light.Results.FunctionalExtensions;

/// <summary>
/// Provides MapError extension methods for result types.
/// </summary>
public static class MapErrorExtensions
{
    /// <summary>
    /// Transforms each error in this result using the specified function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to transform errors for.</param>
    /// <param name="mapError">The function to transform each error.</param>
    /// <returns>A new result with transformed errors if invalid; otherwise, the original result.</returns>
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> mapError)
    {
        if (result.IsValid)
        {
            return result;
        }

        var errors = result.Errors;
        var mappedErrors = new Error[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            mappedErrors[i] = mapError(errors[i]);
        }

        return Result<T>.Fail(mappedErrors, result.Metadata);
    }

    /// <summary>
    /// Transforms each error in this result using the specified function.
    /// </summary>
    /// <param name="result">The result to transform errors for.</param>
    /// <param name="mapError">The function to transform each error.</param>
    /// <returns>A new result with transformed errors if invalid; otherwise, the original result.</returns>
    public static Result MapError(this Result result, Func<Error, Error> mapError)
    {
        if (result.IsValid)
        {
            return result;
        }

        var errors = result.Errors;
        var mappedErrors = new Error[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            mappedErrors[i] = mapError(errors[i]);
        }

        return Result.Fail(mappedErrors, result.Metadata);
    }

    /// <summary>
    /// Transforms each error in this result using the specified async function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to transform errors for.</param>
    /// <param name="mapError">The async function to transform each error.</param>
    /// <returns>A task containing a new result with transformed errors if invalid; otherwise, the original result.</returns>
    public static async ValueTask<Result<T>> MapErrorAsync<T>(
        this Result<T> result,
        Func<Error, ValueTask<Error>> mapError
    )
    {
        if (result.IsValid)
        {
            return result;
        }

        var errors = result.Errors;
        var mappedErrors = new Error[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            mappedErrors[i] = await mapError(errors[i]).ConfigureAwait(false);
        }

        return Result<T>.Fail(mappedErrors, result.Metadata);
    }

    /// <summary>
    /// Transforms each error in this result using the specified async function.
    /// </summary>
    /// <param name="result">The result to transform errors for.</param>
    /// <param name="mapError">The async function to transform each error.</param>
    /// <returns>A task containing a new result with transformed errors if invalid; otherwise, the original result.</returns>
    public static async ValueTask<Result> MapErrorAsync(
        this Result result,
        Func<Error, ValueTask<Error>> mapError
    )
    {
        if (result.IsValid)
        {
            return result;
        }

        var errors = result.Errors;
        var mappedErrors = new Error[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            mappedErrors[i] = await mapError(errors[i]).ConfigureAwait(false);
        }

        return Result.Fail(mappedErrors, result.Metadata);
    }
}
