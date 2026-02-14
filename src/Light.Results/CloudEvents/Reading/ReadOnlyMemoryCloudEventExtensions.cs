using System;
using System.Text.Json;
using Light.Results.CloudEvents.Reading.Json;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using MetadataJsonReader = Light.Results.SharedJsonSerialization.MetadataJsonReader;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Provides extensions to deserialize Light.Results values from CloudEvents JSON envelopes in UTF-8 byte buffers.
/// </summary>
public static class ReadOnlyMemoryCloudEventExtensions
{
    /// <summary>
    /// Reads a non-generic <see cref="Result" /> from a CloudEvents JSON envelope.
    /// </summary>
    public static Result ReadResult(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var envelope = cloudEvent.ReadResultWithCloudEventEnvelope(readOptions);
        return MergeEnvelopeMetadataIfNeeded(envelope.Data, envelope.ExtensionAttributes, readOptions);
    }

    /// <summary>
    /// Reads a generic <see cref="Result{T}" /> from a CloudEvents JSON envelope.
    /// </summary>
    public static Result<T> ReadResult<T>(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var envelope = cloudEvent.ReadResultWithCloudEventEnvelope<T>(readOptions);
        return MergeEnvelopeMetadataIfNeeded(envelope.Data, envelope.ExtensionAttributes, readOptions);
    }

    /// <summary>
    /// Reads a non-generic <see cref="CloudEventEnvelope" /> from a CloudEvents JSON envelope.
    /// </summary>
    public static CloudEventEnvelope ReadResultWithCloudEventEnvelope(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var parsedEnvelope = ParseEnvelope(cloudEvent);
        var isFailure = DetermineIsFailure(parsedEnvelope, readOptions);

        var result = ParseResultPayload(parsedEnvelope, isFailure, readOptions);

        return new CloudEventEnvelope(
            parsedEnvelope.Type,
            parsedEnvelope.Source,
            parsedEnvelope.Id,
            result,
            parsedEnvelope.Subject,
            parsedEnvelope.Time,
            parsedEnvelope.DataContentType,
            parsedEnvelope.DataSchema,
            parsedEnvelope.ExtensionAttributes
        );
    }

    /// <summary>
    /// Reads a generic <see cref="CloudEventEnvelope{T}" /> from a CloudEvents JSON envelope.
    /// </summary>
    public static CloudEventEnvelope<T> ReadResultWithCloudEventEnvelope<T>(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var parsedEnvelope = ParseEnvelope(cloudEvent);
        var isFailure = DetermineIsFailure(parsedEnvelope, readOptions);

        var result = ParseGenericResultPayload<T>(parsedEnvelope, isFailure, readOptions);

        return new CloudEventEnvelope<T>(
            parsedEnvelope.Type,
            parsedEnvelope.Source,
            parsedEnvelope.Id,
            result,
            parsedEnvelope.Subject,
            parsedEnvelope.Time,
            parsedEnvelope.DataContentType,
            parsedEnvelope.DataSchema,
            parsedEnvelope.ExtensionAttributes
        );
    }

