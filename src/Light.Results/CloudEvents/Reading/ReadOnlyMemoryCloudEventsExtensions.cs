using System;
using System.Text.Json;
using Light.Results.CloudEvents.Reading.Json;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Provides extensions to deserialize Light.Results values from CloudEvents JSON envelopes in UTF-8 byte buffers.
/// </summary>
public static class ReadOnlyMemoryCloudEventsExtensions
{
    /// <summary>
    /// Reads a non-generic <see cref="Result" /> from a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <returns>The deserialized <see cref="Result" /> created from the CloudEvents data section.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvents envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outcome cannot be determined because neither the lroutcome extension nor
    /// <see cref="LightResultsCloudEventsReadOptions.IsFailureType" /> is provided.
    /// </exception>
    public static Result ReadResult(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventsReadOptions? options = null
    )
    {
        options ??= LightResultsCloudEventsReadOptions.Default;
        var envelope = cloudEvent.ReadResultWithCloudEventsEnvelope(options);
        return MergeEnvelopeMetadataIfNeeded(envelope.Data, envelope.ExtensionAttributes, options);
    }

    /// <summary>
    /// Reads a generic <see cref="Result{T}" /> from a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <typeparam name="T">The type of the success value stored in the <see cref="Result{T}" />.</typeparam>
    /// <returns>The deserialized <see cref="Result{T}" /> created from the CloudEvents data section.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvents envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outcome cannot be determined because neither the lroutcome extension nor
    /// <see cref="LightResultsCloudEventsReadOptions.IsFailureType" /> is provided.
    /// </exception>
    public static Result<T> ReadResult<T>(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventsReadOptions? options = null
    )
    {
        options ??= LightResultsCloudEventsReadOptions.Default;
        var envelope = cloudEvent.ReadResultWithCloudEventsEnvelope<T>(options);
        return MergeEnvelopeMetadataIfNeeded(envelope.Data, envelope.ExtensionAttributes, options);
    }

