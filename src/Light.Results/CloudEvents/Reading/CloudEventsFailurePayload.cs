using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Represents a deserialized failed CloudEvents data payload.
/// </summary>
public readonly struct CloudEventsFailurePayload
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsFailurePayload" />.
    /// </summary>
    public CloudEventsFailurePayload(Errors errors, MetadataObject? metadata)
    {
        Errors = errors;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the parsed errors.
    /// </summary>
    public Errors Errors { get; }

    /// <summary>
    /// Gets the parsed optional metadata.
    /// </summary>
    public MetadataObject? Metadata { get; }
}
