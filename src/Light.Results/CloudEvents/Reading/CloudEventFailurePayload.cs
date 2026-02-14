using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Represents a deserialized failed CloudEvent data payload.
/// </summary>
public readonly struct CloudEventFailurePayload
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventFailurePayload" />.
    /// </summary>
    public CloudEventFailurePayload(Errors errors, MetadataObject? metadata)
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
