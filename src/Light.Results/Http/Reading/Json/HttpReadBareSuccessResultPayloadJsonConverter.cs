using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="HttpReadBareSuccessResultPayload{T}" /> payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class HttpReadBareSuccessResultPayloadJsonConverter<T> :
    JsonConverter<HttpReadBareSuccessResultPayload<T>>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="HttpReadBareSuccessResultPayload{T}" />.
    /// </summary>
    public override HttpReadBareSuccessResultPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadBareSuccessPayload<T>(ref reader, options);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadBareSuccessResultPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadBareSuccessResultPayloadJsonConverter<>)} supports deserialization only. Use a serialization converter for writing."
        );
}
