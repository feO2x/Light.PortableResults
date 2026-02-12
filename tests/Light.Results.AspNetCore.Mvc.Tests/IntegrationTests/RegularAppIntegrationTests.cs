using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

public sealed class RegularAppIntegrationTests
{
    private readonly RegularMvcApp _fixture;

    public RegularAppIntegrationTests(RegularMvcApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ToMvcActionResult_ShouldSetHeader_EvenWhenMetadataSerializationForSuccessResultsIsDisabled()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/contacts",
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToHttp201CreatedMvcActionResult_ShouldProduce201ResponseWithLocation()
    {
        using var httpClient = _fixture.CreateHttpClient();
        var dto = new ContactDto { Id = new Guid("36921058-446F-4515-8BD9-968893A94153"), Name = "Foo" };

        using var response = await httpClient.PutAsJsonAsync(
            "/api/contacts",
            dto,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldProduce404ProblemDetails_WhenResultHasNotFoundError()
    {
        using var httpClient = _fixture.CreateHttpClient();
        var id = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        using var response = await httpClient.GetAsync(
            $"/api/contacts/not-found/{id}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldProduce400ProblemDetails_WhenResultHasValidationErrors()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/contacts/validation-error",
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldProduce200_ForNonGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();
        var id = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        using var response = await httpClient.DeleteAsync(
            $"/api/contacts/{id}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToHttp201CreatedMvcActionResult_ShouldProduce201_ForNonGenericResult()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.PostAsync(
            "/api/contacts/action",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToMvcActionResult_ShouldProduce400ProblemDetails_ForNonGenericResultWithError()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.PostAsync(
            "/api/contacts/validate",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }
}
