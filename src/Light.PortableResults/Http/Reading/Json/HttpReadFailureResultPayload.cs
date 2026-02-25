using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading.Json;

/// <summary>
/// Represents a deserialized failure payload for HTTP result reading.
/// </summary>
public readonly struct HttpReadFailureResultPayload
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadFailureResultPayload" />.
    /// </summary>
    public HttpReadFailureResultPayload(Errors errors, MetadataObject? metadata)
    {
        Errors = errors;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the errors parsed from the payload.
    /// </summary>
    public Errors Errors { get; }

    /// <summary>
    /// Gets the metadata parsed from the payload.
    /// </summary>
    public MetadataObject? Metadata { get; }
}
