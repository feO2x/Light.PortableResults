namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Represents a deserialized successful generic CloudEvents data payload interpreted as a bare value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct CloudEventsBareSuccessPayload<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventsBareSuccessPayload{T}" />.
    /// </summary>
    public CloudEventsBareSuccessPayload(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed result value.
    /// </summary>
    public T Value { get; }
}
