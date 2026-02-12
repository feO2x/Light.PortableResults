using System.Net.Http;
using System.Threading.Tasks;
using Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: AssemblyFixture(typeof(RegularMvcApp))]

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

public sealed class RegularMvcApp : IAsyncLifetime
{
    public RegularMvcApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLightResultsForMvc();
        builder.Services.AddControllers()
           .AddApplicationPart(typeof(RegularMvcController).Assembly);

        App = builder.Build();
        App.MapControllers();
    }

    public WebApplication App { get; }

    public async ValueTask InitializeAsync() => await App.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }

    public HttpClient CreateHttpClient() => App.GetTestClient();
}
