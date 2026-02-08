using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

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
        WriteMetadataValue(writer, value);

    /// <summary>
    /// Writes the JSON representation for the specified metadata value.
    /// </summary>
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

    /// <summary>
    /// Writes the JSON representation for the specified metadata array.
    /// </summary>
    public static void WriteMetadataArray(Utf8JsonWriter writer, MetadataArray array)
    {
        writer.WriteStartArray();
        foreach (var metadataValue in array)
        {
            if (metadataValue.HasAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody))
            {
                WriteMetadataValue(writer, metadataValue);
            }
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata object.
    /// </summary>
    public static void WriteMetadataObject(Utf8JsonWriter writer, MetadataObject metadataObject)
    {
        writer.WriteStartObject();
        foreach (var keyValuePair in metadataObject)
        {
            if (!keyValuePair.Value.HasAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody))
            {
                continue;
            }

            writer.WritePropertyName(keyValuePair.Key);
            WriteMetadataValue(writer, keyValuePair.Value);
        }

        writer.WriteEndObject();
    }
}
