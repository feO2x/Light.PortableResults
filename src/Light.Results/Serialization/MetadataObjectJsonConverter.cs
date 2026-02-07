using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.Serialization;

/// <summary>
/// JSON converter for <see cref="MetadataObject" />.
/// </summary>
public sealed class MetadataObjectJsonConverter : JsonConverter<MetadataObject>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="MetadataObject" />.
    /// </summary>
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        MetadataJsonReader.ReadMetadataObject(ref reader);

    /// <summary>
    /// Writes the JSON representation for the specified metadata object.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The metadata object.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options) =>
        MetadataValueJsonConverter.WriteMetadataObject(writer, value);
}
