using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="MetadataObject" /> payloads.
/// </summary>
public sealed class HttpReadMetadataObjectJsonConverter : JsonConverter<MetadataObject>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="MetadataObject" />.
    /// </summary>
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        MetadataJsonReader.ReadMetadataObject(ref reader);

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadMetadataObjectJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
