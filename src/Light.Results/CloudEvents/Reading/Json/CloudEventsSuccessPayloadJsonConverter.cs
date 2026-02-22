using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Http.Reading.Json;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsSuccessPayload" /> from CloudEvents data payloads.
/// </summary>
public sealed class CloudEventsSuccessPayloadJsonConverter : JsonConverter<CloudEventsSuccessPayload>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="CloudEventsSuccessPayload" />.
    /// </summary>
    public override CloudEventsSuccessPayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var httpPayload = ResultJsonReader.ReadSuccessPayload(ref reader);
        return new CloudEventsSuccessPayload(httpPayload.Metadata);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsSuccessPayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsSuccessPayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