    private static ParsedEnvelope ParseEnvelope(ReadOnlyMemory<byte> cloudEvent)
    {
        var reader = new Utf8JsonReader(cloudEvent.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
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

        byte[]? dataBytes = null;
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
                    using var document = JsonDocument.ParseValue(ref reader);
                    dataBytes = JsonSerializer.SerializeToUtf8Bytes(document.RootElement);
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

        return new ParsedEnvelope(
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
            dataBytes
        );
    }

    private static Result ParseResultPayload(
        ParsedEnvelope parsedEnvelope,
        bool isFailure,
        LightResultsCloudEventReadOptions options
    )
    {
        if (!parsedEnvelope.HasData || parsedEnvelope.IsDataNull)
        {
            if (isFailure)
            {
                throw new JsonException(
                    "CloudEvent failure payloads for non-generic Result must contain non-null data."
                );
            }

            return Result.Ok();
        }

        var dataBytes = parsedEnvelope.DataBytes!;
        var dataReader = new Utf8JsonReader(dataBytes, isFinalBlock: true, state: default);
        if (!dataReader.Read())
        {
            throw new JsonException("CloudEvent data payload is empty.");
        }

        if (isFailure)
        {
            var failurePayload = CloudEventDataJsonReader.ReadFailurePayload(ref dataReader);
            return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var successPayload = ResultJsonReader.ReadSuccessPayload(ref dataReader);
        var metadata = successPayload.Metadata;
        if (metadata is not null)
        {
            metadata = MetadataValueAnnotationHelper.WithAnnotation(
                metadata.Value,
                MetadataValueAnnotation.SerializeInCloudEventData
            );
        }

        return Result.Ok(metadata);
    }

    private static Result<T> ParseGenericResultPayload<T>(
        ParsedEnvelope parsedEnvelope,
        bool isFailure,
        LightResultsCloudEventReadOptions options
    )
    {
        if (!parsedEnvelope.HasData || parsedEnvelope.IsDataNull)
        {
            throw new JsonException("CloudEvent payloads for Result<T> must contain non-null data.");
        }

        var dataBytes = parsedEnvelope.DataBytes!;
        var dataReader = new Utf8JsonReader(dataBytes, isFinalBlock: true, state: default);
        if (!dataReader.Read())
        {
            throw new JsonException("CloudEvent data payload is empty.");
        }

        if (isFailure)
        {
            var failurePayload = CloudEventDataJsonReader.ReadFailurePayload(ref dataReader);
            return Result<T>.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var normalizedPreference = options.PreferSuccessPayload == PreferSuccessPayload.BareValue ||
                                   options.PreferSuccessPayload == PreferSuccessPayload.WrappedValue ?
            options.PreferSuccessPayload :
            PreferSuccessPayload.Auto;

        if (normalizedPreference == PreferSuccessPayload.BareValue)
        {
            var payload = ResultJsonReader.ReadBareSuccessPayload<T>(ref dataReader, options.SerializerOptions);
            return CreateSuccessfulGenericResult(payload.Value, metadata: null);
        }

        if (normalizedPreference == PreferSuccessPayload.WrappedValue)
        {
            var payload = ResultJsonReader.ReadWrappedSuccessPayload<T>(ref dataReader, options.SerializerOptions);
            var metadata = payload.Metadata;
            if (metadata is not null)
            {
                metadata = MetadataValueAnnotationHelper.WithAnnotation(
                    metadata.Value,
                    MetadataValueAnnotation.SerializeInCloudEventData
                );
            }

            return CreateSuccessfulGenericResult(payload.Value, metadata);
        }

        var autoPayload = ResultJsonReader.ReadAutoSuccessPayload<T>(ref dataReader, options.SerializerOptions);
        var autoMetadata = autoPayload.Metadata;
        if (autoMetadata is not null)
        {
            autoMetadata = MetadataValueAnnotationHelper.WithAnnotation(
                autoMetadata.Value,
                MetadataValueAnnotation.SerializeInCloudEventData
            );
        }

        return CreateSuccessfulGenericResult(autoPayload.Value, autoMetadata);
    }

    private static bool DetermineIsFailure(ParsedEnvelope parsedEnvelope, LightResultsCloudEventReadOptions options)
    {
        if (parsedEnvelope.ExtensionAttributes is { } extensionAttributes &&
            extensionAttributes.TryGetValue(
                CloudEventConstants.LightResultsOutcomeAttributeName,
                out var outcomeMetadata
            ))
        {
            if (!outcomeMetadata.TryGetString(out var outcomeValue) || string.IsNullOrWhiteSpace(outcomeValue))
            {
                throw new JsonException(
                    $"CloudEvent extension '{CloudEventConstants.LightResultsOutcomeAttributeName}' must be either 'success' or 'failure'."
                );
            }

            if (string.Equals(outcomeValue, "success", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(outcomeValue, "failure", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            throw new JsonException(
                $"CloudEvent extension '{CloudEventConstants.LightResultsOutcomeAttributeName}' must be either 'success' or 'failure'."
            );
        }

        if (options.IsFailureType is not null)
        {
            return options.IsFailureType(parsedEnvelope.Type);
        }

        throw new InvalidOperationException(
            "CloudEvent outcome could not be classified. Provide lroutcome or configure IsFailureType."
        );
    }

    private static TResult MergeEnvelopeMetadataIfNeeded<TResult>(
        TResult result,
        MetadataObject? extensionAttributes,
        LightResultsCloudEventReadOptions options
    )
        where TResult : struct, ICanReplaceMetadata<TResult>
    {
        if (options.ParsingService is null || extensionAttributes is null)
        {
            return result;
        }

        var extensionMetadata = options.ParsingService.ReadExtensionMetadata(
            FilterSpecialExtensionAttributes(extensionAttributes.Value)
        );

        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(
            extensionMetadata,
            result.Metadata,
            options.MergeStrategy
        );

        return mergedMetadata is null ? result : result.ReplaceMetadata(mergedMetadata);
    }

    private static MetadataObject FilterSpecialExtensionAttributes(MetadataObject extensionAttributes)
    {
        if (!extensionAttributes.ContainsKey(CloudEventConstants.LightResultsOutcomeAttributeName))
        {
            return extensionAttributes;
        }

        using var builder = MetadataObjectBuilder.Create(extensionAttributes.Count);
        foreach (var keyValuePair in extensionAttributes)
        {
            if (string.Equals(
                    keyValuePair.Key,
                    CloudEventConstants.LightResultsOutcomeAttributeName,
                    StringComparison.Ordinal
                ))
            {
                continue;
            }

            builder.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return builder.Count == 0 ? MetadataObject.Empty : builder.Build();
    }

    private static Result<T> CreateSuccessfulGenericResult<T>(T value, MetadataObject? metadata)
    {
        try
        {
            return Result<T>.Ok(value, metadata);
        }
        catch (ArgumentNullException argumentNullException)
        {
            throw new JsonException("Result value cannot be null.", argumentNullException);
        }
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
        metadataKind == MetadataKind.Null ||
        metadataKind == MetadataKind.Boolean ||
        metadataKind == MetadataKind.Int64 ||
        metadataKind == MetadataKind.Double ||
        metadataKind == MetadataKind.String;

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

    private readonly struct ParsedEnvelope
    {
        public ParsedEnvelope(
            string type,
            string source,
            string id,
            string? subject,
            DateTimeOffset? time,
            string? dataContentType,
            string? dataSchema,
            MetadataObject? extensionAttributes,
            bool hasData,
            bool isDataNull,
            byte[]? dataBytes
        )
        {
            Type = type;
            Source = source;
            Id = id;
            Subject = subject;
            Time = time;
            DataContentType = dataContentType;
            DataSchema = dataSchema;
            ExtensionAttributes = extensionAttributes;
            HasData = hasData;
            IsDataNull = isDataNull;
            DataBytes = dataBytes;
        }

        public string Type { get; }
        public string Source { get; }
        public string Id { get; }
        public string? Subject { get; }
        public DateTimeOffset? Time { get; }
        public string? DataContentType { get; }
        public string? DataSchema { get; }
        public MetadataObject? ExtensionAttributes { get; }
        public bool HasData { get; }
        public bool IsDataNull { get; }
        public byte[]? DataBytes { get; }
    }
}
