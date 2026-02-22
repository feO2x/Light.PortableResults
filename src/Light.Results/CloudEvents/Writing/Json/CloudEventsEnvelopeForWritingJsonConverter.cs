using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// JSON converter for writing <see cref="CloudEventsEnvelopeForWriting" /> values.
/// </summary>
public sealed class CloudEventsEnvelopeForWritingJsonConverter : JsonConverter<CloudEventsEnvelopeForWriting>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override CloudEventsEnvelopeForWriting Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsEnvelopeForWritingJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified envelope.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting envelope,
        JsonSerializerOptions options
    ) =>
        writer.WriteCloudEvents(envelope, options);
}

/// <summary>
/// JSON converter for writing <see cref="CloudEventsEnvelopeForWriting{T}" /> values.
/// </summary>
public sealed class CloudEventsEnvelopeForWritingJsonConverter<T> : JsonConverter<CloudEventsEnvelopeForWriting<T>>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override CloudEventsEnvelopeForWriting<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventsEnvelopeForWritingJsonConverter<>)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified envelope.
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting<T> envelope,
        JsonSerializerOptions options
    ) =>
        writer.WriteCloudEvents(envelope, options);
}
