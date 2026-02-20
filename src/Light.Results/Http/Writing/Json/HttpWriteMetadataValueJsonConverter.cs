using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Writing;

namespace Light.Results.Http.Writing.Json;

/// <summary>
/// JSON converter for writing <see cref="MetadataValue" /> payloads.
/// </summary>
public sealed class HttpWriteMetadataValueJsonConverter : JsonConverter<MetadataValue>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override MetadataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpWriteMetadataValueJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified metadata value.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, MetadataValue value, JsonSerializerOptions options) =>
        writer.WriteMetadataValue(value, requiredAnnotation: MetadataValueAnnotation.SerializeInHttpResponseBody);
}
