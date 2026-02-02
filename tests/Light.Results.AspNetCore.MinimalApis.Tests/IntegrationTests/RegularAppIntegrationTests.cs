using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class RegularAppIntegrationTests
{
    private readonly RegularMinimalApiApp _fixture;

    public RegularAppIntegrationTests(RegularMinimalApiApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ToMinimalApiResult_ShouldSetHeader_EvenWhenMetadataSerializationForSuccessResultsIsDisabled()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/contacts",
            cancellationToken: TestContext.Current.CancellationToken
        );

        await Verifier.Verify(response);
    }

    [Fact]
    public async Task ToHttp201CreatedMinimalApiResult_ShouldProduce201ResponseWithLocation()
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
}
