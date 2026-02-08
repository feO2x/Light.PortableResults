using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="MetadataValue" /> payloads.
/// </summary>
public sealed class HttpReadMetadataValueJsonConverter : JsonConverter<MetadataValue>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="MetadataValue" />.
    /// </summary>
    public override MetadataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        MetadataJsonReader.ReadMetadataValue(ref reader);

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, MetadataValue value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadMetadataValueJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
