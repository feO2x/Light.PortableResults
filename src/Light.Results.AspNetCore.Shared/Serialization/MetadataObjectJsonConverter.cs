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
    public override MetadataObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization of MetadataObject is not supported");
    }

    public override void Write(Utf8JsonWriter writer, MetadataObject value, JsonSerializerOptions options)
    {
        MetadataValueJsonConverter.WriteMetadataObject(writer, value);
    }
}
