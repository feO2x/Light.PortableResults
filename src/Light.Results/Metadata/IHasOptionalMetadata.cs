namespace Light.Results.Metadata;

/// <summary>
/// Represents a type that can optionally carry metadata.
/// </summary>
/// <typeparam name="T">The implementing type.</typeparam>
public interface IHasOptionalMetadata<T>
    where T : struct, IHasOptionalMetadata<T>
{
    /// <summary>
    /// Gets the optional metadata associated with this instance.
    /// </summary>
    MetadataObject? Metadata { get; }

    /// <summary>
    /// Returns a new instance with the specified metadata. Pass <c>null</c> to clear metadata.
    /// </summary>
    /// <param name="metadata">The metadata to set, or <c>null</c> to clear.</param>
    /// <returns>A new instance with the specified metadata.</returns>
    T ReplaceMetadata(MetadataObject? metadata);
}
