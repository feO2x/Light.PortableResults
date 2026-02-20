using System;
using System.Text.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Reading;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for CloudEvent envelopes.
/// </summary>
public static class CloudEventEnvelopeJsonReader
{
    /// <summary>
    /// Reads a CloudEvent envelope from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the envelope object.</param>
    /// <returns>The parsed envelope payload.</returns>
    /// <exception cref="JsonException">Thrown when the envelope is malformed or violates CloudEvents requirements.</exception>
    public static CloudEventEnvelopePayload ReadEnvelope(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("CloudEvent payload must be a JSON object.");
        }

        string? specVersion = null;
        string? type = null;
        string? source = null;
        string? subject = null;
        string? id = null;
        DateTimeOffset? time = null;
        string? dataContentType = null;
        string? dataSchema = null;

        var dataStart = 0;
        var dataLength = 0;
        var hasData = false;
        var isDataNull = false;

        using var extensionBuilder = MetadataObjectBuilder.Create();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in CloudEvent envelope.");
            }

            if (reader.ValueTextEquals("specversion"))
            {
                specVersion = ReadRequiredStringValue(ref reader, "specversion");
            }
            else if (reader.ValueTextEquals("type"))
            {
                type = ReadRequiredStringValue(ref reader, "type");
            }
            else if (reader.ValueTextEquals("source"))
            {
                source = ReadRequiredStringValue(ref reader, "source");
            }
            else if (reader.ValueTextEquals("subject"))
            {
                subject = ReadOptionalStringValue(ref reader, "subject");
            }
            else if (reader.ValueTextEquals("id"))
            {
                id = ReadRequiredStringValue(ref reader, "id");
            }
            else if (reader.ValueTextEquals("time"))
            {
                var parsedTime = ReadOptionalStringValue(ref reader, "time");
                if (!string.IsNullOrWhiteSpace(parsedTime))
                {
                    if (!DateTimeOffset.TryParse(parsedTime, out var parsed))
                    {
                        throw new JsonException("CloudEvent attribute 'time' must be a valid RFC 3339 timestamp.");
                    }

                    time = parsed;
                }
            }
            else if (reader.ValueTextEquals("datacontenttype"))
            {
                dataContentType = ReadOptionalStringValue(ref reader, "datacontenttype");
            }
            else if (reader.ValueTextEquals("dataschema"))
            {
                dataSchema = ReadOptionalStringValue(ref reader, "dataschema");
            }
            else if (reader.ValueTextEquals("data_base64"))
            {
                throw new JsonException("CloudEvent attribute 'data_base64' is not supported by this integration.");
            }
            else if (reader.ValueTextEquals("data"))
            {
                var positionBeforeDataValue = (int) reader.BytesConsumed;

                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading data.");
                }

                hasData = true;
                if (reader.TokenType == JsonTokenType.Null)
                {
                    isDataNull = true;
                }
                else
                {
                    reader.Skip();
                    var positionAfterDataValue = (int) reader.BytesConsumed;
                    dataStart = positionBeforeDataValue;
                    dataLength = positionAfterDataValue - positionBeforeDataValue;
                }
            }
            else
            {
                var extensionAttributeName = reader.GetString() ??
                                             throw new JsonException(
                                                 "CloudEvent extension attribute names must be strings."
                                             );
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading extension attribute value.");
                }

                var extensionValue = ReadExtensionAttributeValue(ref reader);
                extensionBuilder.AddOrReplace(extensionAttributeName, extensionValue);
            }
        }

        if (string.IsNullOrWhiteSpace(specVersion))
        {
            throw new JsonException("CloudEvent attribute 'specversion' is required.");
        }

        if (!string.Equals(specVersion, CloudEventConstants.SpecVersion, StringComparison.Ordinal))
        {
            throw new JsonException(
                $"CloudEvent attribute 'specversion' must be '{CloudEventConstants.SpecVersion}'."
            );
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new JsonException("CloudEvent attribute 'type' is required.");
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new JsonException("CloudEvent attribute 'source' is required.");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new JsonException("CloudEvent attribute 'id' is required.");
        }

        ValidateSource(source!);
        ValidateDataSchema(dataSchema);
        ValidateDataContentType(dataContentType);

        MetadataObject? extensionAttributes = extensionBuilder.Count == 0 ? null : extensionBuilder.Build();

        return new CloudEventEnvelopePayload(
            type!,
            source!,
            id!,
            subject,
            time,
            dataContentType,
            dataSchema,
            extensionAttributes,
            hasData,
            isDataNull,
            dataStart,
            dataLength
        );
    }

    private static MetadataValue ReadExtensionAttributeValue(ref Utf8JsonReader reader)
    {
        var parsedValue = MetadataJsonReader.ReadMetadataValue(
            ref reader,
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        return IsPrimitive(parsedValue.Kind) ?
            MetadataValueAnnotationHelper.WithAnnotation(
                parsedValue,
                MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
            ) :
            parsedValue;
    }

    private static bool IsPrimitive(MetadataKind metadataKind) =>
        metadataKind is MetadataKind.Null or
                        MetadataKind.Boolean or
                        MetadataKind.Int64 or
                        MetadataKind.Double or
                        MetadataKind.String;

    private static string ReadRequiredStringValue(ref Utf8JsonReader reader, string propertyName)
    {
        if (!reader.Read())
        {
            throw new JsonException($"Unexpected end of JSON while reading '{propertyName}'.");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"CloudEvent attribute '{propertyName}' must be a string.");
        }

        return reader.GetString() ?? string.Empty;
    }

    private static string? ReadOptionalStringValue(ref Utf8JsonReader reader, string propertyName)
    {
        if (!reader.Read())
        {
            throw new JsonException($"Unexpected end of JSON while reading '{propertyName}'.");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"CloudEvent attribute '{propertyName}' must be a string or null.");
        }

        return reader.GetString();
    }

    private static void ValidateSource(string source)
    {
        if (!Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out _))
        {
            throw new JsonException("CloudEvent attribute 'source' must be a valid URI-reference.");
        }
    }

    private static void ValidateDataSchema(string? dataSchema)
    {
        if (string.IsNullOrWhiteSpace(dataSchema))
        {
            return;
        }

        if (!Uri.TryCreate(dataSchema, UriKind.Absolute, out _))
        {
            throw new JsonException("CloudEvent attribute 'dataschema' must be an absolute URI.");
        }
    }

    private static void ValidateDataContentType(string? dataContentType)
    {
        if (string.IsNullOrWhiteSpace(dataContentType))
        {
            return;
        }

        var contentType = dataContentType!;
        var separatorIndex = contentType.IndexOf(';');
        var mediaType = separatorIndex >= 0 ?
            contentType.Substring(0, separatorIndex).Trim() :
            contentType.Trim();

        if (string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new JsonException(
            "CloudEvent attribute 'datacontenttype' must be 'application/json' or a media type ending with '+json'."
        );
    }
}
