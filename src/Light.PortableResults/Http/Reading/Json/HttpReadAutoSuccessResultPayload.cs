using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading.Json;

/// <summary>
/// Represents a deserialized successful generic payload with automatic wrapper detection.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct HttpReadAutoSuccessResultPayload<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadAutoSuccessResultPayload{T}" />.
    /// </summary>
    public HttpReadAutoSuccessResultPayload(T value, MetadataObject? metadata)
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
