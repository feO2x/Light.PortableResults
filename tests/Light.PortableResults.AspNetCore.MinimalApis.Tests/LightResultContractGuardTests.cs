using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

/// <summary>
/// Verifies that the Minimal API results report the unresolved wrapper type instead of failing inside
/// System.Text.Json when the configured resolver cannot supply its contract.
/// </summary>
public sealed class LightResultContractGuardTests
{
    [Fact]
    public async Task ExecuteAsyncShouldNameTheUnresolvedWrapperType()
    {
        await using var provider = CreateServiceProvider();
        var httpContext = CreateHttpContext(provider);
        var lightResult = new LightResult(Result.Ok(), serializerOptions: CreateSerializerOptions());

        var act = async () => await lightResult.ExecuteAsync(httpContext);

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage($"Could not resolve 'JsonTypeInfo<{typeof(HttpResultForWriting)}>'*");
    }

    [Fact]
    public async Task ExecuteAsyncShouldNameTheUnresolvedGenericWrapperType()
    {
        await using var provider = CreateServiceProvider();
        var httpContext = CreateHttpContext(provider);
        var lightResult = new LightResult<string>(
            Result<string>.Ok("hello"),
            serializerOptions: CreateSerializerOptions()
        );

        var act = async () => await lightResult.ExecuteAsync(httpContext);

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage($"Could not resolve 'JsonTypeInfo<{typeof(HttpResultForWriting<string>)}>'*");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddPortableResultsForMinimalApis();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider provider) =>
        new ()
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() }
        };

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = NoContractsJsonTypeInfoResolver.Instance
        };
        serializerOptions.AddDefaultPortableResultsHttpWriteJsonConverters();
        return serializerOptions;
    }
}

/// <summary>
/// A resolver that supplies no contract for any type.
/// </summary>
public sealed class NoContractsJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    public static NoContractsJsonTypeInfoResolver Instance { get; } = new ();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
}
