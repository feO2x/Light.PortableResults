using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Http.Reading;

public sealed class HttpResponseMessageExtensionsTests
{
    [Fact]
    public async Task ReadResultAsync_ShouldThrow_WhenResponseIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        HttpResponseMessage response = null!;

        Func<Task> act = async () => await response.ReadResultAsync(cancellationToken: cancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldThrow_WhenResponseIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        HttpResponseMessage response = null!;

        Func<Task> act = async () => await response.ReadResultAsync<int>(cancellationToken: cancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldTreatProblemDetailsAsFailure_ByDefault()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = CreateJsonResponse(
            HttpStatusCode.OK,
            """
            {
                "type": "https://example.org/problems/validation",
                "title": "Validation failed",
                "status": 400,
                "errors": [
                    {
                        "message": "Name is required",
                        "target": "name",
                        "category": "Validation"
                    }
                ]
            }
            """,
            "application/problem+json"
        );

        var result = await response.ReadResultAsync<JsonElement>(cancellationToken: cancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldTreatProblemDetailsAsSuccess_WhenDisabled()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = CreateJsonResponse(
            HttpStatusCode.OK,
            """
            {
                "type": "dto",
                "title": "Successful DTO",
                "status": 200,
                "errors": []
            }
            """,
            "application/problem+json"
        );

        var options = new PortableResultsHttpReadOptions
        {
            TreatProblemDetailsAsFailure = false
        };

        var result = await response.ReadResultAsync<JsonElement>(options, cancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.GetProperty("type").GetString().Should().Be("dto");
        result.Value.GetProperty("title").GetString().Should().Be("Successful DTO");
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldNormalizeUnknownPreferenceToAuto()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = CreateJsonResponse(
            HttpStatusCode.OK,
            """
            {
                "value": "ok",
                "metadata": {
                    "source": "body"
                }
            }
            """
        );

        var options = new PortableResultsHttpReadOptions
        {
            PreferSuccessPayload = (PreferSuccessPayload) 99
        };

        var result = await response.ReadResultAsync<string>(options, cancellationToken);

        var expectedResult = Result<string>.Ok(
            "ok",
            MetadataObject.Create(("source", MetadataValue.FromString("body")))
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldThrow_WhenBarePayloadConverterProducesNullValue()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var serializerOptions = Module.CreateDefaultSerializerOptions();
        serializerOptions.Converters.Insert(0, new NullBareStringPayloadConverter());

        var options = new PortableResultsHttpReadOptions
        {
            SerializerOptions = serializerOptions,
            PreferSuccessPayload = PreferSuccessPayload.BareValue
        };

        using var response = CreateJsonResponse(HttpStatusCode.OK, "\"ignored\"");

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await response.ReadResultAsync<string>(options, cancellationToken);

        await act.Should().ThrowAsync<JsonException>().WithMessage("Result value cannot be null.");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_WhenFailurePayloadConverterProducesNoErrors()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var serializerOptions = Module.CreateDefaultSerializerOptions();
        serializerOptions.Converters.Insert(0, new EmptyFailurePayloadConverter());

        var options = new PortableResultsHttpReadOptions
        {
            SerializerOptions = serializerOptions
        };

        using var response = CreateJsonResponse(HttpStatusCode.BadRequest, "{}", "application/problem+json");

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await response.ReadResultAsync(options, cancellationToken);

        await act
           .Should().ThrowAsync<JsonException>()
           .WithMessage(HttpResponseMessageExtensions.NonGenericFailureMustDeserializeToFailedMessage);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldApplyLastWriteWins_ForHeaderAliasConflicts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = CreateJsonResponse(HttpStatusCode.OK, """{"metadata":null}""");
        response.Headers.Add("X-TraceId", "first");
        response.Headers.Add("X-Correlation-Id", "second");

        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                new AllowListHeaderSelectionStrategy(["X-TraceId", "X-Correlation-Id"]),
                HttpHeaderParserRegistry.Create([new TraceParser()]),
                HeaderConflictStrategy.LastWriteWins
            )
        };

        var result = await response.ReadResultAsync(options, cancellationToken);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceId", out var traceId).Should().BeTrue();
        traceId.Should().Be("second");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_ForHeaderAliasConflicts_WhenStrategyIsThrow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = CreateJsonResponse(HttpStatusCode.OK, """{"metadata":null}""");
        response.Headers.Add("X-TraceId", "first");
        response.Headers.Add("X-Correlation-Id", "second");

        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                new AllowListHeaderSelectionStrategy(["X-TraceId", "X-Correlation-Id"]),
                HttpHeaderParserRegistry.Create([new TraceParser()])
            )
        };

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await response.ReadResultAsync(options, cancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldReadBody_WhenContentLengthIsUnknown_ForNonGenericResult()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new UnknownLengthJsonContent("""{"metadata":{"note":"body"}}""");

        var result = await response.ReadResultAsync(cancellationToken: cancellationToken);

        result.Should().Be(Result.Ok(MetadataObject.Create(("note", MetadataValue.FromString("body")))));
    }

    [Fact]
    public async Task ReadResultAsyncOfT_ShouldReadBody_WhenContentLengthIsUnknown_ForGenericResult()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new UnknownLengthJsonContent("42");

        var result = await response.ReadResultAsync<int>(cancellationToken: cancellationToken);

        result.Should().Be(Result<int>.Ok(42));
    }

