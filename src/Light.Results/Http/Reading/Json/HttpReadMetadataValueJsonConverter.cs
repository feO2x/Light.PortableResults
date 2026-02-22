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
    /// <inheritdoc />
    public override MetadataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        MetadataJsonReader.ReadMetadataValue(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, MetadataValue value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadMetadataValueJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}
