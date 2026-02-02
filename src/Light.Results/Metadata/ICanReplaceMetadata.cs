namespace Light.Results.Metadata;

/// <summary>
/// Represents a type that can optionally carry metadata and can replace it.
/// </summary>
/// <typeparam name="T">The implementing type.</typeparam>
public interface ICanReplaceMetadata<out T> : IHasOptionalMetadata
    where T : struct, ICanReplaceMetadata<T>
{
    /// <summary>
    /// Returns a new instance with the specified metadata. Pass <c>null</c> to clear metadata.
    /// </summary>
    /// <param name="metadata">The metadata to set, or <c>null</c> to clear.</param>
    /// <returns>A new instance with the specified metadata.</returns>
    T ReplaceMetadata(MetadataObject? metadata);
}
