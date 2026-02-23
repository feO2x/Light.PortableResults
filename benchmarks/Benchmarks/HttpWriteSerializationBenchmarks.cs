using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BenchmarkDotNet.Attributes;
using Light.Results;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;

namespace Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class HttpWriteSerializationBenchmarks
{
    private ArrayBufferWriter<byte> _buffer = null!;
    private Result<ContactDto> _genericFailure;
    private Result<ContactDto> _genericSuccess;
    private Result<ContactDto> _genericSuccessWithMetadata;
    private Result _nonGenericFailure;
    private Result _nonGenericSuccess;
    private ResolvedHttpWriteOptions _resolvedOptions;
    private JsonSerializerOptions _serializerOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _buffer = new ArrayBufferWriter<byte>(4096);

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                HttpWriteSerializationBenchmarksJsonContext.Default,
                new DefaultJsonTypeInfoResolver()
            )
        };
        _serializerOptions.AddDefaultLightResultsHttpWriteJsonConverters();

        _resolvedOptions = new LightResultsHttpWriteOptions().ToResolvedHttpWriteOptions();

        _nonGenericSuccess = Result.Ok();

        _nonGenericFailure = Result.Fail(
            new Error
            {
                Message = "Validation failed",
                Code = "ValidationError",
                Category = ErrorCategory.Validation,
                Target = "email"
            }
        );

        _genericSuccess = Result<ContactDto>.Ok(
            new ContactDto
            {
                Id = Guid.Parse("6B8A4DCA-779D-4F36-8274-487FE3E86B5A"),
                Name = "Contact A",
                Email = "contact@example.com"
            }
        );

        _genericFailure = Result<ContactDto>.Fail(
            new Error
            {
                Message = "Contact not found",
                Code = "ContactNotFound",
                Category = ErrorCategory.NotFound
            }
        );

        var metadata = MetadataObject.Create(
            ("correlationId",
             MetadataValue.FromString("corr-123", MetadataValueAnnotation.SerializeInHttpResponseBody)),
            ("traceId", MetadataValue.FromString("trace-456", MetadataValueAnnotation.SerializeInHttpHeader))
        );
        _genericSuccessWithMetadata = Result<ContactDto>.Ok(
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
    public void NonGenericSuccess()
    {
        _buffer.ResetWrittenCount();
        var wrapper = new HttpResultForWriting(_nonGenericSuccess, _resolvedOptions);
        using var writer = new Utf8JsonWriter(_buffer);
        HttpResultForWritingJsonConverter.Instance.Write(writer, wrapper, _serializerOptions);
        writer.Flush();
    }

    [Benchmark]
    public void NonGenericFailure()
    {
        _buffer.ResetWrittenCount();
        var wrapper = new HttpResultForWriting(_nonGenericFailure, _resolvedOptions);
        using var writer = new Utf8JsonWriter(_buffer);
        HttpResultForWritingJsonConverter.Instance.Write(writer, wrapper, _serializerOptions);
        writer.Flush();
    }

    [Benchmark]
    public void GenericSuccess()
    {
        _buffer.ResetWrittenCount();
        var wrapper = new HttpResultForWriting<ContactDto>(_genericSuccess, _resolvedOptions);
        using var writer = new Utf8JsonWriter(_buffer);
        var converter = new HttpResultForWritingJsonConverter<ContactDto>();
        converter.Write(writer, wrapper, _serializerOptions);
        writer.Flush();
    }

    [Benchmark]
    public void GenericFailure()
    {
        _buffer.ResetWrittenCount();
        var wrapper = new HttpResultForWriting<ContactDto>(_genericFailure, _resolvedOptions);
        using var writer = new Utf8JsonWriter(_buffer);
        var converter = new HttpResultForWritingJsonConverter<ContactDto>();
        converter.Write(writer, wrapper, _serializerOptions);
        writer.Flush();
    }

    [Benchmark]
    public void GenericSuccessWithMetadata()
    {
        var alwaysOptions = new ResolvedHttpWriteOptions(
            ValidationProblemSerializationFormat.AspNetCoreCompatible,
            MetadataSerializationMode.Always,
            null,
            true
        );

        _buffer.ResetWrittenCount();
        var wrapper = new HttpResultForWriting<ContactDto>(_genericSuccessWithMetadata, alwaysOptions);
        using var writer = new Utf8JsonWriter(_buffer);
        var converter = new HttpResultForWritingJsonConverter<ContactDto>();
        converter.Write(writer, wrapper, _serializerOptions);
        writer.Flush();
    }

    public sealed class ContactDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}

[JsonSerializable(typeof(HttpWriteSerializationBenchmarks.ContactDto))]
[JsonSerializable(typeof(HttpResultForWriting))]
[JsonSerializable(typeof(HttpResultForWriting<HttpWriteSerializationBenchmarks.ContactDto>))]
internal partial class HttpWriteSerializationBenchmarksJsonContext : JsonSerializerContext;
