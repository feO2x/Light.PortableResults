using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Represents a deserialized successful generic CloudEvents data payload interpreted as a wrapped value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct CloudEventsWrappedSuccessPayload<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsWrappedSuccessPayload{T}" />.
    /// </summary>
    public CloudEventsWrappedSuccessPayload(T value, MetadataObject? metadata)
    {
        Value = value;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the parsed result value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the parsed metadata.
    /// </summary>
    public MetadataObject? Metadata { get; }
}
