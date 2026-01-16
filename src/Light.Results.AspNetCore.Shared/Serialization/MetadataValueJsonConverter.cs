using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.Shared.Serialization;

/// <summary>
/// JSON converter for <see cref="MetadataValue" />.
/// </summary>
public sealed class MetadataValueJsonConverter : JsonConverter<MetadataValue>
{
    public override MetadataValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization of MetadataValue is not supported");
    }

    public override void Write(Utf8JsonWriter writer, MetadataValue value, JsonSerializerOptions options)
    {
        WriteMetadataValue(writer, value);
    }

    public static void WriteMetadataValue(Utf8JsonWriter writer, MetadataValue value)
    {
        switch (value.Kind)
        {
            case MetadataKind.Null:
                writer.WriteNullValue();
                break;
            case MetadataKind.Boolean:
                value.TryGetBoolean(out var boolVal);
                writer.WriteBooleanValue(boolVal);
                break;
            case MetadataKind.Int64:
                value.TryGetInt64(out var longVal);
                writer.WriteNumberValue(longVal);
                break;
            case MetadataKind.Double:
                value.TryGetDouble(out var doubleVal);
                writer.WriteNumberValue(doubleVal);
                break;
            case MetadataKind.String:
                value.TryGetString(out var stringVal);
                writer.WriteStringValue(stringVal);
                break;
            case MetadataKind.Array:
                value.TryGetArray(out var arrayVal);
                WriteMetadataArray(writer, arrayVal);
                break;
            case MetadataKind.Object:
                value.TryGetObject(out var objVal);
                WriteMetadataObject(writer, objVal);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    public static void WriteMetadataArray(Utf8JsonWriter writer, MetadataArray array)
    {
        writer.WriteStartArray();
        foreach (var item in array)
        {
            WriteMetadataValue(writer, item);
        }

        writer.WriteEndArray();
    }

    public static void WriteMetadataObject(Utf8JsonWriter writer, MetadataObject obj)
    {
        writer.WriteStartObject();
        foreach (var kvp in obj)
        {
            writer.WritePropertyName(kvp.Key);
            WriteMetadataValue(writer, kvp.Value);
        }

        writer.WriteEndObject();
    }
}