    [Fact]
    public async Task ReadResultAsync_ShouldHonorCanceledToken_WhenBodyIsPresent()
    {
        using var response = CreateJsonResponse(HttpStatusCode.OK, """{"metadata":{"note":"hi"}}""");
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // ReSharper disable AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await response.ReadResultAsync(cancellationToken: cancellationTokenSource.Token);
        // ReSharper restore AccessToDisposedClosure

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrowJsonException_WhenLengthIsUnknownAndStreamIsNonSeekableAndEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new UnknownLengthNonSeekableEmptyContent();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await response.ReadResultAsync(cancellationToken: cancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    private static HttpResponseMessage CreateJsonResponse(
        HttpStatusCode statusCode,
        string json,
        string? mediaType = null
    )
    {
        var contentType = mediaType ?? "application/json";
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, contentType)
        };
    }

    private sealed class NullBareStringPayloadConverter : JsonConverter<HttpReadBareSuccessResultPayload<string>>
    {
        public override HttpReadBareSuccessResultPayload<string> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            reader.Skip();
            return new HttpReadBareSuccessResultPayload<string>(null!);
        }

        public override void Write(
            Utf8JsonWriter writer,
            HttpReadBareSuccessResultPayload<string> value,
            JsonSerializerOptions options
        ) => throw new NotSupportedException();
    }

    private sealed class EmptyFailurePayloadConverter : JsonConverter<HttpReadFailureResultPayload>
    {
        public override HttpReadFailureResultPayload Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            reader.Skip();
            return new HttpReadFailureResultPayload(default, null);
        }

        public override void Write(
            Utf8JsonWriter writer,
            HttpReadFailureResultPayload value,
            JsonSerializerOptions options
        ) => throw new NotSupportedException();
    }

    private sealed class TraceParser : HttpHeaderParser
    {
        public TraceParser() : base("traceId", ImmutableArray.Create("X-TraceId", "X-Correlation-Id")) { }

        public override MetadataValue ParseHeader(
            string headerName,
            IReadOnlyList<string> values,
            MetadataValueAnnotation annotation
        )
        {
            return MetadataValue.FromString(values[0], annotation);
        }
    }

    private sealed class UnknownLengthJsonContent : HttpContent
    {
        private readonly byte[] _payload;

        public UnknownLengthJsonContent(string json)
        {
            _payload = Encoding.UTF8.GetBytes(json);
            Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8"
            };
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return stream.WriteAsync(_payload, 0, _payload.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class UnknownLengthNonSeekableEmptyContent : HttpContent
    {
        public UnknownLengthNonSeekableEmptyContent()
        {
            Headers.ContentType = new MediaTypeHeaderValue("application/json")
            {
                CharSet = "utf-8"
            };
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromResult<Stream>(new EmptyNonSeekableStream());
    }

    private sealed class EmptyNonSeekableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => 0L;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => 0;

        public override int Read(Span<byte> buffer) => 0;

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => Task.FromResult(0);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            new (0);

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
