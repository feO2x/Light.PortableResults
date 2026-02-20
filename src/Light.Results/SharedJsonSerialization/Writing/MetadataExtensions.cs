using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.SharedJsonSerialization.Writing;

/// <summary>
/// Provides methods to serialize Light.Results metadata values to JSON.
/// </summary>
public static class MetadataExtensions
{
    /// <summary>
    /// Writes the metadata JSON object in a property named <c>metadata</c>.
    /// </summary>
    public static void WriteMetadataPropertyAndValue(
        this Utf8JsonWriter writer,
        MetadataObject metadata,
        JsonSerializerOptions serializerOptions
    )
    {
        var metadataTypeInfo =
            serializerOptions.GetTypeInfo(typeof(MetadataObject)) ??
            throw new InvalidOperationException(
                $"No JSON serialization metadata was found for type '{typeof(MetadataObject)}' - please ensure that JsonOptions are configured properly"
            );
        writer.WritePropertyName("metadata");
        ((JsonConverter<MetadataObject>) metadataTypeInfo.Converter).Write(writer, metadata, serializerOptions);
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata value.
    /// </summary>
    /// <param name="writer">The System.Text.Json writer instance which writes the target JSON document.</param>
    /// <param name="value">The metadata value to be writter.</param>
    /// <param name="requiredAnnotation">
    /// The annotation that must be present on complex child values so that they are included in the JSON document.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteMetadataValue(
        this Utf8JsonWriter writer,
        MetadataValue value,
        MetadataValueAnnotation requiredAnnotation
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        switch (value.Kind)
        {
            case MetadataKind.Null:
                writer.WriteNullValue();
                break;
            case MetadataKind.Boolean:
                value.TryGetBoolean(out var booleanMetadataValue);
                writer.WriteBooleanValue(booleanMetadataValue);
                break;
            case MetadataKind.Int64:
                value.TryGetInt64(out var int64MetadataValue);
                writer.WriteNumberValue(int64MetadataValue);
                break;
            case MetadataKind.Double:
                value.TryGetDouble(out var doubleMetadataValue);
                writer.WriteNumberValue(doubleMetadataValue);
                break;
            case MetadataKind.String:
                value.TryGetString(out var stringMetadataValue);
                writer.WriteStringValue(stringMetadataValue);
                break;
            case MetadataKind.Array:
                value.TryGetArray(out var arrayMetadataValue);
                writer.WriteMetadataArray(arrayMetadataValue, requiredAnnotation);
                break;
            case MetadataKind.Object:
                value.TryGetObject(out var objectMetadataValue);
                writer.WriteMetadataObject(objectMetadataValue, requiredAnnotation);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata array.
    /// </summary>
    /// <param name="writer">The System.Text.Json writer instance which writes the target JSON document.</param>
    /// <param name="array">The metadata array to be written.</param>
    /// <param name="requiredAnnotation">
    /// The annotation that must be present on complex child values so that they are included in the JSON document.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteMetadataArray(
        this Utf8JsonWriter writer,
        MetadataArray array,
        MetadataValueAnnotation requiredAnnotation
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteStartArray();
        foreach (var metadataValue in array)
        {
            if (metadataValue.HasAnnotation(requiredAnnotation))
            {
                writer.WriteMetadataValue(metadataValue, requiredAnnotation);
            }
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata object.
    /// </summary>
    /// <param name="writer">The System.Text.Json writer instance which writes the target JSON document.</param>
    /// <param name="metadataObject">The metadata object to be written.</param>
    /// <param name="requiredAnnotation">
    /// The annotation that must be present on complex child values so that they are included in the JSON document.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteMetadataObject(
        this Utf8JsonWriter writer,
        MetadataObject metadataObject,
        MetadataValueAnnotation requiredAnnotation
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WriteStartObject();
        foreach (var keyValuePair in metadataObject)
        {
            if (!keyValuePair.Value.HasAnnotation(requiredAnnotation))
            {
                continue;
            }

            writer.WritePropertyName(keyValuePair.Key);
            writer.WriteMetadataValue(keyValuePair.Value, requiredAnnotation);
        }

        writer.WriteEndObject();
    }
}
