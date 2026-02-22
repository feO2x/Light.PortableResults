using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Http.Reading.Json;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="CloudEventsBareSuccessPayload{T}" /> from CloudEvents data payloads.
/// </summary>
/// <typeparam name="T">The payload value type.</typeparam>
public sealed class CloudEventsBareSuccessPayloadJsonConverter<T> : JsonConverter<CloudEventsBareSuccessPayload<T>>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="CloudEventsBareSuccessPayload{T}" />.
    /// </summary>
    public override CloudEventsBareSuccessPayload<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var httpPayload = ResultJsonReader.ReadBareSuccessPayload<T>(ref reader, options);
        return new CloudEventsBareSuccessPayload<T>(httpPayload.Value);
    }

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsBareSuccessPayload<T> value,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsBareSuccessPayloadJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
