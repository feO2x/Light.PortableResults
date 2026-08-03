using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Xunit;

namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// Verifies that results round-trip when the configured <see cref="JsonSerializerOptions" /> have no
/// reflection-backed resolver and the source-generated context declares nothing but the result value type.
/// </summary>
public sealed class SourceGeneratedRoundTripTests
{
    private static readonly MovieDto Movie = new ("Blade Runner", 1982);

    [Fact]
    public void GenericResultShouldRoundTripOverCloudEvents()
    {
        var result = Result<MovieDto>.Ok(Movie, MetadataObject.Create(("note", MetadataValue.FromString("body"))));

        var cloudEvent = result.ToCloudEvent(
            successType: "movie.created",
            failureType: "movie.rejected",
            id: "evt-1",
            options: SourceGeneratedOptions.CreateCloudEventsWriteOptions()
        );

        var readResult = new ReadOnlyMemory<byte>(cloudEvent)
           .ReadResult<MovieDto>(SourceGeneratedOptions.CreateCloudEventsReadOptions());

        readResult.Should().Be(result);
    }

    [Fact]
    public void FailedGenericResultShouldRoundTripOverCloudEvents()
    {
        var result = Result<MovieDto>.Fail(CreateFailure());

        var cloudEvent = result.ToCloudEvent(
            successType: "movie.created",
            failureType: "movie.rejected",
            id: "evt-2",
            options: SourceGeneratedOptions.CreateCloudEventsWriteOptions()
        );

        var readResult = new ReadOnlyMemory<byte>(cloudEvent)
           .ReadResult<MovieDto>(SourceGeneratedOptions.CreateCloudEventsReadOptions());

        readResult.Should().Be(result);
    }

    [Fact]
    public void NonGenericResultShouldRoundTripOverCloudEventsWithoutAnyConsumerRegistration()
    {
        var result = Result.Ok(MetadataObject.Create(("note", MetadataValue.FromString("body"))));

        var cloudEvent = result.ToCloudEvent(
            successType: "note.created",
            failureType: "note.rejected",
            id: "evt-3",
            options: SourceGeneratedOptions.CreateCloudEventsWriteOptions(NoContractsJsonTypeInfoResolver.Instance)
        );

        var readResult = new ReadOnlyMemory<byte>(cloudEvent)
           .ReadResult(SourceGeneratedOptions.CreateCloudEventsReadOptions(NoContractsJsonTypeInfoResolver.Instance));

        readResult.Should().Be(result);
    }

    [Fact]
    public void FailedNonGenericResultShouldRoundTripOverCloudEventsWithoutAnyConsumerRegistration()
    {
        var result = Result.Fail(CreateFailure());

        var cloudEvent = result.ToCloudEvent(
            successType: "note.created",
            failureType: "note.rejected",
            id: "evt-4",
            options: SourceGeneratedOptions.CreateCloudEventsWriteOptions(NoContractsJsonTypeInfoResolver.Instance)
        );

        var readResult = new ReadOnlyMemory<byte>(cloudEvent)
           .ReadResult(SourceGeneratedOptions.CreateCloudEventsReadOptions(NoContractsJsonTypeInfoResolver.Instance));

        readResult.Should().Be(result);
    }

    [Fact]
    public async Task GenericResultShouldRoundTripOverHttp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var result = Result<MovieDto>.Ok(Movie, MetadataObject.Create(("note", MetadataValue.FromString("body"))));

        using var response = SourceGeneratedOptions.CreateResponse(WriteGenericHttpBody(result));

        var readResult = await response.ReadResultAsync<MovieDto>(
            SourceGeneratedOptions.CreateHttpReadOptions(),
            cancellationToken
        );

