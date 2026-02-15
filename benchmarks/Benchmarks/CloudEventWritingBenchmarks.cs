using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using Light.Results;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class CloudEventWritingBenchmarks
{
    private Result _nonGenericSuccessResult;
    private Result _nonGenericFailureResult;
    private Result<ContactDto> _genericSuccessResult;
    private Result<ContactDto> _genericFailureResult;
    private Result<ContactDto> _genericSuccessWithMetadataResult;
    private LightResultsCloudEventWriteOptions _options = null!;

    [GlobalSetup]
    public void Setup()
    {
        var serializerOptions = Light.Results.CloudEvents.Reading.Module.CreateDefaultSerializerOptions();
        serializerOptions.TypeInfoResolverChain.Add(CloudEventWritingBenchmarksJsonContext.Default);

        _options = new LightResultsCloudEventWriteOptions
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
        metadataBuilder.Add("correlationId", MetadataValue.FromString("corr-123", MetadataValueAnnotation.SerializeInCloudEventData));
        metadataBuilder.Add("traceid", MetadataValue.FromString("trace-456", MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute));
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
    public byte[] ToCloudEvent_NonGenericSuccess()
    {
        return _nonGenericSuccessResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-1",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvent_NonGenericFailure()
    {
        return _nonGenericFailureResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-2",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvent_GenericSuccess()
    {
        return _genericSuccessResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-3",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvent_GenericFailure()
    {
        return _genericFailureResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-4",
            options: _options
        );
    }

    [Benchmark]
    public byte[] ToCloudEvent_GenericSuccessWithMetadata()
    {
        return _genericSuccessWithMetadataResult.ToCloudEvent(
            successType: "com.example.success",
            failureType: "com.example.failure",
            id: "event-5",
            options: _options
        );
    }

    public sealed class ContactDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}

[JsonSerializable(typeof(CloudEventWritingBenchmarks.ContactDto))]
internal partial class CloudEventWritingBenchmarksJsonContext : JsonSerializerContext;
