using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class HttpResponseMessageReadOutsideInTests
{
    private readonly HttpReadMinimalApiApp _fixture;

    public HttpResponseMessageReadOutsideInTests(HttpReadMinimalApiApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ReadResultAsync_ShouldParseBareValue()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/bare-int",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<int>(cancellationToken: TestContext.Current.CancellationToken);

        result.Should().Be(Result<int>.Ok(42));
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseWrappedValueAndMetadata()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/wrapped-string",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result<string>.Ok(
            "ok",
            MetadataObject.Create(("trace", MetadataValue.FromString("t-1")))
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseWrappedValue_WhenPreferWrappedValueIsSet()
    {
        var options = new PortableResultsHttpReadOptions
        {
            PreferSuccessPayload = PreferSuccessPayload.WrappedValue
        };

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/wrapped-string",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<string>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedResult = Result<string>.Ok(
            "ok",
            MetadataObject.Create(("trace", MetadataValue.FromString("t-1")))
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldTreatProblemDetailsShapeAsSuccess_WhenHttpResponseIndicatesSuccess()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/problem-shape-success",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result =
            await response.ReadResultAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.GetProperty("type").GetString().Should().Be("dto");
        result.Value.GetProperty("title").GetString().Should().Be("Successful DTO");
        result.Value.GetProperty("status").GetInt32().Should().Be(200);
        result.Value.GetProperty("errors").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_WhenWrapperMissingValue()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/wrapper-missing-value",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () =>
            await response.ReadResultAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseMetadataOnly_ForNonGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/non-generic-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result.Ok(MetadataObject.Create(("note", MetadataValue.FromString("hi"))));
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldThrow_ForNonGenericUnexpectedBody()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/non-generic-unexpected",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await response.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseRichProblemDetails()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/failure-rich",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Message.Should().Be("Name is required");
        result.Errors[0].Code.Should().Be("NameRequired");
        result.Errors[0].Target.Should().Be("name");
        result.Errors[0].Category.Should().Be(ErrorCategory.Validation);
        result.Metadata.Should().Be(MetadataObject.Create(("traceId", MetadataValue.FromString("abc"))));
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParseAspNetCoreProblemDetailsWithErrorDetails()
    {
        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/failure-aspnet",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

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
        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                AllHeadersSelectionStrategy.Instance,
                HttpHeaderParserRegistry.Create([parser])
            )
        };

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/header-alias",
            cancellationToken: TestContext.Current.CancellationToken
        );

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await response.ReadResultAsync(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldAllowLastWriteWins_ForAliasConflicts()
    {
        var parser = new TraceHeaderParser();
        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                AllHeadersSelectionStrategy.Instance,
                HttpHeaderParserRegistry.Create([parser]),
                HeaderConflictStrategy.LastWriteWins
            )
        };

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/header-alias",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceId", out var traceId).Should().BeTrue();
        traceId.Should().Be("second");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldParsePrimitiveAndMultiValueHeaders_WhenPrimitiveParsingIsEnabled()
    {
        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                new AllowListHeaderSelectionStrategy(["X-Bool", "X-Int", "X-Double", "X-Text", "X-Ids"])
            )
        };

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/header-values",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<int>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetBoolean("X-Bool", out var boolValue).Should().BeTrue();
        result.Metadata.Value.TryGetInt64("X-Int", out var intValue).Should().BeTrue();
        result.Metadata.Value.TryGetDouble("X-Double", out var doubleValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Text", out var textValue).Should().BeTrue();
        result.Metadata.Value.TryGetArray("X-Ids", out var ids).Should().BeTrue();
        ids.Count.Should().Be(2);
        ids[0].TryGetInt64(out var firstId).Should().BeTrue();
        ids[1].TryGetInt64(out var secondId).Should().BeTrue();

        boolValue.Should().BeTrue();
        intValue.Should().Be(123);
        doubleValue.Should().Be(3.5);
        textValue.Should().Be("alpha");
        firstId.Should().Be(1);
        secondId.Should().Be(2);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldKeepHeaderValuesAsStrings_WhenStringOnlyParsingIsEnabled()
    {
        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                new AllowListHeaderSelectionStrategy(["X-Bool", "X-Int", "X-Double"]),
                HttpHeaderParserRegistry.Create(Array.Empty<HttpHeaderParser>()),
                headerValueParsingMode: HeaderValueParsingMode.StringOnly
            )
        };

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/header-values",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<int>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("X-Bool", out var boolValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Int", out var intValue).Should().BeTrue();
        result.Metadata.Value.TryGetString("X-Double", out var doubleValue).Should().BeTrue();
        boolValue.Should().Be("true");
        intValue.Should().Be("123");
        doubleValue.Should().Be("3.5");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldApplyEmptyBodyRules()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var successResponse = await httpClient.GetAsync(
            "/api/read/empty-success",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var nonGenericResult =
            await successResponse.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);
        nonGenericResult.Should().Be(Result.Ok());

        var genericAct = async () =>
        {
            // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
            using var genericSuccessResponse = await httpClient.GetAsync(
                "/api/read/empty-success",
                cancellationToken: TestContext.Current.CancellationToken
            );
            await genericSuccessResponse.ReadResultAsync<int>(cancellationToken: TestContext.Current.CancellationToken);
        };

        await genericAct.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage(HttpResponseMessageExtensions.GenericSuccessPayloadRequiredMessage);

        var failureAct = async () =>
        {
            // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
            using var failureResponse = await httpClient.GetAsync(
                "/api/read/empty-failure",
                cancellationToken: TestContext.Current.CancellationToken
            );
            await failureResponse.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);
        };

        await failureAct.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage(HttpResponseMessageExtensions.FailurePayloadRequiredMessage);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldUseJsonSerializerContext_ForGenericSuccess()
    {
        var options = CreateContextBackedOptions();

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/context-success",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<ContactDto>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedResult = Result<ContactDto>.Ok(
            new ContactDto
            {
                Id = new Guid("6B8A4DCA-779D-4F36-8274-487FE3E86B5A"),
                Name = "Contact A"
            },
            MetadataObject.Create(("source", MetadataValue.FromString("context")))
        );

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldUseJsonSerializerContext_ForGenericFailure()
    {
        var options = CreateContextBackedOptions();

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/context-failure",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync<ContactDto>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedResult = Result<ContactDto>.Fail(
            new Error
            {
                Message = "Name is required",
                Code = "NameRequired",
                Target = "name",
                Category = ErrorCategory.Validation
            },
            MetadataObject.Create(("source", MetadataValue.FromString("context")))
        );

        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldUseJsonSerializerContext_ForNonGenericSuccess()
    {
        var options = CreateContextBackedOptions();

        using var httpClient = _fixture.CreateHttpClient();
        using var response = await httpClient.GetAsync(
            "/api/read/non-generic-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        var result = await response.ReadResultAsync(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedResult = Result.Ok(MetadataObject.Create(("note", MetadataValue.FromString("hi"))));
        result.Should().Be(expectedResult);
    }

    private static PortableResultsHttpReadOptions CreateContextBackedOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = HttpReadJsonContext.Default
        };
        serializerOptions.AddDefaultPortableResultsHttpReadJsonConverters();

        return new PortableResultsHttpReadOptions
        {
            SerializerOptions = serializerOptions
        };
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
}
