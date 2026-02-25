namespace Light.PortableResults.Http.Reading.Json;

/// <summary>
/// Represents a deserialized successful generic payload interpreted as a bare value.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct HttpReadBareSuccessResultPayload<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadBareSuccessResultPayload{T}" />.
    /// </summary>
    public HttpReadBareSuccessResultPayload(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the parsed result value.
    /// </summary>
    public T Value { get; }
}
