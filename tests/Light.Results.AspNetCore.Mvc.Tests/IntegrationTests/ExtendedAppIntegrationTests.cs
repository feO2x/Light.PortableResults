using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using VerifyXunit;
using Xunit;

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

public sealed class ExtendedAppIntegrationTests
{
    private readonly ExtendedMvcApp _fixture;

    public ExtendedAppIntegrationTests(ExtendedMvcApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ToMvcActionResult_ShouldSerializeMetadata_WhenMetadataSerializationIsAlways()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldSerializeValueAndMetadata_WhenMetadataSerializationIsAlways()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/value-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldSerializeMetadataOnly_ForNonGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/non-generic-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldSerializeValidationErrors_InRichFormat()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/validation-rich",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldSerializeValidationErrors_WhenErrorCountExceedsMax()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/validation-fallback",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldUseCustomSerializerOptions_ForNonGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/custom-converter",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldUseCustomSerializerOptions_ForGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/custom-converter-generic",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldIncludeEnricherMetadata_ForErrorResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/enriched-error",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldNotSetContentType_WhenNonGenericMetadataIsHeaderOnly()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/non-generic-header-only-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.TryGetValues("X-TraceId", out var headerValues).Should().BeTrue();
        headerValues.Should().ContainSingle("trace-42");

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().BeEmpty();

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldNotSerializeMetadata_WhenGenericMetadataIsHeaderOnly()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/generic-header-only-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.TryGetValues("X-TraceId", out var headerValues).Should().BeTrue();
        headerValues.Should().ContainSingle("trace-43");
        response.Headers.Contains("X-Null").Should().BeFalse();

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldNotSetHeader_WhenMetadataValueIsNull()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/non-generic-null-header",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Contains("X-Null").Should().BeFalse();

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldHonorHeaderAnnotationFlags_ForGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/generic-header-annotation-flags",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.TryGetValues("X-HeaderAndBody", out var headerValues).Should().BeTrue();
        headerValues.Should().ContainSingle("both");
        response.Headers.Contains("X-None").Should().BeFalse();

        await Verifier.Verify(response);
    }
}
