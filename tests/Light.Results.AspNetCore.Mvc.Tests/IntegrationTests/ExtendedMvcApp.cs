using System.Net.Http;
using System.Threading.Tasks;
using Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;
using Light.Results.AspNetCore.Shared.Enrichment;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[assembly: AssemblyFixture(typeof(ExtendedMvcApp))]

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

public sealed class ExtendedMvcApp : IAsyncLifetime
{
    public ExtendedMvcApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLightResultsForMvc();
        builder.Services.AddControllers()
           .AddApplicationPart(typeof(ExtendedMvcController).Assembly);
        builder.Services.AddSingleton<IHttpResultEnricher, StaticMetadataEnricher>();

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

    public sealed class StaticMetadataEnricher : IHttpResultEnricher
    {
        public TResult Enrich<TResult>(TResult result, HttpContext httpContext)
            where TResult : struct, IResultObject, ICanReplaceMetadata<TResult>
        {
            if (result.Metadata is not null)
            {
                return result;
            }

            var metadata = MetadataObject.Create(("enriched", MetadataValue.FromString("true")));
            return result.ReplaceMetadata(metadata);
        }
    }
}
