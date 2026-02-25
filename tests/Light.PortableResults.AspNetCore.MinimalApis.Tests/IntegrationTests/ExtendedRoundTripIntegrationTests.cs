using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class ExtendedRoundTripIntegrationTests
{
    private readonly ExtendedMinimalApiApp _fixture;

    public ExtendedRoundTripIntegrationTests(ExtendedMinimalApiApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_GenericSuccessWithMetadata()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/value-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        var result = await response.ReadResultAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result<string>.Ok(
            "contact-42",
            MetadataObject.Create(("source", MetadataValue.FromString("value-metadata")))
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_NonGenericSuccessWithMetadata()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/non-generic-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result.Ok(
            MetadataObject.Create(
                ("note", MetadataValue.FromString("non-generic")),
                ("count", MetadataValue.FromInt64(3))
            )
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_RichValidationFailure()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/validation-rich",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

        Error[] errors =
        [
            new ()
            {
                Message = "Name is required",
                Code = "NameRequired",
                Target = "",
                Category = ErrorCategory.Validation,
                Metadata = MetadataObject.Create(("minLength", MetadataValue.FromInt64(1)))
            },
            new ()
            {
                Message = "Email is required",
                Target = "email",
                Category = ErrorCategory.Validation
            }
        ];
        var expectedResult = Result<ContactDto>.Fail(
            errors,
            MetadataObject.Create(("source", MetadataValue.FromString("rich")))
        );
        result.Should().Be(expectedResult);
    }
}
