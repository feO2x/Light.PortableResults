using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading.Json;

/// <summary>
/// Represents a deserialized successful non-generic payload for HTTP result reading.
/// </summary>
public readonly struct HttpReadSuccessResultPayload
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadSuccessResultPayload" />.
    /// </summary>
    public HttpReadSuccessResultPayload(MetadataObject? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the metadata parsed from the payload.
    /// </summary>
    public MetadataObject? Metadata { get; }
}
