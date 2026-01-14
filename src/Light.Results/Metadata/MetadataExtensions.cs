namespace Light.Results.Metadata;

/// <summary>
/// Provides extension methods for types implementing <see cref="IHasOptionalMetadata{T}" />.
/// </summary>
public static class MetadataExtensions
{
    /// <param name="result">The instance to clear metadata from.</param>
    /// <typeparam name="T">The type implementing <see cref="IHasOptionalMetadata{T}" />.</typeparam>
    extension<T>(T result) where T : struct, IHasOptionalMetadata<T>
    {
        /// <summary>
        /// Returns a new instance with no metadata.
        /// </summary>
        /// <returns>A new instance with no metadata.</returns>
        public T ClearMetadata() => result.ReplaceMetadata(null);

        /// <summary>
        /// Merges the specified metadata with the metadata of this instance and returns a new instance.
        /// </summary>
        /// <param name="properties">The metadata properties to merge.</param>
        /// <returns>A new instance with the merged metadata.</returns>
        public T MergeMetadata(params (string Key, MetadataValue Value)[] properties)
        {
            var newMetadata = result.Metadata?.With(properties) ?? MetadataObject.Create(properties);
            return result.ReplaceMetadata(newMetadata);
        }

        /// <summary>
        /// Merges the specified metadata with the metadata of this instance and returns a new instance.
        /// </summary>
        /// <param name="other">The metadata to merge.</param>
        /// <param name="strategy">The merge strategy to use.</param>
        /// <returns>A new instance with the merged metadata.</returns>
        public T MergeMetadata(
            MetadataObject other,
            MetadataMergeStrategy strategy = MetadataMergeStrategy.AddOrReplace
        )
        {
            var merged = MetadataObjectExtensions.MergeIfNeeded(result.Metadata, other, strategy);
            return merged is null ? result : result.ReplaceMetadata(merged.Value);
        }
    }
}
