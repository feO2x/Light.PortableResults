using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsFailurePayload" /> from CloudEvents data payloads.
/// </summary>
public sealed class CloudEventsFailurePayloadJsonConverter : JsonConverter<CloudEventsFailurePayload>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="CloudEventsFailurePayload" />.
    /// </summary>
    public override CloudEventsFailurePayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return CloudEventsDataJsonReader.ReadFailurePayload(ref reader);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsFailurePayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsFailurePayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
