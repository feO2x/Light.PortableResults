using Light.Results.Metadata;

namespace Light.Results;

/// <summary>
/// Represents an error with a message, optional code, target, and metadata.
/// </summary>
public readonly record struct Error(
    string Message,
    string? Code = null,
    string? Target = null,
    MetadataObject? Metadata = null
)
{
    /// <summary>
    /// Creates a new <see cref="Error" /> with additional metadata properties.
    /// </summary>
    public Error WithMetadata(params (string Key, MetadataValue Value)[] properties)
    {
        var newMetadata = Metadata?.With(properties) ?? MetadataObject.Create(properties);
        return this with { Metadata = newMetadata };
    }

    /// <summary>
    /// Creates a new <see cref="Error" /> with the specified metadata, replacing any existing metadata.
    /// </summary>
    public Error WithMetadata(MetadataObject metadata) => this with { Metadata = metadata };
}
