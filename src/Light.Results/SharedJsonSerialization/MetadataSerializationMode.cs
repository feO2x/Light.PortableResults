namespace Light.Results.SharedJsonSerialization;

/// <summary>
/// Specifies when metadata should be serialized.
/// </summary>
public enum MetadataSerializationMode
{
    /// <summary>
    /// Metadata is only serialized for failure payloads.
    /// </summary>
    ErrorsOnly,

    /// <summary>
    /// Metadata is always serialized for both success and failure payloads.
    /// </summary>
    Always
}
