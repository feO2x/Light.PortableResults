using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Provides extension methods to serialize <see cref="Result" /> and <see cref="Result{T}" /> values as CloudEvents JSON envelopes.
/// </summary>
public static class CloudEventResultExtensions
{
    /// <summary>
    /// Serializes a non-generic <see cref="Result" /> to a CloudEvents JSON envelope.
    /// </summary>
    public static byte[] ToCloudEvent(
        this Result result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventWriteOptions? options = null
    )
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a non-generic <see cref="Result" /> as a CloudEvents JSON envelope.
    /// </summary>
    public static void WriteCloudEvent(
        this Result result,
        Utf8JsonWriter writer,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var serializerOptions = (options ?? LightResultsCloudEventWriteOptions.Default).SerializerOptions;
        var resolvedOptions = options ?? LightResultsCloudEventWriteOptions.Default;
        var convertedAttributes =
            ConvertMetadataToCloudEventAttributes(result.Metadata, resolvedOptions.ConversionService);
        var resolvedAttributes = ResolveAttributes(
            result.IsValid,
            convertedAttributes,
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            resolvedOptions.Source
        );

        var metadataForData = SelectMetadataByAnnotation(
            result.Metadata,
            MetadataValueAnnotation.SerializeInCloudEventData
        );
        var includeData = !result.IsValid ||
                          (
                              resolvedOptions.MetadataSerializationMode == MetadataSerializationMode.Always &&
                              metadataForData is not null
                          );

        writer.WriteStartObject();
        WriteEnvelopeAttributes(writer, resolvedAttributes, result.IsValid, includeData);
        WriteExtensionAttributes(writer, convertedAttributes);

        if (includeData)
        {
            writer.WritePropertyName("data");
            if (result.IsValid && metadataForData is { } successMetadata)
            {
                writer.WriteStartObject();
                WriteMetadataPropertyAndValue(writer, successMetadata);
                writer.WriteEndObject();
            }
            else
            {
                WriteFailurePayload(writer, result.Errors, metadataForData, serializerOptions);
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Serializes a generic <see cref="Result{T}" /> to a CloudEvents JSON envelope.
    /// </summary>
    public static byte[] ToCloudEvent<T>(
        this Result<T> result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventWriteOptions? options = null
    )
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a generic <see cref="Result{T}" /> as a CloudEvents JSON envelope.
    /// </summary>
    public static void WriteCloudEvent<T>(
        this Result<T> result,
        Utf8JsonWriter writer,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var resolvedOptions = options ?? LightResultsCloudEventWriteOptions.Default;
        var serializerOptions = resolvedOptions.SerializerOptions;
        var convertedAttributes =
            ConvertMetadataToCloudEventAttributes(result.Metadata, resolvedOptions.ConversionService);
        var resolvedAttributes = ResolveAttributes(
            result.IsValid,
            convertedAttributes,
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            resolvedOptions.Source
        );

        var metadataForData = SelectMetadataByAnnotation(
            result.Metadata,
            MetadataValueAnnotation.SerializeInCloudEventData
        );
        var includeWrappedSuccess = result.IsValid &&
                                    resolvedOptions.MetadataSerializationMode == MetadataSerializationMode.Always &&
                                    metadataForData is not null;

        writer.WriteStartObject();
        WriteEnvelopeAttributes(writer, resolvedAttributes, result.IsValid, includeData: true);
        WriteExtensionAttributes(writer, convertedAttributes);

        writer.WritePropertyName("data");
        if (result.IsValid)
        {
            if (includeWrappedSuccess && metadataForData is { } successMetadata)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteGenericValue(result.Value, serializerOptions);
                WriteMetadataPropertyAndValue(writer, successMetadata);
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteGenericValue(result.Value, serializerOptions);
            }
        }
        else
        {
            WriteFailurePayload(writer, result.Errors, metadataForData, serializerOptions);
        }

        writer.WriteEndObject();
    }

    private static MetadataObject? ConvertMetadataToCloudEventAttributes(
        MetadataObject? metadata,
        ICloudEventAttributeConversionService conversionService
    )
    {
        if (metadata is null)
        {
            return null;
        }

        using var builder = MetadataObjectBuilder.Create(metadata.Value.Count);
        foreach (var keyValuePair in metadata.Value)
        {
            if (!keyValuePair.Value.HasAnnotation(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute))
            {
                continue;
            }

            var preparedAttribute = conversionService.PrepareCloudEventAttribute(keyValuePair.Key, keyValuePair.Value);
            builder.AddOrReplace(preparedAttribute.Key, preparedAttribute.Value);
        }

        return builder.Count == 0 ? null : builder.Build();
    }

    private static MetadataObject? SelectMetadataByAnnotation(
        MetadataObject? metadata,
        MetadataValueAnnotation annotation
    )
    {
        if (metadata is null)
        {
            return null;
        }

        using var builder = MetadataObjectBuilder.Create(metadata.Value.Count);
        foreach (var keyValuePair in metadata.Value)
        {
            if (keyValuePair.Value.HasAnnotation(annotation))
            {
                builder.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        return builder.Count == 0 ? null : builder.Build();
    }

    private static ResolvedAttributes ResolveAttributes(
        bool isSuccess,
        MetadataObject? convertedAttributes,
        string? successType,
        string? failureType,
        string? id,
        string? source,
        string? subject,
        string? dataschema,
        DateTimeOffset? time,
        string? defaultSource
    )
    {
        var explicitType = isSuccess ? successType : failureType;
        var resolvedType = !string.IsNullOrWhiteSpace(explicitType) ?
            explicitType! :
            GetStringAttribute(convertedAttributes, "type");

        var resolvedSource = !string.IsNullOrWhiteSpace(source) ?
            source! :
            GetStringAttribute(convertedAttributes, "source") ??
            defaultSource;

        var resolvedId = !string.IsNullOrWhiteSpace(id) ?
            id! :
            GetStringAttribute(convertedAttributes, "id");

        var resolvedSubject = subject ?? GetStringAttribute(convertedAttributes, "subject");
        var resolvedDataSchema = dataschema ?? GetStringAttribute(convertedAttributes, "dataschema");
        var resolvedTime = time ?? GetDateTimeOffsetAttribute(convertedAttributes, "time") ?? DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(resolvedType))
        {
            throw new InvalidOperationException("CloudEvent attribute 'type' could not be resolved.");
        }

        if (string.IsNullOrWhiteSpace(resolvedSource))
        {
            throw new InvalidOperationException("CloudEvent attribute 'source' could not be resolved.");
        }

        if (string.IsNullOrWhiteSpace(resolvedId))
        {
            throw new InvalidOperationException("CloudEvent attribute 'id' could not be resolved.");
        }

        ValidateSourceUriReference(resolvedSource!);
        ValidateDataSchema(resolvedDataSchema);

        return new ResolvedAttributes(
            resolvedType!,
            resolvedSource!,
            resolvedId!,
            resolvedSubject,
            resolvedDataSchema,
            resolvedTime
        );
    }

    private static void WriteEnvelopeAttributes(
        Utf8JsonWriter writer,
        ResolvedAttributes attributes,
        bool isSuccess,
        bool includeData
    )
    {
        writer.WriteString("specversion", CloudEventConstants.SpecVersion);
        writer.WriteString("type", attributes.Type);
        writer.WriteString("source", attributes.Source);
        if (!string.IsNullOrWhiteSpace(attributes.Subject))
        {
            writer.WriteString("subject", attributes.Subject);
        }

        if (!string.IsNullOrWhiteSpace(attributes.DataSchema))
        {
            writer.WriteString("dataschema", attributes.DataSchema);
        }

        writer.WriteString("id", attributes.Id);
        writer.WriteString("time", attributes.Time);
        writer.WriteString(CloudEventConstants.LightResultsOutcomeAttributeName, isSuccess ? "success" : "failure");

        if (includeData)
        {
            writer.WriteString("datacontenttype", CloudEventConstants.JsonContentType);
        }
    }

    private static void WriteExtensionAttributes(Utf8JsonWriter writer, MetadataObject? convertedAttributes)
    {
        if (convertedAttributes is null)
        {
            return;
        }

        foreach (var keyValuePair in convertedAttributes.Value)
        {
            if (CloudEventConstants.StandardAttributeNames.Contains(keyValuePair.Key) ||
                CloudEventConstants.ForbiddenConvertedAttributeNames.Contains(keyValuePair.Key))
            {
                continue;
            }

            writer.WritePropertyName(keyValuePair.Key);
            WriteMetadataValue(writer, keyValuePair.Value);
        }
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
        if (metadata is not null)
        {
            WriteMetadataPropertyAndValue(writer, metadata.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteMetadataPropertyAndValue(Utf8JsonWriter writer, MetadataObject metadata)
    {
        writer.WritePropertyName("metadata");
        WriteMetadataObject(writer, metadata);
    }

    private static void WriteMetadataObject(Utf8JsonWriter writer, MetadataObject metadataObject)
    {
        writer.WriteStartObject();
        foreach (var keyValuePair in metadataObject)
        {
            writer.WritePropertyName(keyValuePair.Key);
            WriteMetadataValue(writer, keyValuePair.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteMetadataArray(Utf8JsonWriter writer, MetadataArray array)
    {
        writer.WriteStartArray();
        for (var i = 0; i < array.Count; i++)
        {
            WriteMetadataValue(writer, array[i]);
        }

        writer.WriteEndArray();
    }

    private static void WriteMetadataValue(Utf8JsonWriter writer, MetadataValue value)
    {
        switch (value.Kind)
        {
            case MetadataKind.Null:
                writer.WriteNullValue();
                break;
            case MetadataKind.Boolean:
                value.TryGetBoolean(out var boolValue);
                writer.WriteBooleanValue(boolValue);
                break;
            case MetadataKind.Int64:
                value.TryGetInt64(out var int64Value);
                writer.WriteNumberValue(int64Value);
                break;
            case MetadataKind.Double:
                value.TryGetDouble(out var doubleValue);
                writer.WriteNumberValue(doubleValue);
                break;
            case MetadataKind.String:
                value.TryGetString(out var stringValue);
                writer.WriteStringValue(stringValue);
                break;
            case MetadataKind.Array:
                value.TryGetArray(out var arrayValue);
                WriteMetadataArray(writer, arrayValue);
                break;
            case MetadataKind.Object:
                value.TryGetObject(out var objectValue);
                WriteMetadataObject(writer, objectValue);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    private static string? GetStringAttribute(MetadataObject? attributes, string attributeName)
    {
        if (attributes is null || !attributes.Value.TryGetValue(attributeName, out var metadataValue))
        {
            return null;
        }

        if (metadataValue.TryGetString(out var stringValue))
        {
            return stringValue;
        }

        if (metadataValue.TryGetBoolean(out var boolValue))
        {
            return boolValue ? "true" : "false";
        }

        if (metadataValue.TryGetInt64(out var int64Value))
        {
            return int64Value.ToString(CultureInfo.InvariantCulture);
        }

        if (metadataValue.TryGetDouble(out var doubleValue))
        {
            return doubleValue.ToString(CultureInfo.InvariantCulture);
        }

        return null;
    }

    private static DateTimeOffset? GetDateTimeOffsetAttribute(MetadataObject? attributes, string attributeName)
    {
        var stringValue = GetStringAttribute(attributes, attributeName);
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
                stringValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed
            ))
        {
            throw new ArgumentException(
                $"CloudEvent attribute '{attributeName}' has an invalid RFC 3339 timestamp value.",
                attributeName
            );
        }

        return parsed;
    }

    private static void ValidateSourceUriReference(string source)
    {
        if (!Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out _))
        {
            throw new ArgumentException(
                "CloudEvent attribute 'source' must be a valid URI-reference.",
                nameof(source)
            );
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
            throw new ArgumentException(
                "CloudEvent attribute 'dataschema' must be an absolute URI.",
                nameof(dataSchema)
            );
        }
    }

    private readonly struct ResolvedAttributes
    {
        public ResolvedAttributes(
            string type,
            string source,
            string id,
            string? subject,
            string? dataSchema,
            DateTimeOffset time
        )
        {
            Type = type;
            Source = source;
            Id = id;
            Subject = subject;
            DataSchema = dataSchema;
            Time = time;
        }

        public string Type { get; }
        public string Source { get; }
        public string Id { get; }
        public string? Subject { get; }
        public string? DataSchema { get; }
        public DateTimeOffset Time { get; }
    }
}
