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
    /// <inheritdoc />
    public override HttpReadAutoSuccessResultPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadAutoSuccessPayload<T>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadAutoSuccessResultPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadAutoSuccessResultPayloadJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
