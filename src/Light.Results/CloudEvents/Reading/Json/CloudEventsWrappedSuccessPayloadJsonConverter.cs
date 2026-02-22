using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Http.Reading.Json;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsWrappedSuccessPayload{T}" /> from CloudEvents data payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class
    CloudEventsWrappedSuccessPayloadJsonConverter<T> : JsonConverter<CloudEventsWrappedSuccessPayload<T>>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="CloudEventsWrappedSuccessPayload{T}" />.
    /// </summary>
    public override CloudEventsWrappedSuccessPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var httpPayload = ResultJsonReader.ReadWrappedSuccessPayload<T>(ref reader, options);
        return new CloudEventsWrappedSuccessPayload<T>(httpPayload.Value, httpPayload.Metadata);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsWrappedSuccessPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsWrappedSuccessPayloadJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
