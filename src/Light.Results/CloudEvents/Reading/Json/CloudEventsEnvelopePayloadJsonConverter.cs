using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsEnvelopePayload" /> from CloudEvents JSON envelopes.
/// </summary>
public sealed class CloudEventsEnvelopePayloadJsonConverter : JsonConverter<CloudEventsEnvelopePayload>
{
    /// <inheritdoc />
    public override CloudEventsEnvelopePayload Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return CloudEventsEnvelopeJsonReader.ReadEnvelope(ref reader);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsEnvelopePayload value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsEnvelopePayloadJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