    /// <summary>
    /// Reads a non-generic <see cref="CloudEventsEnvelope" /> from a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <returns>The parsed <see cref="CloudEventsEnvelope" /> containing the original CloudEvents attributes and a deserialized <see cref="Result" />.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvents envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outcome cannot be determined because neither the lroutcome extension nor
    /// <see cref="LightResultsCloudEventsReadOptions.IsFailureType" /> is provided.
    /// </exception>
    public static CloudEventsEnvelope ReadResultWithCloudEventsEnvelope(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventsReadOptions? options = null
    )
    {
        options ??= LightResultsCloudEventsReadOptions.Default;
        var parsedEnvelope = JsonSerializer.Deserialize<CloudEventsEnvelopePayload>(
            cloudEvent.Span,
            options.SerializerOptions
        );
        var isFailure = DetermineIsFailure(parsedEnvelope, options);

        var dataSegment = parsedEnvelope is { HasData: true, IsDataNull: false } ?
            cloudEvent.Slice(parsedEnvelope.DataStart, parsedEnvelope.DataLength) :
            ReadOnlyMemory<byte>.Empty;

        var result = ParseResultPayload(dataSegment, isFailure, options);

        return new CloudEventsEnvelope(
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
    /// Reads a generic <see cref="CloudEventsEnvelope{T}" /> from a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="cloudEvent">The serialized CloudEvents JSON envelope in a UTF-8 byte buffer.</param>
    /// <param name="options">Optional settings that control parsing behavior, metadata merging, and failure detection.</param>
    /// <typeparam name="T">The type of the success value stored in the <see cref="Result{T}" />.</typeparam>
    /// <returns>The parsed <see cref="CloudEventsEnvelope{T}" /> containing the original CloudEvents attributes and a deserialized <see cref="Result{T}" />.</returns>
    /// <exception cref="JsonException">Thrown when the CloudEvents envelope or data payload is malformed or violates the Light.Results expectations.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the outcome cannot be determined because neither the lroutcome extension nor
    /// <see cref="LightResultsCloudEventsReadOptions.IsFailureType" /> is provided.
    /// </exception>
    public static CloudEventsEnvelope<T> ReadResultWithCloudEventsEnvelope<T>(
        this ReadOnlyMemory<byte> cloudEvent,
        LightResultsCloudEventsReadOptions? options = null
    )
    {
        options ??= LightResultsCloudEventsReadOptions.Default;
        var parsedEnvelope = JsonSerializer.Deserialize<CloudEventsEnvelopePayload>(
            cloudEvent.Span,
            options.SerializerOptions
        );
        var isFailure = DetermineIsFailure(parsedEnvelope, options);

        var dataSegment = parsedEnvelope is { HasData: true, IsDataNull: false } ?
            cloudEvent.Slice(parsedEnvelope.DataStart, parsedEnvelope.DataLength) :
            ReadOnlyMemory<byte>.Empty;

        var result = ParseGenericResultPayload<T>(dataSegment, parsedEnvelope.HasData, isFailure, options);

        return new CloudEventsEnvelope<T>(
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
        ReadOnlyMemory<byte> dataSegment,
        bool isFailure,
        LightResultsCloudEventsReadOptions options
    )
    {
        if (dataSegment.IsEmpty)
        {
            if (isFailure)
            {
                throw new JsonException(
                    "CloudEvents failure payloads for non-generic Result must contain non-null data."
                );
            }

            return Result.Ok();
        }

        if (isFailure)
        {
            var failurePayload = JsonSerializer.Deserialize<CloudEventsFailurePayload>(
                dataSegment.Span,
                options.SerializerOptions
            );
            return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var successPayload = JsonSerializer.Deserialize<CloudEventsSuccessPayload>(
            dataSegment.Span,
            options.SerializerOptions
        );
        var metadata = successPayload.Metadata;
        if (metadata is not null)
        {
            metadata = MetadataValueAnnotationHelper.WithAnnotation(
                metadata.Value,
                MetadataValueAnnotation.SerializeInCloudEventsData
            );
        }

        return Result.Ok(metadata);
    }

    private static Result<T> ParseGenericResultPayload<T>(
        ReadOnlyMemory<byte> dataSegment,
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local - this is not a precondition check.
        // This will shortcut the deserialization of the cloud event.
        bool hasData,
        bool isFailure,
        LightResultsCloudEventsReadOptions options
    )
    {
        if (dataSegment.IsEmpty || !hasData)
        {
            throw new JsonException("CloudEvents payloads for Result<T> must contain non-null data.");
        }

        if (isFailure)
        {
            var failurePayload = JsonSerializer.Deserialize<CloudEventsFailurePayload>(
                dataSegment.Span,
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
            var payload = JsonSerializer.Deserialize<CloudEventsBareSuccessPayload<T>>(
                dataSegment.Span,
                options.SerializerOptions
            );
            return CreateSuccessfulGenericResult(payload.Value, metadata: null);
        }

        if (normalizedPreference == PreferSuccessPayload.WrappedValue)
        {
            var payload = JsonSerializer.Deserialize<CloudEventsWrappedSuccessPayload<T>>(
                dataSegment.Span,
                options.SerializerOptions
            );
            var metadata = payload.Metadata;
            if (metadata is not null)
            {
                metadata = MetadataValueAnnotationHelper.WithAnnotation(
                    metadata.Value,
                    MetadataValueAnnotation.SerializeInCloudEventsData
                );
            }

            return CreateSuccessfulGenericResult(payload.Value, metadata);
        }

        var autoPayload = JsonSerializer.Deserialize<CloudEventsAutoSuccessPayload<T>>(
            dataSegment.Span,
            options.SerializerOptions
        );
        var autoMetadata = autoPayload.Metadata;
        if (autoMetadata is not null)
        {
            autoMetadata = MetadataValueAnnotationHelper.WithAnnotation(
                autoMetadata.Value,
                MetadataValueAnnotation.SerializeInCloudEventsData
            );
        }

        return CreateSuccessfulGenericResult(autoPayload.Value, autoMetadata);
    }

    private static bool DetermineIsFailure(
        CloudEventsEnvelopePayload parsedEnvelope,
        LightResultsCloudEventsReadOptions options
    )
    {
        if (parsedEnvelope.ExtensionAttributes is { } extensionAttributes &&
            extensionAttributes.TryGetValue(
                CloudEventsConstants.LightResultsOutcomeAttributeName,
                out var outcomeMetadata
            ))
        {
            if (!outcomeMetadata.TryGetString(out var outcomeValue) || string.IsNullOrWhiteSpace(outcomeValue))
            {
                throw new JsonException(
                    $"CloudEvents extension '{CloudEventsConstants.LightResultsOutcomeAttributeName}' must be either 'success' or 'failure'."
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
                $"CloudEvents extension '{CloudEventsConstants.LightResultsOutcomeAttributeName}' must be either 'success' or 'failure'."
            );
        }

        if (options.IsFailureType is not null)
        {
            return options.IsFailureType(parsedEnvelope.Type);
        }

        throw new InvalidOperationException(
            "CloudEvents outcome could not be classified. Provide lroutcome or configure IsFailureType."
        );
    }

    private static TResult MergeEnvelopeMetadataIfNeeded<TResult>(
        TResult result,
        MetadataObject? extensionAttributes,
        LightResultsCloudEventsReadOptions options
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
        if (!extensionAttributes.ContainsKey(CloudEventsConstants.LightResultsOutcomeAttributeName))
        {
            return extensionAttributes;
        }

        using var builder = MetadataObjectBuilder.Create(extensionAttributes.Count);
        foreach (var keyValuePair in extensionAttributes)
        {
            if (string.Equals(
                    keyValuePair.Key,
                    CloudEventsConstants.LightResultsOutcomeAttributeName,
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
