using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// JSON converter for writing <see cref="CloudEventEnvelopeForWriting" /> values.
/// </summary>
public sealed class CloudEventEnvelopeForWritingJsonConverter : JsonConverter<CloudEventEnvelopeForWriting>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override CloudEventEnvelopeForWriting Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventEnvelopeForWritingJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified envelope.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventEnvelopeForWriting envelope,
        JsonSerializerOptions options
    ) =>
        writer.WriteCloudEvent(envelope, options);
}

/// <summary>
/// JSON converter for writing <see cref="CloudEventEnvelopeForWriting{T}" /> values.
/// </summary>
public sealed class CloudEventEnvelopeForWritingJsonConverter<T> : JsonConverter<CloudEventEnvelopeForWriting<T>>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override CloudEventEnvelopeForWriting<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventEnvelopeForWritingJsonConverter<>)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified envelope.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventEnvelopeForWriting<T> envelope,
        JsonSerializerOptions options
    ) =>
        writer.WriteCloudEvent(envelope, options);
}
