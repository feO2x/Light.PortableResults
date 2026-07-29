using System;
using System.Text.Json;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.SharedJsonSerialization.Writing;

/// <summary>
/// Provides methods to serialize Light.PortableResults metadata values to JSON.
/// </summary>
public static class MetadataExtensions
{
    /// <summary>
    /// Writes the metadata JSON object in a property named <c>metadata</c>.
    /// </summary>
    /// <param name="writer">The System.Text.Json writer instance which writes the target JSON document.</param>
    /// <param name="metadata">The metadata object to serialize.</param>
    /// <param name="requiredAnnotation">
    /// The annotation that must be present on metadata values so that they are included in the JSON document.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteMetadataPropertyAndValue(
        this Utf8JsonWriter writer,
        MetadataObject metadata,
        MetadataValueAnnotation requiredAnnotation
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.WritePropertyName("metadata");
        writer.WriteMetadataObject(metadata, requiredAnnotation);
    }

    /// <summary>
    /// Writes the JSON representation for the specified metadata value.
    /// </summary>
    /// <param name="writer">The System.Text.Json writer instance which writes the target JSON document.</param>
    /// <param name="value">The metadata value to be written.</param>
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

        switch (value.JsonShape)
        {
            case MetadataJsonShape.Null:
                writer.WriteNullValue();
                break;
            case MetadataJsonShape.Boolean:
                value.TryGetBoolean(out var booleanMetadataValue);
                writer.WriteBooleanValue(booleanMetadataValue);
                break;
            case MetadataJsonShape.Number:
                WriteNumberValue(writer, value);
                break;
            case MetadataJsonShape.String:
                writer.WriteStringValue(value.ToCanonicalString());
                break;
            case MetadataJsonShape.Array:
                value.TryGetArray(out var arrayMetadataValue);
                writer.WriteMetadataArray(arrayMetadataValue, requiredAnnotation);
                break;
            case MetadataJsonShape.Object:
                value.TryGetObject(out var objectMetadataValue);
                writer.WriteMetadataObject(objectMetadataValue, requiredAnnotation);
                break;
        }
    }

    private static void WriteNumberValue(Utf8JsonWriter writer, MetadataValue value)
    {
        switch (value.Kind.GetNumberEncoding())
        {
            case MetadataNumberEncoding.Int64:
                value.TryGetInt64(out var int64MetadataValue);
                writer.WriteNumberValue(int64MetadataValue);
                return;
            case MetadataNumberEncoding.Decimal:
                value.TryGetDecimal(out var decimalMetadataValue);
                writer.WriteNumberValue(decimalMetadataValue);
                return;
            case MetadataNumberEncoding.Double:
            case MetadataNumberEncoding.Single:
                // These are the only kinds whose JSON text is not what the writer's own number formatting
                // produces: a whole-number Double or Single carries a trailing ".0" so that it does not read
                // back as an Int64. The canonical text is therefore built and written raw. Validation is
                // skipped because the text comes from our own formatter and non-finite values are rejected at
                // construction, so it is always a well-formed JSON number.
                writer.WriteRawValue(value.ToCanonicalString(), skipInputValidation: true);
                return;
            case MetadataNumberEncoding.None:
                throw new InvalidOperationException(
                    $"Metadata kind '{value.Kind}' does not have a JSON number shape."
                );
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
