using System;
using System.Globalization;
using System.Text.Json;
using Light.PortableResults.Buffers;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.CloudEvents.Writing;

/// <summary>
/// Provides extension methods to serialize <see cref="Result" /> and <see cref="Result{T}" /> values as CloudEvents JSON envelopes.
/// </summary>
public static class CloudEventsResultExtensions
{
    /// <summary>
    /// Serializes a non-generic <see cref="Result" /> to a CloudEvents JSON envelope.
    /// </summary>
    /// <param name="result">The result to serialize.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>The CloudEvents payload as a new byte array.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static byte[] ToCloudEvent(
        this Result result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        using var pooledArray = result.ToCloudEventPooled(
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            options
        );
        return pooledArray.Memory.ToArray();
    }

    /// <summary>
    /// Serializes a non-generic <see cref="Result" /> into a rented byte array that contains the CloudEvents JSON envelope.
    /// </summary>
    /// <param name="result">The result instance to serialize.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>An <see cref="IRentedArray" /> whose lifetime is owned by the caller.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static IRentedArray ToCloudEventPooled(
        this Result result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        options ??= PortableResultsCloudEventsWriteOptions.Default;
        var bufferWriter = new RentedArrayBufferWriter(options.ArrayPool, options.PooledArrayInitialCapacity);
        try
        {
            using var writer = new Utf8JsonWriter(bufferWriter);
            result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
            writer.Flush();
            return bufferWriter.FinishWriting();
        }
        catch (Exception)
        {
            bufferWriter.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Writes a non-generic <see cref="Result" /> as a CloudEvents JSON envelope using the provided <see cref="Utf8JsonWriter" />.
    /// </summary>
    /// <param name="result">The result to serialize.</param>
    /// <param name="writer">The JSON writer receiving the CloudEvents payload.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
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
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        options ??= PortableResultsCloudEventsWriteOptions.Default;
        var envelope = result.ToCloudEventsEnvelopeForWriting(
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            options
        );

        JsonSerializer.Serialize(writer, envelope, options.SerializerOptions);
    }

    /// <summary>
    /// Creates a non-generic <see cref="CloudEventsEnvelopeForWriting" /> with resolved attributes and frozen write options that can be serialized later.
    /// </summary>
    /// <param name="result">The result that provides data and metadata for the cloud event.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>A fully resolved CloudEvents envelope that can be serialized without additional allocations.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static CloudEventsEnvelopeForWriting ToCloudEventsEnvelopeForWriting(
        this Result result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        options ??= PortableResultsCloudEventsWriteOptions.Default;
        var convertedAttributes =
            ConvertMetadataToCloudEventsAttributes(result.Metadata, options.ConversionService);
        var resolvedAttributes = ResolveAttributes(
            result.IsValid,
            convertedAttributes,
            ResolveOptionalString(successType, options.SuccessType),
            ResolveOptionalString(failureType, options.FailureType),
            ResolveOptionalString(id, options.IdResolver?.Invoke()),
            source,
            subject ?? options.Subject,
            dataschema ?? options.DataSchema,
            time ?? options.Time,
            options.Source
        );

        return new CloudEventsEnvelopeForWriting(
            resolvedAttributes.Type,
            resolvedAttributes.Source,
            resolvedAttributes.Id,
            result,
            new ResolvedCloudEventsWriteOptions(options.MetadataSerializationMode),
            resolvedAttributes.Subject,
            resolvedAttributes.Time,
            resolvedAttributes.DataSchema,
            convertedAttributes
        );
    }

    /// <summary>
    /// Serializes a generic <see cref="Result{T}" /> to a CloudEvents JSON envelope.
    /// </summary>
    /// <typeparam name="T">The value type carried by the result.</typeparam>
    /// <param name="result">The result to serialize.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>The CloudEvents payload as a new byte array.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static byte[] ToCloudEvent<T>(
        this Result<T> result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        using var pooledArray = result.ToCloudEventPooled(
            successType,
            failureType,
            id,
            source,
            subject,
            dataschema,
            time,
            options
        );
        return pooledArray.Memory.ToArray();
    }

    /// <summary>
    /// Serializes a generic <see cref="Result{T}" /> into a pooled byte buffer that contains the CloudEvents JSON envelope.
    /// </summary>
    /// <typeparam name="T">The value type carried by the result.</typeparam>
    /// <param name="result">The result to serialize.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">
    /// The optional CloudEvents <c>subject</c>. We highly recommend setting this value for routing and
    /// observability purposes.
    /// </param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>An <see cref="IRentedArray" /> whose lifetime is owned by the caller.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static IRentedArray ToCloudEventPooled<T>(
        this Result<T> result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        options ??= PortableResultsCloudEventsWriteOptions.Default;
        var bufferWriter = new RentedArrayBufferWriter(options.ArrayPool, options.PooledArrayInitialCapacity);
        try
        {
            using var writer = new Utf8JsonWriter(bufferWriter);
            result.WriteCloudEvent(writer, successType, failureType, id, source, subject, dataschema, time, options);
            writer.Flush();
            return bufferWriter.FinishWriting();
        }
        catch (Exception)
        {
            bufferWriter.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Writes a generic <see cref="Result{T}" /> as a CloudEvents JSON envelope using the provided <see cref="Utf8JsonWriter" />.
    /// </summary>
    /// <typeparam name="T">The value type carried by the result.</typeparam>
    /// <param name="result">The result to serialize.</param>
    /// <param name="writer">The JSON writer receiving the CloudEvents payload.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">The optional CloudEvents <c>subject</c>.</param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
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
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        var resolvedOptions = options ?? PortableResultsCloudEventsWriteOptions.Default;
        var envelope = result.ToCloudEventsEnvelopeForWriting(
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
    /// Creates a generic <see cref="CloudEventsEnvelopeForWriting{T}" /> with resolved attributes and frozen write options that can be serialized later.
    /// </summary>
    /// <typeparam name="T">The value type carried by the result.</typeparam>
    /// <param name="result">The result that provides data and metadata for the cloud event.</param>
    /// <param name="successType">Optional CloudEvents <c>type</c> to use when the result is valid.</param>
    /// <param name="failureType">Optional CloudEvents <c>type</c> to use when the result contains failures.</param>
    /// <param name="id">
    /// An explicit CloudEvents <c>id</c>; generated when omitted. We highly recommend setting this value for idempotent
    /// event processing. If you do not set this value, a new ID will be generated on each call.
    /// </param>
    /// <param name="source">The CloudEvents <c>source</c> URI reference to apply.</param>
    /// <param name="subject">The optional CloudEvents <c>subject</c>.</param>
    /// <param name="dataschema">The optional CloudEvents <c>dataschema</c> absolute URI.</param>
    /// <param name="time">The CloudEvents timestamp. Defaults to <see cref="DateTimeOffset.UtcNow" /> when not provided.</param>
    /// <param name="options">Customization options for serialization and metadata conversion.</param>
    /// <returns>A fully resolved CloudEvents envelope that can be serialized without additional allocations.</returns>
    /// <remarks>
    /// The required CloudEvents attributes <c>type</c> and <c>source</c> are resolved from the supplied arguments, the result metadata,
    /// or the configured defaults in <paramref name="options" />. An <see cref="InvalidOperationException" /> is thrown when
    /// neither path provides valid values. Consider explicitly setting <paramref name="id" /> and <paramref name="subject" />
    /// to keep downstream consumers idempotent and support routing and observability.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the CloudEvents <c>type</c> or <c>source</c> cannot be resolved.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" />, <paramref name="dataschema" />, or <paramref name="time" /> has an invalid format.</exception>
    public static CloudEventsEnvelopeForWriting<T> ToCloudEventsEnvelopeForWriting<T>(
        this Result<T> result,
        string? successType = null,
        string? failureType = null,
        string? id = null,
        string? source = null,
        string? subject = null,
        string? dataschema = null,
        DateTimeOffset? time = null,
        PortableResultsCloudEventsWriteOptions? options = null
    )
    {
        var resolvedOptions = options ?? PortableResultsCloudEventsWriteOptions.Default;
        var convertedAttributes =
            ConvertMetadataToCloudEventsAttributes(result.Metadata, resolvedOptions.ConversionService);
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

        return new CloudEventsEnvelopeForWriting<T>(
            resolvedAttributes.Type,
            resolvedAttributes.Source,
            resolvedAttributes.Id,
            result,
            new ResolvedCloudEventsWriteOptions(resolvedOptions.MetadataSerializationMode),
            resolvedAttributes.Subject,
            resolvedAttributes.Time,
            resolvedAttributes.DataSchema,
            convertedAttributes
        );
    }

    private static string? ResolveOptionalString(string? primaryValue, string? fallbackValue)
    {
        return !string.IsNullOrWhiteSpace(primaryValue) ? primaryValue : fallbackValue;
    }

    private static MetadataObject? ConvertMetadataToCloudEventsAttributes(
        MetadataObject? metadata,
        ICloudEventsAttributeConversionService conversionService
    )
    {
        if (metadata is null ||
            !metadata.Value.HasAnyValuesWithAnnotation(
                MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
            ))
        {
            return null;
        }


        using var builder = MetadataObjectBuilder.Create(metadata.Value.Count);
        foreach (var keyValuePair in metadata.Value)
        {
            if (!keyValuePair.Value.HasAnnotation(MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes))
            {
                continue;
            }

            var preparedAttribute = conversionService.PrepareCloudEventsAttribute(keyValuePair.Key, keyValuePair.Value);
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
                Ulid.NewUlid().ToGuid().ToString();
        }

        var resolvedSubject = subject ?? GetStringAttribute(convertedAttributes, "subject");
        var resolvedDataSchema = dataschema ?? GetStringAttribute(convertedAttributes, "dataschema");
        var resolvedTime = time ?? GetDateTimeOffsetAttribute(convertedAttributes, "time") ?? DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(resolvedType))
        {
            throw new InvalidOperationException("CloudEvents attribute 'type' could not be resolved.");
        }

        if (string.IsNullOrWhiteSpace(resolvedSource))
        {
            throw new InvalidOperationException("CloudEvents attribute 'source' could not be resolved.");
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
                $"CloudEvents attribute '{attributeName}' has an invalid RFC 3339 timestamp value.",
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
                "CloudEvents attribute 'source' must be a valid URI-reference.",
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
                "CloudEvents attribute 'dataschema' must be an absolute URI.",
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
