using System;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.SharedJsonSerialization.Reading;

/// <summary>
/// Provides transport-agnostic JSON parsing helpers for result payload fragments.
/// </summary>
public static class SharedResultJsonReader
{
    /// <summary>
    /// Reads a rich Light.Results <c>errors</c> array from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the start of the errors array.</param>
    /// <param name="metadataAnnotation">The annotation applied to parsed metadata values.</param>
    /// <returns>The parsed <see cref="Errors" /> collection, or the default instance when the array is empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON shape is invalid.</exception>
    public static Errors ReadRichErrors(
        ref Utf8JsonReader reader,
        MetadataValueAnnotation metadataAnnotation = MetadataValueAnnotation.SerializeInHttpResponseBody
    )
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("errors must be an array.");
        }

        var errors = new List<Error>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Each error must be a JSON object.");
            }

            errors.Add(ReadSingleError(ref reader, metadataAnnotation));
        }

        return errors.Count == 0 ? default : new Errors(errors.ToArray());
    }

    private static Error ReadSingleError(ref Utf8JsonReader reader, MetadataValueAnnotation metadataAnnotation)
    {
        string? message = null;
        string? code = null;
        string? target = null;
        var category = ErrorCategory.Unclassified;
        MetadataObject? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in error object.");
            }

            if (reader.ValueTextEquals("message"))
            {
                message = ReadRequiredString(ref reader);
            }
            else if (reader.ValueTextEquals("code"))
            {
                code = ReadOptionalString(ref reader);
            }
            else if (reader.ValueTextEquals("target"))
            {
                target = ReadOptionalString(ref reader);
            }
            else if (reader.ValueTextEquals("category"))
            {
                var categoryString = ReadOptionalString(ref reader);
                if (!string.IsNullOrWhiteSpace(categoryString) &&
                    !Enum.TryParse(categoryString, ignoreCase: true, out category))
                {
                    throw new JsonException($"Unknown error category '{categoryString}'.");
                }
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading error metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader, metadataAnnotation);
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping error property.");
                }

                reader.Skip();
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new JsonException("Error objects must include a message.");
        }

        return new Error
        {
            Message = message!,
            Code = code,
            Target = target,
            Category = category,
            Metadata = metadata
        };
    }

    private static string ReadRequiredString(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading string value.");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string value.");
        }

        return reader.GetString() ?? string.Empty;
    }

    private static string? ReadOptionalString(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading string value.");
        }

        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            _ => throw new JsonException("Expected string or null value.")
        };
    }
}
