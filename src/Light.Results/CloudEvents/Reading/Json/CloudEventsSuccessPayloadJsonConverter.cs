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
    /// <inheritdoc />
    public override CloudEventsSuccessPayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var httpPayload = ResultJsonReader.ReadSuccessPayload(ref reader);
        return new CloudEventsSuccessPayload(httpPayload.Metadata);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsSuccessPayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsSuccessPayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
