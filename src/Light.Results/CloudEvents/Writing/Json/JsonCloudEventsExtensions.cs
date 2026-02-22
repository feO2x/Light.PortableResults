using System;
using System.Globalization;
using System.Text.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Writing;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// Provides methods to serialize CloudEvents as JSON.
/// </summary>
public static class JsonCloudEventsExtensions
{
    /// <summary>
    /// Serializes the contents of a <see cref="CloudEventsEnvelopeForWriting" /> into the provided
    /// <see cref="Utf8JsonWriter" /> using the supplied serializer options.
    /// </summary>
    /// <param name="writer">The writer that receives the CloudEvents JSON.</param>
    /// <param name="envelope">The envelope whose metadata and error details will be emitted.</param>
    /// <param name="serializerOptions">The serializer options used for writing complex values.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteCloudEvents(
        this Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting envelope,
        JsonSerializerOptions serializerOptions
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var shouldWriteMetadataToCloudEventsDataSectionWhenResultIsValid =
            envelope.CheckIfMetadataShouldBeWrittenForValidResult<CloudEventsEnvelopeForWriting, Result>();
        var shouldWriteData = !envelope.Data.IsValid || shouldWriteMetadataToCloudEventsDataSectionWhenResultIsValid;

        WriteEnvelopeStart(
            writer,
            envelope.Type,
            envelope.Source,
            envelope.Id,
            envelope.Subject,
            envelope.Time,
            envelope.DataSchema,
            envelope.ExtensionAttributes,
            includeData: shouldWriteData,
            envelope.Data.IsValid
        );

        if (shouldWriteData)
        {
            writer.WritePropertyName("data");
            if (envelope.Data.IsValid)
            {
                writer.WriteStartObject();
                writer.WriteMetadataPropertyAndValue(envelope.Data.Metadata!.Value, serializerOptions);
                writer.WriteEndObject();
            }
            else
            {
                WriteFailurePayload(
                    writer,
                    envelope.Data.Errors,
                    envelope.Data.Metadata,
                    serializerOptions
                );
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Serializes a typed <see cref="CloudEventsEnvelopeForWriting{T}" /> to JSON, including the
    /// result value and optional metadata when configured.
    /// </summary>
    /// <typeparam name="T">The type of the value contained in the envelope.</typeparam>
    /// <param name="writer">The writer that receives the CloudEvents JSON.</param>
    /// <param name="envelope">The envelope containing the typed payload and metadata.</param>
    /// <param name="serializerOptions">The serializer options used when writing the payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
    public static void WriteCloudEvents<T>(
        this Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting<T> envelope,
        JsonSerializerOptions serializerOptions
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        WriteEnvelopeStart(
            writer,
            envelope.Type,
            envelope.Source,
            envelope.Id,
            envelope.Subject,
            envelope.Time,
            envelope.DataSchema,
            envelope.ExtensionAttributes,
            includeData: true,
            envelope.Data.IsValid
        );

        writer.WritePropertyName("data");
        if (envelope.Data.IsValid)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteGenericValue(envelope.Data.Value, serializerOptions);
            if (envelope.CheckIfMetadataShouldBeWrittenForValidResult<CloudEventsEnvelopeForWriting<T>, Result<T>>())
            {
                writer.WriteMetadataPropertyAndValue(envelope.Data.Metadata!.Value, serializerOptions);
            }

            writer.WriteEndObject();
        }
        else
        {
            WriteFailurePayload(writer, envelope.Data.Errors, envelope.Data.Metadata, serializerOptions);
        }

        writer.WriteEndObject();
    }

    private static void WriteEnvelopeStart(
        Utf8JsonWriter writer,
        string type,
        string source,
        string id,
        string? subject,
        DateTimeOffset? time,
        string? dataSchema,
        MetadataObject? extensionAttributes,
        bool includeData,
        bool isSuccess
    )
    {
        writer.WriteStartObject();

        writer.WriteString("specversion", CloudEventsConstants.SpecVersion);
        writer.WriteString("type", type);
        writer.WriteString("source", source);

        if (!string.IsNullOrWhiteSpace(subject))
        {
            writer.WriteString("subject", subject);
        }

        if (!string.IsNullOrWhiteSpace(dataSchema))
        {
            writer.WriteString("dataschema", dataSchema);
        }

        writer.WriteString("id", id);
        if (time.HasValue)
        {
            writer.WriteString("time", time.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        writer.WriteString(CloudEventsConstants.LightResultsOutcomeAttributeName, isSuccess ? "success" : "failure");

        if (includeData)
        {
            writer.WriteString("datacontenttype", CloudEventsConstants.JsonContentType);
        }

        WriteExtensionAttributes(writer, extensionAttributes);
    }

    private static void WriteFailurePayload(
        Utf8JsonWriter writer,
        Errors errors,
        MetadataObject? metadata,
        JsonSerializerOptions serializerOptions
    )
    {
        writer.WriteStartObject();
        writer.WriteRichErrors(errors, isValidationResponse: false, serializerOptions);
        if (metadata is not null &&
            metadata.Value.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeInCloudEventsData))
        {
            writer.WriteMetadataPropertyAndValue(metadata.Value, serializerOptions);
        }

        writer.WriteEndObject();
    }

    private static void WriteExtensionAttributes(Utf8JsonWriter writer, MetadataObject? convertedAttributes)
    {
        if (convertedAttributes is null)
        {
            return;
        }

        foreach (var keyValuePair in convertedAttributes.Value)
        {
            if (CloudEventsConstants.StandardAttributeNames.Contains(keyValuePair.Key) ||
                CloudEventsConstants.ForbiddenConvertedAttributeNames.Contains(keyValuePair.Key))
            {
                continue;
            }

            writer.WritePropertyName(keyValuePair.Key);
            writer.WriteMetadataValue(
                keyValuePair.Value,
                MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
            );
        }
    }
}
