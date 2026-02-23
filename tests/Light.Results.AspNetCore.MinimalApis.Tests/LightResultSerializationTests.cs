using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.Http.Writing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class LightResultSerializationTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldSerializeWithCustomSerializerOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsForMinimalApis();

        await using var provider = services.BuildServiceProvider();
        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = responseBody }
        };

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.AddDefaultLightResultsHttpWriteJsonConverters();

        var lightResult = new LightResult(Result.Ok(), serializerOptions: serializerOptions);

        await lightResult.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSerializeGenericResultWithCustomSerializerOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsForMinimalApis();

        await using var provider = services.BuildServiceProvider();
        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = responseBody }
        };

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.AddDefaultLightResultsHttpWriteJsonConverters();

        var lightResult = new LightResult<string>(
            Result<string>.Ok("hello"),
            serializerOptions: serializerOptions
        );

        await lightResult.ExecuteAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
    }
}
