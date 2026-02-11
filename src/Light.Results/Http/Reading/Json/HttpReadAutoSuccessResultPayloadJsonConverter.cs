using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="HttpReadAutoSuccessResultPayload{T}" /> payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class HttpReadAutoSuccessResultPayloadJsonConverter<T> :
    JsonConverter<HttpReadAutoSuccessResultPayload<T>>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="HttpReadAutoSuccessResultPayload{T}" />.
    /// </summary>
    public override HttpReadAutoSuccessResultPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadAutoSuccessPayload<T>(ref reader, options);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadAutoSuccessResultPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadAutoSuccessResultPayloadJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
