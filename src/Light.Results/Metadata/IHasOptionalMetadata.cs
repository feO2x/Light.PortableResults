namespace Light.Results.Metadata;

/// <summary>
/// Represents a type that can optionally carry metadata.
/// </summary>
public interface IHasOptionalMetadata
{
    /// <summary>
    /// Gets the optional metadata associated with this instance.
    /// </summary>
    MetadataObject? Metadata { get; }
}
