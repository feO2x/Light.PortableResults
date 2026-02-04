using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.Shared.Serialization;

/// <summary>
/// JSON converter for <see cref="MetadataObject" />.
/// </summary>
public sealed class MetadataObjectJsonConverter : JsonConverter<MetadataObject>
{
    /// <summary>
    /// Reading is not supported for <see cref="MetadataObject" />.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization of MetadataObject is not supported");
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata object.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The metadata object.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options)
    {
        MetadataValueJsonConverter.WriteMetadataObject(writer, value);
    }
}
