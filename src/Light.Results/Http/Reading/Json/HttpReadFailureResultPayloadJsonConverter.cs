using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="HttpReadFailureResultPayload" /> payloads.
/// </summary>
public sealed class HttpReadFailureResultPayloadJsonConverter : JsonConverter<HttpReadFailureResultPayload>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="HttpReadFailureResultPayload" />.
    /// </summary>
    public override HttpReadFailureResultPayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return ResultJsonReader.ReadFailurePayload(ref reader);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        HttpReadFailureResultPayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadFailureResultPayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
