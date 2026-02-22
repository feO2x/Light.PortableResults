using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Http.Reading.Json;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsAutoSuccessPayload{T}" /> from CloudEvents data payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class CloudEventsAutoSuccessPayloadJsonConverter<T> : JsonConverter<CloudEventsAutoSuccessPayload<T>>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="CloudEventsAutoSuccessPayload{T}" />.
    /// </summary>
    public override CloudEventsAutoSuccessPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var httpPayload = ResultJsonReader.ReadAutoSuccessPayload<T>(ref reader, options);
        return new CloudEventsAutoSuccessPayload<T>(httpPayload.Value, httpPayload.Metadata);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsAutoSuccessPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsAutoSuccessPayloadJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
