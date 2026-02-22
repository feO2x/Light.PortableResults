using System.Text.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Reading;

namespace Light.Results.CloudEvents.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for CloudEvents <c>data</c> payloads.
/// </summary>
public static class CloudEventsDataJsonReader
{
    /// <summary>
    /// Reads a failed result payload from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the failed payload object.</param>
    /// <returns>The parsed payload.</returns>
    /// <exception cref="JsonException">Thrown when the payload shape is invalid.</exception>
    public static CloudEventsFailurePayload ReadFailurePayload(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("CloudEvents failure data payload must be a JSON object.");
        }

        Errors errors = default;
        MetadataObject? metadata = null;
        var hasErrors = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in CloudEvents failure data payload.");
            }

            if (reader.ValueTextEquals("errors"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading errors.");
                }

                errors = SharedResultJsonReader.ReadRichErrors(
                    ref reader,
                    MetadataValueAnnotation.SerializeInCloudEventsData
                );
                hasErrors = true;
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(
                        ref reader,
                        MetadataValueAnnotation.SerializeInCloudEventsData
                    );
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping property.");
                }

                reader.Skip();
            }
        }

        if (!hasErrors || errors.IsEmpty)
        {
            throw new JsonException("CloudEvents failure data payload must contain a non-empty errors array.");
        }

        return new CloudEventsFailurePayload(errors, metadata);
    }
}