        readResult.Should().Be(result);
    }

    [Fact]
    public async Task FailedGenericResultShouldRoundTripOverHttp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var result = Result<MovieDto>.Fail(CreateFailure());

        using var response = SourceGeneratedOptions.CreateResponse(WriteGenericHttpBody(result));

        var readResult = await response.ReadResultAsync<MovieDto>(
            SourceGeneratedOptions.CreateHttpReadOptions(),
            cancellationToken
        );

        readResult.Should().Be(result);
    }

    [Fact]
    public async Task NonGenericResultShouldRoundTripOverHttpWithoutAnyConsumerRegistration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var result = Result.Ok(MetadataObject.Create(("note", MetadataValue.FromString("body"))));

        using var response = SourceGeneratedOptions.CreateResponse(WriteNonGenericHttpBody(result));

        var readResult = await response.ReadResultAsync(
            SourceGeneratedOptions.CreateHttpReadOptions(NoContractsJsonTypeInfoResolver.Instance),
            cancellationToken
        );

        readResult.Should().Be(result);
    }

    [Fact]
    public async Task FailedNonGenericResultShouldRoundTripOverHttpWithoutAnyConsumerRegistration()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var result = Result.Fail(CreateFailure());

        using var response = SourceGeneratedOptions.CreateResponse(WriteNonGenericHttpBody(result));

        var readResult = await response.ReadResultAsync(
            SourceGeneratedOptions.CreateHttpReadOptions(NoContractsJsonTypeInfoResolver.Instance),
            cancellationToken
        );

        readResult.Should().Be(result);
    }

    private static PortableResultsHttpWriteOptions CreateHttpWriteOptions() =>
        new () { MetadataSerializationMode = MetadataSerializationMode.Always };

    private static Errors CreateFailure() =>
        new (
            new Error
            {
                Message = "The movie was rejected.",
                Code = "MOVIE_REJECTED",
                Target = "title",
                Category = ErrorCategory.Validation,
                Metadata = MetadataObject.Create(("attempt", MetadataValue.FromString("1")))
            }
        );

    // The HTTP write path resolves HttpResultForWriting<T> through the configured resolver, which is deliberately
    // left to the consumer. The converter is therefore invoked directly so that the source-generated context only
    // has to declare the value type, which is what the HTTP read path under test requires.
    private static byte[] WriteGenericHttpBody(Result<MovieDto> result) =>
        SourceGeneratedOptions.WriteHttpBody(
            writer => new HttpResultForWritingJsonConverter<MovieDto>().Write(
                writer,
                result.ToHttpResultForWriting(CreateHttpWriteOptions()),
                SourceGeneratedOptions.CreateHttpWriteOptions()
            )
        );

    private static byte[] WriteNonGenericHttpBody(Result result) =>
        SourceGeneratedOptions.WriteHttpBody(
            writer => HttpResultForWritingJsonConverter.Instance.Write(
                writer,
                result.ToHttpResultForWriting(CreateHttpWriteOptions()),
                SourceGeneratedOptions.CreateHttpWriteOptions(NoContractsJsonTypeInfoResolver.Instance)
            )
        );
}

/// <summary>
/// Creates Light.PortableResults options whose only type info resolver is a source-generated context.
/// </summary>
public static class SourceGeneratedOptions
{
    public static PortableResultsCloudEventsWriteOptions CreateCloudEventsWriteOptions(
        IJsonTypeInfoResolver? resolver = null
    )
    {
        var serializerOptions = CreateSerializerOptions(resolver);
        serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();
        return new PortableResultsCloudEventsWriteOptions
        {
            SerializerOptions = serializerOptions,
            Source = "urn:test:source"
        };
    }

    public static PortableResultsCloudEventsReadOptions CreateCloudEventsReadOptions(
        IJsonTypeInfoResolver? resolver = null,
        Action<JsonSerializerOptions>? configureBeforeDefaults = null
    )
    {
        var serializerOptions = CreateSerializerOptions(resolver);
        configureBeforeDefaults?.Invoke(serializerOptions);
        serializerOptions.AddDefaultPortableResultsCloudEventsReadJsonConverters();
        return new PortableResultsCloudEventsReadOptions { SerializerOptions = serializerOptions };
    }

    public static JsonSerializerOptions CreateHttpWriteOptions(IJsonTypeInfoResolver? resolver = null)
    {
        var serializerOptions = CreateSerializerOptions(resolver);
        serializerOptions.AddDefaultPortableResultsHttpWriteJsonConverters();
        return serializerOptions;
    }

    public static PortableResultsHttpReadOptions CreateHttpReadOptions(
        IJsonTypeInfoResolver? resolver = null,
        Action<JsonSerializerOptions>? configureBeforeDefaults = null
    )
    {
        var serializerOptions = CreateSerializerOptions(resolver);
        configureBeforeDefaults?.Invoke(serializerOptions);
        serializerOptions.AddDefaultPortableResultsHttpReadJsonConverters();
        return new PortableResultsHttpReadOptions { SerializerOptions = serializerOptions };
    }

    public static JsonSerializerOptions CreateSerializerOptions(IJsonTypeInfoResolver? resolver = null) =>
        new (JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = resolver ?? MovieJsonContext.Default
        };

    public static byte[] WriteHttpBody(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return stream.ToArray();
    }

    // The status code and content type are derived from the written body so that the response mirrors what the
    // Light.PortableResults ASP.NET Core integrations would emit for the same result.
    public static HttpResponseMessage CreateResponse(byte[] body)
    {
        var json = Encoding.UTF8.GetString(body);
        var statusCode = HttpStatusCode.OK;
        var isProblemDetails = false;

        using (var document = JsonDocument.Parse(json))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("status", out var status))
            {
                isProblemDetails = true;
                statusCode = (HttpStatusCode) status.GetInt32();
            }
        }

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                isProblemDetails ? "application/problem+json" : "application/json"
            )
        };
    }
}
