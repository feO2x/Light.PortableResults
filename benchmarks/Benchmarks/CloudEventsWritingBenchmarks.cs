using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using Light.Results;
using Light.Results.CloudEvents;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
using Light.Results.SharedJsonSerialization.Writing;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class CloudEventsWritingBenchmarks
{
    private Result<ContactDto> _genericFailureResult;
    private Result<ContactDto> _genericSuccessResult;
    private Result<ContactDto> _genericSuccessWithMetadataResult;
    private Result _nonGenericFailureResult;
    private Result _nonGenericSuccessResult;
    private LightResultsCloudEventsWriteOptions _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();
        serializerOptions.TypeInfoResolverChain.Add(CloudEventsWritingBenchmarksJsonContext.Default);

        _options = new LightResultsCloudEventsWriteOptions
        {
            Source = "/benchmarks",
            SerializerOptions = serializerOptions
        };

        // Non-generic success result
        _nonGenericSuccessResult = Result.Ok();

        // Non-generic failure result
        _nonGenericFailureResult = Result.Fail(
            new Error { Message = "Validation failed", Code = "ValidationError", Category = ErrorCategory.Validation }
        );

        // Generic success result
        _genericSuccessResult = Result<ContactDto>.Ok(
            new ContactDto
            {
                Id = Guid.Parse("6B8A4DCA-779D-4F36-8274-487FE3E86B5A"),
                Name = "Contact A",
                Email = "contact@example.com"
            }
        );

        // Generic failure result
        _genericFailureResult = Result<ContactDto>.Fail(
            new Error { Message = "Contact not found", Code = "ContactNotFound", Category = ErrorCategory.NotFound }
        );

        // Generic success with metadata
        var metadataBuilder = MetadataObjectBuilder.Create();
        metadataBuilder.Add(
            "correlationId",
            MetadataValue.FromString("corr-123", MetadataValueAnnotation.SerializeInCloudEventsData)
        );
        metadataBuilder.Add(
            "traceid",
            MetadataValue.FromString("trace-456", MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes)
        );
        var metadata = metadataBuilder.Build();

        _genericSuccessWithMetadataResult = Result<ContactDto>.Ok(
            new ContactDto
            {
                Id = Guid.Parse("6B8A4DCA-779D-4F36-8274-487FE3E86B5A"),
                Name = "Contact A",
                Email = "contact@example.com"
            },
            metadata
        );
    }

    [Benchmark(Baseline = true)]
    public byte[] ToCloudEvents_NonGenericSuccess()
    {
        return _nonGenericSuccessResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-1",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_NonGenericSuccess_LegacyDirect()
    {
        return ToCloudEventsLegacyDirect(
            _nonGenericSuccessResult,
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-1"
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_NonGenericFailure()
    {
        return _nonGenericFailureResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-2",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_NonGenericFailure_LegacyDirect()
    {
        return ToCloudEventsLegacyDirect(
            _nonGenericFailureResult,
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-2"
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericSuccess()
    {
        return _genericSuccessResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-3",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericSuccess_LegacyDirect()
    {
        return ToCloudEventsLegacyDirect(
            _genericSuccessResult,
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-3"
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericFailure()
    {
        return _genericFailureResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-4",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericFailure_LegacyDirect()
    {
        return ToCloudEventsLegacyDirect(
            _genericFailureResult,
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-4"
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericSuccessWithMetadata()
    {
        return _genericSuccessWithMetadataResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-5",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvents_GenericSuccessWithMetadata_LegacyDirect()
    {
        return ToCloudEventsLegacyDirect(
            _genericSuccessWithMetadataResult,
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-5"
        );
    }

    private byte[] ToCloudEventsLegacyDirect(
        Result result,
        string successType,
        string failureType,
        string id
    )
    {
        var envelope = result.ToCloudEventsEnvelopeForWriting(
            successType: successType,
            failureType: failureType,
            id: id,
            options: _options
        );

        var bufferWriter = new ArrayBufferWriter<byte>(2048);
        using var writer = new Utf8JsonWriter(bufferWriter);
        WriteLegacyEnvelope(writer, envelope, _options.SerializerOptions);
        writer.Flush();
        return bufferWriter.WrittenSpan.ToArray();
    }

    private byte[] ToCloudEventsLegacyDirect<T>(
        Result<T> result,
        string successType,
        string failureType,
        string id
    )
    {
        var envelope = result.ToCloudEventsEnvelopeForWriting(
            successType: successType,
            failureType: failureType,
            id: id,
            options: _options
        );

        var bufferWriter = new ArrayBufferWriter<byte>(2048);
        using var writer = new Utf8JsonWriter(bufferWriter);
        WriteLegacyEnvelope(writer, envelope, _options.SerializerOptions);
        writer.Flush();
        return bufferWriter.WrittenSpan.ToArray();
    }

    private static void WriteLegacyEnvelope(
        Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting envelope,
        JsonSerializerOptions serializerOptions
    )
    {
        var metadataForData = SelectMetadataByAnnotation(
            envelope.Data.Metadata,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );
        var includeData = !envelope.Data.IsValid ||
                          (
                              envelope.ResolvedOptions.MetadataSerializationMode == MetadataSerializationMode.Always &&
                              metadataForData is not null
                          );

        writer.WriteStartObject();
        WriteLegacyEnvelopeAttributes(
            writer,
            envelope.Type,
            envelope.Source,
            envelope.Id,
            envelope.Subject,
            envelope.Time,
            envelope.DataSchema,
            includeData,
            envelope.Data.IsValid
        );
        WriteLegacyExtensionAttributes(writer, envelope.ExtensionAttributes);

        if (includeData)
        {
            writer.WritePropertyName("data");
            if (envelope.Data.IsValid && metadataForData is { } successMetadata)
            {
                writer.WriteStartObject();
                WriteLegacyMetadataPropertyAndValue(writer, successMetadata);
                writer.WriteEndObject();
            }
            else
            {
                WriteLegacyFailurePayload(writer, envelope.Data.Errors, metadataForData, serializerOptions);
            }
        }

        writer.WriteEndObject();
    }

    private static void WriteLegacyEnvelope<T>(
        Utf8JsonWriter writer,
        CloudEventsEnvelopeForWriting<T> envelope,
        JsonSerializerOptions serializerOptions
    )
    {
        var metadataForData = SelectMetadataByAnnotation(
            envelope.Data.Metadata,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );
        var includeWrappedSuccess = envelope.Data.IsValid &&
                                    envelope.ResolvedOptions.MetadataSerializationMode ==
                                    MetadataSerializationMode.Always &&
                                    metadataForData is not null;

        writer.WriteStartObject();
        WriteLegacyEnvelopeAttributes(
            writer,
            envelope.Type,
            envelope.Source,
            envelope.Id,
            envelope.Subject,
            envelope.Time,
            envelope.DataSchema,
            includeData: true,
            envelope.Data.IsValid
        );
        WriteLegacyExtensionAttributes(writer, envelope.ExtensionAttributes);

        writer.WritePropertyName("data");
        if (envelope.Data.IsValid)
        {
            if (includeWrappedSuccess && metadataForData is { } successMetadata)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteGenericValue(envelope.Data.Value, serializerOptions);
                WriteLegacyMetadataPropertyAndValue(writer, successMetadata);
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteGenericValue(envelope.Data.Value, serializerOptions);
            }
        }
        else
        {
            WriteLegacyFailurePayload(writer, envelope.Data.Errors, metadataForData, serializerOptions);
        }

        writer.WriteEndObject();
    }

    private static void WriteLegacyEnvelopeAttributes(
        Utf8JsonWriter writer,
        string type,
        string source,
        string id,
        string? subject,
        DateTimeOffset? time,
        string? dataSchema,
        bool includeData,
        bool isSuccess
    )
    {
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
            writer.WriteString("time", time.Value);
        }

        writer.WriteString(CloudEventsConstants.LightResultsOutcomeAttributeName, isSuccess ? "success" : "failure");
        if (includeData)
        {
            writer.WriteString("datacontenttype", CloudEventsConstants.JsonContentType);
        }
    }

    private static void WriteLegacyExtensionAttributes(Utf8JsonWriter writer, MetadataObject? extensionAttributes)
    {
        if (extensionAttributes is null)
        {
            return;
        }

        foreach (var keyValuePair in extensionAttributes.Value)
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

    private static void WriteLegacyFailurePayload(
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
            WriteLegacyMetadataPropertyAndValue(writer, metadata.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteLegacyMetadataPropertyAndValue(Utf8JsonWriter writer, MetadataObject metadata)
    {
        writer.WritePropertyName("metadata");
        writer.WriteStartObject();
        foreach (var keyValuePair in metadata)
        {
            writer.WritePropertyName(keyValuePair.Key);
            writer.WriteMetadataValue(keyValuePair.Value, MetadataValueAnnotation.SerializeInCloudEventsData);
        }

        writer.WriteEndObject();
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

    public sealed class ContactDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}

[JsonSerializable(typeof(CloudEventsWritingBenchmarks.ContactDto))]
internal partial class CloudEventsWritingBenchmarksJsonContext : JsonSerializerContext;
