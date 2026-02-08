using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.Http.Writing.Json;

/// <summary>
/// JSON converter for writing <see cref="MetadataObject" /> payloads.
/// </summary>
public sealed class HttpWriteMetadataObjectJsonConverter : JsonConverter<MetadataObject>
{
    /// <summary>
    /// Reading is not supported by this converter.
    /// </summary>
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpWriteMetadataObjectJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified metadata object.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options) =>
        HttpWriteMetadataValueJsonConverter.WriteMetadataObject(writer, value);
}
