using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="HttpReadSuccessResultPayload" /> payloads.
/// </summary>
public sealed class HttpReadSuccessResultPayloadJsonConverter : JsonConverter<HttpReadSuccessResultPayload>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="HttpReadSuccessResultPayload" />.
    /// </summary>
    public override HttpReadSuccessResultPayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadSuccessPayload(ref reader);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadSuccessResultPayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadSuccessResultPayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
