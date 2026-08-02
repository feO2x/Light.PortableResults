using System;
using System.Globalization;
using System.Text.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;

namespace Light.PortableResults.CloudEvents.Writing.Json;

/// <summary>
/// Provides methods to serialize CloudEvents as JSON.
/// </summary>
public static class JsonCloudEventsExtensions
{
    // Guid's canonical D format is the longest bounded primitive encoding. Arbitrarily long String and Uri
    // values reuse their existing text when this buffer is insufficient.
    private const int CanonicalTextBufferLength = 36;

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
                writer.WriteMetadataPropertyAndValue(
                    envelope.Data.Metadata!.Value,
                    MetadataValueAnnotation.SerializeInCloudEventsData
                );
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
                writer.WriteMetadataPropertyAndValue(
                    envelope.Data.Metadata!.Value,
                    MetadataValueAnnotation.SerializeInCloudEventsData
                );
            }

            writer.WriteEndObject();
        }
        else
        {
            WriteFailurePayload(writer, envelope.Data.Errors, envelope.Data.Metadata, serializerOptions);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Writes one CloudEvents extension attribute using the JSON Event Format context-attribute mapping.
    /// </summary>
    /// <param name="writer">The JSON writer that receives the complete name/value operation.</param>
    /// <param name="attributeName">The lowercase alphanumeric extension attribute name.</param>
    /// <param name="value">The metadata value to encode.</param>
    /// <remarks>
    /// <para>
    /// Null values are omitted. Booleans use JSON booleans, signed integers in the inclusive 32-bit range
    /// use JSON numbers, and every other conforming primitive uses a JSON string containing its canonical
    /// invariant text. This differs from the metadata value's natural JSON shape.
    /// </para>
    /// <para>
    /// The attribute name and string text are validated before the property name is written, so a rejected
    /// attribute cannot leave the writer with a partial JSON property.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> or <paramref name="attributeName" /> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="attributeName" /> is empty, invalid, reserved, or a standard CloudEvents
    /// attribute name.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="value" /> is complex or its string encoding violates the CloudEvents
    /// character contract.
    /// </exception>
    public static void WriteCloudEventsExtensionAttribute(
        this Utf8JsonWriter writer,
        string attributeName,
        MetadataValue value
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        ValidateExtensionAttributeName(attributeName);
        var encoding = value.GetCloudEventsAttributeJsonEncoding();

        switch (encoding)
        {
            case CloudEventsAttributeJsonEncoding.Null:
                return;

            case CloudEventsAttributeJsonEncoding.Boolean:
                value.TryGetBoolean(out var booleanValue);
                writer.WritePropertyName(attributeName);
                writer.WriteBooleanValue(booleanValue);
                return;

            case CloudEventsAttributeJsonEncoding.Integer:
                value.TryGetInt64(out var int64Value);
                writer.WritePropertyName(attributeName);
                writer.WriteNumberValue(int64Value);
                return;

            case CloudEventsAttributeJsonEncoding.String:
                ValidateCallerControlledText(attributeName, value);
                WriteStringAttribute(writer, attributeName, value);
                return;

            default:
                throw new InvalidOperationException(
                    $"Unknown CloudEvents attribute JSON encoding '{encoding}'."
                );
        }
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

        writer.WriteString(CloudEventsConstants.PortableResultsOutcomeAttributeName, isSuccess ? "success" : "failure");

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
            writer.WriteMetadataPropertyAndValue(metadata.Value, MetadataValueAnnotation.SerializeInCloudEventsData);
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
            if (CloudEventsConstants.StandardAttributeNames.Contains(keyValuePair.Key))
            {
                continue;
            }

            writer.WriteCloudEventsExtensionAttribute(keyValuePair.Key, keyValuePair.Value);
        }
    }

    private static void ValidateExtensionAttributeName(string attributeName)
    {
        if (attributeName is null)
        {
            throw new ArgumentNullException(nameof(attributeName));
        }

        if (string.IsNullOrWhiteSpace(attributeName))
        {
            throw new ArgumentException(
                "CloudEvents extension attribute names must not be empty or whitespace.",
                nameof(attributeName)
            );
        }

        if (!CloudEventsAttributeName.IsValidExtensionAttributeName(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvents extension attribute '{attributeName}' is invalid. Only lowercase alphanumeric names are allowed.",
                nameof(attributeName)
            );
        }

        if (CloudEventsConstants.ForbiddenConvertedAttributeNames.Contains(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvents extension attribute '{attributeName}' is reserved.",
                nameof(attributeName)
            );
        }

        if (CloudEventsConstants.StandardAttributeNames.Contains(attributeName))
        {
            throw new ArgumentException(
                $"The CloudEvents attribute '{attributeName}' is standard and cannot use extension encoding.",
                nameof(attributeName)
            );
        }
    }

    private static void ValidateCallerControlledText(string attributeName, MetadataValue value)
    {
        switch (value.Kind)
        {
            case MetadataKind.String:
                value.TryGetString(out var stringValue);
                ValidateAttributeText(attributeName, stringValue.AsSpan());
                return;

            case MetadataKind.Char:
                value.TryGetChar(out var character);
                Span<char> characterText = stackalloc char[1];
                characterText[0] = character;
                ValidateAttributeText(attributeName, characterText);
                return;

            case MetadataKind.Uri:
                value.TryGetUri(out var uri);
                ValidateAttributeText(attributeName, uri!.OriginalString.AsSpan());
                return;
        }
    }

    private static void ValidateAttributeText(string attributeName, ReadOnlySpan<char> text)
    {
        var invalidIndex = CloudEventsAttributeText.IndexOfDisallowedCharacter(text);
        if (invalidIndex < 0)
        {
            return;
        }

        var codePoint = GetCodePoint(text, invalidIndex);
        throw new InvalidOperationException(
            $"CloudEvents extension attribute '{attributeName}' contains disallowed code point " +
            $"U+{codePoint.ToString("X4", CultureInfo.InvariantCulture)} at UTF-16 index {invalidIndex}."
        );
    }

    private static int GetCodePoint(ReadOnlySpan<char> text, int index)
    {
        var first = text[index];
        return first is >= '\uD800' and <= '\uDBFF' &&
               index + 1 < text.Length &&
               text[index + 1] is >= '\uDC00' and <= '\uDFFF' ?
            0x10000 + ((first - '\uD800') << 10) + text[index + 1] - '\uDC00' :
            first;
    }

    private static void WriteStringAttribute(
        Utf8JsonWriter writer,
        string attributeName,
        MetadataValue value
    )
    {
        Span<char> canonicalText = stackalloc char[CanonicalTextBufferLength];
        if (value.TryFormatCanonical(canonicalText, out var charsWritten))
        {
            writer.WritePropertyName(attributeName);
            writer.WriteStringValue(canonicalText.Slice(0, charsWritten));
            return;
        }

        var materializedText = value.ToCanonicalString();
        writer.WritePropertyName(attributeName);
        writer.WriteStringValue(materializedText);
    }
}
