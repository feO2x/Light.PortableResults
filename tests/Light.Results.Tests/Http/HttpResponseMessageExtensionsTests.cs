using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.Http;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http;

public sealed class HttpResponseMessageExtensionsTests
{
    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReadResultAsync_ShouldParseBareValue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("42", Encoding.UTF8, "application/json");

        var result = await response.ReadResultAsync<int>(cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("Content-Type", out var contentType).Should().BeTrue();
        contentType.Should().Contain("application/json");
        result.Metadata.Value.TryGetInt64("Content-Length", out var contentLength).Should().BeTrue();
        contentLength.Should().Be(2);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseWrappedValueAndMetadata()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(
            "{\"value\":\"ok\",\"metadata\":{\"trace\":\"t-1\"}}",
            Encoding.UTF8,
            "application/json"
        );

        var result = await response.ReadResultAsync<string>(cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be("ok");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("trace", out var trace).Should().BeTrue();
        trace.Should().Be("t-1");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_WhenWrapperMissingValue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"metadata\":{\"trace\":\"t-1\"}}", Encoding.UTF8, "application/json");

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await response.ReadResultAsync<string>(cancellationToken: TestCancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseMetadataOnly_ForNonGenericResult()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"metadata\":{\"note\":\"hi\"}}", Encoding.UTF8, "application/json");

        var result = await response.ReadResultAsync(cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("note", out var note).Should().BeTrue();
        note.Should().Be("hi");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_ForNonGenericUnexpectedBody()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"value\":\"ok\"}", Encoding.UTF8, "application/json");

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await response.ReadResultAsync(cancellationToken: TestCancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseRichProblemDetails()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        response.Content = new StringContent(
            """
            {
                "type": "https://example.org/problems/validation",
                "title": "Validation failed",
                "status": 400,
                "errors": [
                    {
                        "message": "Name is required",
                        "code": "NameRequired",
                        "target": "name",
                        "category": "Validation"
                    }
                ],
                "metadata": {
                    "traceId": "abc"
                }
            }
            """,
            Encoding.UTF8,
            "application/problem+json"
        );

        var result = await response.ReadResultAsync<string>(cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Message.Should().Be("Name is required");
        result.Errors[0].Code.Should().Be("NameRequired");
        result.Errors[0].Target.Should().Be("name");
        result.Errors[0].Category.Should().Be(ErrorCategory.Validation);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceId", out var traceId).Should().BeTrue();
        traceId.Should().Be("abc");
        result.Metadata.Value.TryGetString("Content-Type", out var contentType).Should().BeTrue();
        contentType.Should().Contain("application/problem+json");
        result.Metadata.Value.TryGetInt64("Content-Length", out var contentLength).Should().BeTrue();
        contentLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseAspNetCoreProblemDetailsWithErrorDetails()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        response.Content = new StringContent(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Bad Request",
              "status": 400,
              "errors": {
                "name": [
                  "Name required",
                  "Name too short"
                ]
              },
              "errorDetails": [
                {
                  "target": "name",
                  "index": 1,
                  "code": "MinLength",
                  "category": "Validation"
                }
              ]
            }
            """,
            Encoding.UTF8,
            "application/problem+json"
        );

        var result = await response.ReadResultAsync<string>(cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(2);
        result.Errors[0].Message.Should().Be("Name required");
        result.Errors[0].Code.Should().BeNull();
        result.Errors[0].Category.Should().Be(ErrorCategory.Validation);
        result.Errors[1].Message.Should().Be("Name too short");
        result.Errors[1].Code.Should().Be("MinLength");
        result.Errors[1].Category.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_OnAliasConflictByDefault()
    {
        var parser = new TraceHeaderParser();
        var options = new LightResultHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(HttpHeaderParserRegistry.Create([parser]))
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"metadata\":{\"note\":\"ok\"}}", Encoding.UTF8, "application/json");

        response.Headers.Add("X-TraceId", "first");
        response.Content.Headers.Add("X-Correlation-Id", "second");

        var act =
            // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
            async () => await response.ReadResultAsync(options: options, cancellationToken: TestCancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldAllowLastWriteWins_ForAliasConflicts()
    {
        var parser = new TraceHeaderParser();
        var options = new LightResultHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(HttpHeaderParserRegistry.Create([parser])),
            HeaderConflictStrategy = HeaderConflictStrategy.LastWriteWins
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"metadata\":{\"note\":\"ok\"}}", Encoding.UTF8, "application/json");

        response.Headers.Add("X-TraceId", "first");
        response.Content.Headers.Add("X-Correlation-Id", "second");

        var result = await response.ReadResultAsync(options: options, cancellationToken: TestCancellationToken);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceId", out var traceId).Should().BeTrue();
        traceId.Should().Be("second");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseMultiValueHeaders()
    {
        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.AllowList,
            HeaderAllowList = ["X-Ids"]
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("{\"metadata\":{\"note\":\"ok\"}}", Encoding.UTF8, "application/json");

        response.Headers.Add("X-Ids", new[] { "1", "2" });

        var result = await response.ReadResultAsync(options: options, cancellationToken: TestCancellationToken);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetArray("X-Ids", out var values).Should().BeTrue();
        values.Count.Should().Be(2);
        values[0].TryGetInt64(out var first).Should().BeTrue();
        values[1].TryGetInt64(out var second).Should().BeTrue();
        first.Should().Be(1);
        second.Should().Be(2);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParsePrimitiveHeaderValues_WhenPrimitiveParsingIsEnabled()
    {
        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.AllowList,
            HeaderAllowList = ["X-Bool", "X-Int", "X-Double", "X-Text"]
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("42", Encoding.UTF8, "application/json");

        response.Headers.Add("X-Bool", "true");
        response.Headers.Add("X-Int", "123");
        response.Headers.Add("X-Double", "3.5");
        response.Headers.Add("X-Text", "alpha");

        var result = await response.ReadResultAsync<int>(options: options, cancellationToken: TestCancellationToken);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetBoolean("X-Bool", out var boolValue).Should().BeTrue();
        result.Metadata.Value.TryGetInt64("X-Int", out var intValue).Should().BeTrue();
        result.Metadata.Value.TryGetDouble("X-Double", out var doubleValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Text", out var textValue).Should().BeTrue();
        boolValue.Should().BeTrue();
        intValue.Should().Be(123);
        doubleValue.Should().Be(3.5);
        textValue.Should().Be("alpha");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldKeepHeaderValuesAsStrings_WhenStringOnlyParsingIsEnabled()
    {
        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.AllowList,
            HeaderAllowList = ["X-Bool", "X-Int", "X-Double"],
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                HttpHeaderParserRegistry.Create(Array.Empty<HttpHeaderParser>()),
                HeaderValueParsingMode.StringOnly
            )
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("42", Encoding.UTF8, "application/json");

        response.Headers.Add("X-Bool", "true");
        response.Headers.Add("X-Int", "123");
        response.Headers.Add("X-Double", "3.5");

        var result = await response.ReadResultAsync<int>(options: options, cancellationToken: TestCancellationToken);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("X-Bool", out var boolValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Int", out var intValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Double", out var doubleValue).Should().BeTrue();
        boolValue.Should().Be("true");
        intValue.Should().Be("123");
        doubleValue.Should().Be("3.5");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldUseCustomGenericResultConverter_WhenProvided()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.Converters.Add(new CustomIntResultConverter());

        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.None,
            SerializerOptions = serializerOptions
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("\"ignored\"", Encoding.UTF8, "application/json");

        var result = await response.ReadResultAsync<int>(options: options, cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(1337);
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_WhenFailureResponseDeserializesToSuccessViaCustomConverter()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.Converters.Add(new CustomIntResultConverter());

        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.None,
            SerializerOptions = serializerOptions
        };

        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        response.Content = new StringContent("\"ignored\"", Encoding.UTF8, "application/problem+json");

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => response.ReadResultAsync<int>(options: options, cancellationToken: TestCancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldUseCustomNonGenericResultConverter_WhenProvided()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.Converters.Add(new CustomResultConverter());

        var options = new LightResultHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.None,
            SerializerOptions = serializerOptions
        };

        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("\"ignored\"", Encoding.UTF8, "application/json");

        var result = await response.ReadResultAsync(options: options, cancellationToken: TestCancellationToken);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("source", out var source).Should().BeTrue();
        source.Should().Be("custom");
    }

    private sealed class TraceHeaderParser : HttpHeaderParser
    {
        public TraceHeaderParser() : base("traceId", ImmutableArray.Create("X-TraceId", "X-Correlation-Id")) { }

        public override MetadataValue ParseHeader(
            string headerName,
            IReadOnlyList<string> values,
            MetadataValueAnnotation annotation
        )
        {
            return MetadataValue.FromString(values[0], annotation);
        }
    }

    private sealed class CustomIntResultConverter : JsonConverter<Result<int>>
    {
        public override Result<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return Result<int>.Ok(1337);
        }

        public override void Write(Utf8JsonWriter writer, Result<int> value, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }

    private sealed class CustomResultConverter : JsonConverter<Result>
    {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return Result.Ok(MetadataObject.Create(("source", MetadataValue.FromString("custom"))));
        }

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options) =>
            throw new NotSupportedException();
    }
}
