using System;
using System.Text.Json;
using Light.Results.CloudEvents.Reading.Json;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Provides extensions to deserialize Light.Results values from CloudEvents JSON envelopes in UTF-8 byte buffers.
/// </summary>
public static class ReadOnlyMemoryCloudEventExtensions
{
    /// <summary>
    /// Reads a non-generic <see cref="Result" /> from a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <returns>The deserialized <see cref="Result" /> created from the CloudEvent data section.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvent envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the outcome cannot be determined because neither the lroutcome extension nor <see cref="LightResultsCloudEventReadOptions.IsFailureType" /> is provided.</exception>
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
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <typeparam name="T">The type of the success value stored in the <see cref="Result{T}" />.</typeparam>
    /// <returns>The deserialized <see cref="Result{T}" /> created from the CloudEvent data section.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvent envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the outcome cannot be determined because neither the lroutcome extension nor <see cref="LightResultsCloudEventReadOptions.IsFailureType" /> is provided.</exception>
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
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <returns>The parsed <see cref="CloudEventEnvelope" /> containing the original CloudEvent attributes and a deserialized <see cref="Result" />.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvent envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the outcome cannot be determined because neither the lroutcome extension nor <see cref="LightResultsCloudEventReadOptions.IsFailureType" /> is provided.</exception>
    public static CloudEventEnvelope ReadResultWithCloudEventEnvelope(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var parsedEnvelope = JsonSerializer.Deserialize<CloudEventEnvelopePayload>(
            cloudEvent.Span,
            readOptions.SerializerOptions
        );
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
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <typeparam name="T">The type of the success value stored in the <see cref="Result{T}" />.</typeparam>
    /// <returns>The parsed <see cref="CloudEventEnvelope{T}" /> containing the original CloudEvent attributes and a deserialized <see cref="Result{T}" />.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvent envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the outcome cannot be determined because neither the lroutcome extension nor <see cref="LightResultsCloudEventReadOptions.IsFailureType" /> is provided.</exception>
    public static CloudEventEnvelope<T> ReadResultWithCloudEventEnvelope<T>(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventReadOptions? options = null
    )
    {
        var readOptions = options ?? LightResultsCloudEventReadOptions.Default;
        var parsedEnvelope = JsonSerializer.Deserialize<CloudEventEnvelopePayload>(
            cloudEvent.Span,
            readOptions.SerializerOptions
        );
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


    private static Result ParseResultPayload(
        CloudEventEnvelopePayload parsedEnvelope,
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

        if (isFailure)
        {
            var failurePayload = JsonSerializer.Deserialize<CloudEventFailurePayload>(
                dataBytes.AsSpan(),
                options.SerializerOptions
            );
            return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var successPayload = JsonSerializer.Deserialize<CloudEventSuccessPayload>(
            dataBytes.AsSpan(),
            options.SerializerOptions
        );
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
        CloudEventEnvelopePayload parsedEnvelope,
        bool isFailure,
        LightResultsCloudEventReadOptions options
    )
    {
        if (!parsedEnvelope.HasData || parsedEnvelope.IsDataNull)
        {
            throw new JsonException("CloudEvent payloads for Result<T> must contain non-null data.");
        }

        var dataBytes = parsedEnvelope.DataBytes!;

        if (isFailure)
        {
            var failurePayload = JsonSerializer.Deserialize<CloudEventFailurePayload>(
                dataBytes.AsSpan(),
                options.SerializerOptions
            );
            return Result<T>.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var normalizedPreference = options.PreferSuccessPayload == PreferSuccessPayload.BareValue ||
                                   options.PreferSuccessPayload == PreferSuccessPayload.WrappedValue ?
            options.PreferSuccessPayload :
            PreferSuccessPayload.Auto;

        if (normalizedPreference == PreferSuccessPayload.BareValue)
        {
            var payload = JsonSerializer.Deserialize<CloudEventBareSuccessPayload<T>>(
                dataBytes.AsSpan(),
                options.SerializerOptions
            );
            return CreateSuccessfulGenericResult(payload.Value, metadata: null);
        }

        if (normalizedPreference == PreferSuccessPayload.WrappedValue)
        {
            var payload = JsonSerializer.Deserialize<CloudEventWrappedSuccessPayload<T>>(
                dataBytes.AsSpan(),
                options.SerializerOptions
            );
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

        var autoPayload = JsonSerializer.Deserialize<CloudEventAutoSuccessPayload<T>>(
            dataBytes.AsSpan(),
            options.SerializerOptions
        );
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

    private static bool DetermineIsFailure(
        CloudEventEnvelopePayload parsedEnvelope,
        LightResultsCloudEventReadOptions options
    )
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
}
