using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for metadata values.
/// </summary>
public static class MetadataJsonReader
{
    /// <summary>
    /// Reads a <see cref="MetadataValue" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata value.</returns>
    public static MetadataValue ReadMetadataValue(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        EnsureReaderHasToken(ref reader);

        return reader.TokenType switch
        {
            JsonTokenType.Null => MetadataValue.FromNull(annotation),
            JsonTokenType.True => MetadataValue.FromBoolean(true, annotation),
            JsonTokenType.False => MetadataValue.FromBoolean(false, annotation),
            JsonTokenType.Number => ReadNumber(ref reader, annotation),
            JsonTokenType.String => MetadataValue.FromString(reader.GetString(), annotation),
            JsonTokenType.StartArray => MetadataValue.FromArray(ReadMetadataArray(ref reader, annotation), annotation),
            JsonTokenType.StartObject => MetadataValue.FromObject(
                ReadMetadataObject(ref reader, annotation),
                annotation
            ),
            _ => throw new JsonException($"Unsupported JSON token '{reader.TokenType}' for MetadataValue.")
        };
    }

    /// <summary>
    /// Reads a <see cref="MetadataObject" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata object.</returns>
    public static MetadataObject ReadMetadataObject(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType == JsonTokenType.Null)
        {
            return MetadataObject.Empty;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for metadata.");
        }

        using var builder = MetadataObjectBuilder.Create();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in metadata object.");
            }

            var propertyName = reader.GetString();
            if (propertyName is null)
            {
                throw new JsonException("Metadata property names must be strings.");
            }

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading metadata value.");
            }

            var value = ReadMetadataValue(ref reader, annotation);
            builder.AddOrReplace(propertyName, value);
        }

        return builder.Build();
    }

    /// <summary>
    /// Reads a <see cref="MetadataArray" /> from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="annotation">The annotation applied to parsed values.</param>
    /// <returns>The parsed metadata array.</returns>
    public static MetadataArray ReadMetadataArray(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array for metadata.");
        }

        using var builder = MetadataArrayBuilder.Create();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var value = ReadMetadataValue(ref reader, annotation);
            builder.Add(value);
        }

        return builder.Build();
    }

    private static MetadataValue ReadNumber(ref Utf8JsonReader reader, MetadataValueAnnotation annotation) =>
        reader.TryGetInt64(out var longValue) ?
            MetadataValue.FromInt64(longValue, annotation) :
            MetadataValue.FromDouble(reader.GetDouble(), annotation);

    private static void EnsureReaderHasToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.None)
        {
            return;
        }

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON.");
        }
    }
}
