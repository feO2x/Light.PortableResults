using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsFailurePayload" /> from CloudEvents data payloads.
/// </summary>
public sealed class CloudEventsFailurePayloadJsonConverter : JsonConverter<CloudEventsFailurePayload>
{
    /// <inheritdoc />
    public override CloudEventsFailurePayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return CloudEventsDataJsonReader.ReadFailurePayload(ref reader);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsFailurePayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsFailurePayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
