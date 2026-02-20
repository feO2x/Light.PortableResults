using System;
using System.Buffers;
using System.Globalization;
using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Provides extension methods to serialize <see cref="Result" /> and <see cref="Result{T}" /> values as CloudEvents JSON envelopes.
/// </summary>
public static class CloudEventsResultExtensions
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
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        using var bufferWriter = new PooledByteBufferWriter();
        using var writer = new Utf8JsonWriter(bufferWriter);
        result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
        writer.Flush();
        return bufferWriter.ToArray();
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
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var resolvedOptions = options ?? LightResultsCloudEventsWriteOptions.Default;
        var envelope = result.ToCloudEventEnvelopeForWriting(
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            resolvedOptions
        );

        JsonSerializer.Serialize(writer, envelope, resolvedOptions.SerializerOptions);
    }

    /// <summary>
    /// Creates a non-generic <see cref="CloudEventEnvelopeForWriting" /> with resolved attributes and frozen write options.
    /// </summary>
    public static CloudEventEnvelopeForWriting ToCloudEventEnvelopeForWriting(
        this Result result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        var resolvedOptions = options ?? LightResultsCloudEventsWriteOptions.Default;
        var convertedAttributes =
            ConvertMetadataToCloudEventAttributes(result.Metadata, resolvedOptions.ConversionService);
        var resolvedAttributes = ResolveAttributes(
            result.IsValid,
            convertedAttributes,
            ResolveOptionalString(successType, resolvedOptions.SuccessType),
            ResolveOptionalString(failureType, resolvedOptions.FailureType),
            ResolveOptionalString(id, resolvedOptions.IdResolver?.Invoke()),
            source,
            subject ?? resolvedOptions.Subject,
            dataschema ?? resolvedOptions.DataSchema,
            time ?? resolvedOptions.Time,
            resolvedOptions.Source
        );

        return new CloudEventEnvelopeForWriting(
            resolvedAttributes.Type,
            resolvedAttributes.Source,
            resolvedAttributes.Id,
            result,
            new ResolvedCloudEventWriteOptions(resolvedOptions.MetadataSerializationMode),
            resolvedAttributes.Subject,
            resolvedAttributes.Time,
            CloudEventConstants.JsonContentType,
            resolvedAttributes.DataSchema,
            convertedAttributes
        );
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
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        using var bufferWriter = new PooledByteBufferWriter();
        using var writer = new Utf8JsonWriter(bufferWriter);
        result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
        writer.Flush();
        return bufferWriter.ToArray();
    }

    /// <summary>
    /// Writes a generic <see cref="Result{T}" /> as a CloudEvents JSON envelope to the specified buffer writer.
    /// </summary>
    public static void WriteCloudEvent<T>(
        this Result<T> result,
        IBufferWriter<byte> bufferWriter,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        if (bufferWriter is null)
        {
            throw new ArgumentNullException(nameof(bufferWriter));
        }

        using var writer = new Utf8JsonWriter(bufferWriter);
        result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
        writer.Flush();
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
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var resolvedOptions = options ?? LightResultsCloudEventsWriteOptions.Default;
        var envelope = result.ToCloudEventEnvelopeForWriting(
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            resolvedOptions
        );

        JsonSerializer.Serialize(writer, envelope, resolvedOptions.SerializerOptions);
    }

    /// <summary>
    /// Creates a generic <see cref="CloudEventEnvelopeForWriting{T}" /> with resolved attributes and frozen write options.
    /// </summary>
    public static CloudEventEnvelopeForWriting<T> ToCloudEventEnvelopeForWriting<T>(
        this Result<T> result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        LightResultsCloudEventsWriteOptions? options = null
    )
    {
        var resolvedOptions = options ?? LightResultsCloudEventsWriteOptions.Default;
        var convertedAttributes =
            ConvertMetadataToCloudEventAttributes(result.Metadata, resolvedOptions.ConversionService);
        var resolvedAttributes = ResolveAttributes(
            result.IsValid,
            convertedAttributes,
            ResolveOptionalString(successType, resolvedOptions.SuccessType),
            ResolveOptionalString(failureType, resolvedOptions.FailureType),
            ResolveOptionalString(id, resolvedOptions.IdResolver?.Invoke()),
            source,
            subject ?? resolvedOptions.Subject,
            dataschema ?? resolvedOptions.DataSchema,
            time ?? resolvedOptions.Time,
            resolvedOptions.Source
        );

        return new CloudEventEnvelopeForWriting<T>(
            resolvedAttributes.Type,
            resolvedAttributes.Source,
            resolvedAttributes.Id,
            result,
            new ResolvedCloudEventWriteOptions(resolvedOptions.MetadataSerializationMode),
            resolvedAttributes.Subject,
            resolvedAttributes.Time,
            CloudEventConstants.JsonContentType,
            resolvedAttributes.DataSchema,
            convertedAttributes
        );
    }

    private static string? ResolveOptionalString(string? primaryValue, string? fallbackValue)
    {
        return !string.IsNullOrWhiteSpace(primaryValue) ? primaryValue : fallbackValue;
    }

    private static MetadataObject? ConvertMetadataToCloudEventAttributes(
        MetadataObject? metadata,
        ICloudEventsAttributeConversionService conversionService
    )
    {
        if (metadata is null ||
            !metadata.Value.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute))
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

        string resolvedId;
        if (!string.IsNullOrWhiteSpace(id))
        {
            resolvedId = id!;
        }
        else
        {
            var idFromAttributes = GetStringAttribute(convertedAttributes, "id");
            resolvedId = !string.IsNullOrWhiteSpace(idFromAttributes) ?
                idFromAttributes! :
                Guid.NewGuid().ToString();
        }

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

        ValidateSourceUriReference(resolvedSource!);
        ValidateDataSchema(resolvedDataSchema);

        return new ResolvedAttributes(
            resolvedType!,
            resolvedSource!,
            resolvedId,
            resolvedSubject,
            resolvedDataSchema,
            resolvedTime
        );
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
