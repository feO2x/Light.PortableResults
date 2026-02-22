using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="HttpReadWrappedSuccessResultPayload{T}" /> payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class HttpReadWrappedSuccessResultPayloadJsonConverter<T> :
    JsonConverter<HttpReadWrappedSuccessResultPayload<T>>
{
    /// <inheritdoc />
    public override HttpReadWrappedSuccessResultPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadWrappedSuccessPayload<T>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadWrappedSuccessResultPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadWrappedSuccessResultPayloadJsonConverter<>)} supports deserialization only. Use a serialization converter for writing."
        );
}
