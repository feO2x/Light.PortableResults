using System;
using System.Threading.Tasks;
using Light.PortableResults.Metadata;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.FunctionalExtensions;

/// <summary>
/// Provides Bind extension methods for result types.
/// </summary>
public static class BindExtensions
{
    /// <summary>
    /// Binds the value of this result to a new result using the specified function.
    /// The metadata from the new result and this instance will be merged.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bind">The function to bind the value to a new result.</param>
    /// <param name="metadataMergeStrategy">The strategy to use when merging metadata.</param>
    /// <returns>The result returned by the bind function if valid; otherwise, a failed result with the same errors.</returns>
    public static Result<TOut> Bind<T, TOut>(
        this Result<T> result,
        Func<T, Result<TOut>> bind,
        MetadataMergeStrategy metadataMergeStrategy = MetadataMergeStrategy.AddOrReplace
    )
    {
        if (!result.IsValid)
        {
            return Result<TOut>.Fail(result.Errors, result.Metadata);
        }

        var newResult = bind(result.Value);
        var mergedMetadata =
            MetadataObjectExtensions.MergeIfNeeded(result.Metadata, newResult.Metadata, metadataMergeStrategy);
        return mergedMetadata is null ? newResult : newResult.ReplaceMetadata(mergedMetadata.Value);
    }

    /// <summary>
    /// Binds the value of this result to a new result using the specified async function.
    /// The metadata from the new result and this instance will be merged.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="bind">The async function to bind the value to a new result.</param>
    /// <param name="metadataMergeStrategy">The strategy to use when merging metadata.</param>
    /// <returns>A task containing the result returned by the bind function if valid; otherwise, a failed result with the same errors.</returns>
    public static async ValueTask<Result<TOut>> BindAsync<T, TOut>(
        this Result<T> result,
        Func<T, ValueTask<Result<TOut>>> bind,
        MetadataMergeStrategy metadataMergeStrategy = MetadataMergeStrategy.AddOrReplace
    )
    {
        if (!result.IsValid)
        {
            return Result<TOut>.Fail(result.Errors, result.Metadata);
        }

        var newResult = await bind(result.Value).ConfigureAwait(false);
        var mergedMetadata =
            MetadataObjectExtensions.MergeIfNeeded(result.Metadata, newResult.Metadata, metadataMergeStrategy);
        return mergedMetadata is null ? newResult : newResult.ReplaceMetadata(mergedMetadata.Value);
    }
}
