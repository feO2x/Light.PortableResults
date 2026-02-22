using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Represents a deserialized successful non-generic CloudEvents data payload.
/// </summary>
public readonly struct CloudEventsSuccessPayload
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsSuccessPayload" />.
    /// </summary>
    public CloudEventsSuccessPayload(MetadataObject? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the metadata parsed from the payload.
    /// </summary>
    public MetadataObject? Metadata { get; }
}
