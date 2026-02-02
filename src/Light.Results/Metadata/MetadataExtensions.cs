// ReSharper disable ConvertToExtensionBlock -- as of 2026-01-14, the C# compiler produces a warning for
// extension blocks in combination with Nullable Reference Types, which is why we resort back to extension methods.
/* /Users/kenny/Code/Light.Results/src/Light.Results/MetadataExtensions/Tracing.cs(30,34): error CS8620: Argument of type '(string Key, MetadataValue Value)' cannot be used for parameter 'properties' of type '(string Key, MetadataValue Value)[]' in 'T extension<T>(T).MergeMetadata(params (string Key, MetadataValue Value)[] properties)' due to differences in the nullability of reference types. [/Users/kenny/Code/Light.Results/src/Light.Results/Light.Results.csproj]
   /Users/kenny/Code/Light.Results/src/Light.Results/MetadataExtensions/Tracing.cs(38,34): error CS8620: Argument of type '(string Key, MetadataValue Value)' cannot be used for parameter 'properties' of type '(string Key, MetadataValue Value)[]' in 'T extension<T>(T).MergeMetadata(params (string Key, MetadataValue Value)[] properties)' due to differences in the nullability of reference types. [/Users/kenny/Code/Light.Results/src/Light.Results/Light.Results.csproj]
   /Users/kenny/Code/Light.Results/src/Light.Results/MetadataExtensions/Tracing.cs(52,17): error CS8620: Argument of type '(string Key, MetadataValue Value)' cannot be used for parameter 'properties' of type '(string Key, MetadataValue Value)[]' in 'T extension<T>(T).MergeMetadata(params (string Key, MetadataValue Value)[] properties)' due to differences in the nullability of reference types. [/Users/kenny/Code/Light.Results/src/Light.Results/Light.Results.csproj]
   /Users/kenny/Code/Light.Results/src/Light.Results/MetadataExtensions/Tracing.cs(53,17): error CS8620: Argument of type '(string Key, MetadataValue Value)' cannot be used for parameter 'properties' of type '(string Key, MetadataValue Value)[]' in 'T extension<T>(T).MergeMetadata(params (string Key, MetadataValue Value)[] properties)' due to differences in the nullability of reference types. [/Users/kenny/Code/Light.Results/src/Light.Results/Light.Results.csproj]
       0 Warning(s)
       4 Error(s)

   Time Elapsed 00:00:00.47
*/
// This occurs when we use an extension block in this MetadataExtensions.cs file - I have no clue, why this is happening - need to report this as a bug to Microsoft.
// .NET 10.0.100 SDK with C# 14.0


namespace Light.Results.Metadata;

/// <summary>
/// Provides extension methods for types implementing <see cref="ICanReplaceMetadata{T}" />.
/// </summary>
public static class MetadataExtensions
{
    /// <summary>
    /// Returns a new instance with no metadata.
    /// </summary>
    /// <param name="result">The instance to clear metadata from.</param>
    /// <typeparam name="T">The type implementing <see cref="ICanReplaceMetadata{T}" />.</typeparam>
    /// <returns>A new instance with no metadata.</returns>
    public static T ClearMetadata<T>(this T result) where T : struct, ICanReplaceMetadata<T> =>
        result.ReplaceMetadata(null);

    /// <summary>
    /// Merges the specified metadata with the metadata of this instance and returns a new instance.
    /// </summary>
    /// <param name="properties">The metadata properties to merge.</param>
    /// <param name="result">The instance to clear metadata from.</param>
    /// <typeparam name="T">The type implementing <see cref="ICanReplaceMetadata{T}" />.</typeparam>
    /// <returns>A new instance with the merged metadata.</returns>
    public static T MergeMetadata<T>(this T result, params (string Key, MetadataValue Value)[] properties)
        where T : struct, ICanReplaceMetadata<T>
    {
        var newMetadata = result.Metadata?.With(properties) ?? MetadataObject.Create(properties);
        return result.ReplaceMetadata(newMetadata);
    }

    /// <summary>
    /// Merges the specified metadata with the metadata of this instance and returns a new instance.
    /// </summary>
    /// <param name="other">The metadata to merge.</param>
    /// <param name="strategy">The merge strategy to use.</param>
    /// <param name="result">The instance to clear metadata from.</param>
    /// <typeparam name="T">The type implementing <see cref="ICanReplaceMetadata{T}" />.</typeparam>
    /// <returns>A new instance with the merged metadata.</returns>
    public static T MergeMetadata<T>(
        this T result,
        MetadataObject other,
        MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace
    )
        where T : struct, ICanReplaceMetadata<T>
    {
        var merged = MetadataObjectExtensions.MergeIfNeeded(result.Metadata, other, strategy);
        return merged is null ? result : result.ReplaceMetadata(merged.Value);
    }
}
