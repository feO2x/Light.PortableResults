namespace Light.Results.AspNetCore.Shared;

/// <summary>
/// Specifies when metadata should be serialized in HTTP responses.
/// </summary>
public enum MetadataSerializationMode
{
    /// <summary>
    /// Metadata is only serialized for error responses (Problem Details).
    /// Success responses return the value directly without metadata.
    /// This is the default behavior.
    /// </summary>
    ErrorsOnly,

    /// <summary>
    /// Metadata is always serialized, even for success responses.
    /// Success responses use a wrapped format: { "value": T, "metadata": {...} }.
    /// </summary>
    Always
}
