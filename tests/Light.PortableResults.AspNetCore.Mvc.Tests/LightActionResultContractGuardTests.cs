using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.PortableResults.AspNetCore.Mvc.Tests;

/// <summary>
/// Verifies that the MVC action results report the unresolved wrapper type instead of failing inside
/// System.Text.Json when the configured resolver cannot supply its contract.
/// </summary>
public sealed class LightActionResultContractGuardTests
{
    [Fact]
    public async Task ExecuteResultAsyncShouldNameTheUnresolvedWrapperType()
    {
        await using var provider = CreateServiceProvider();
        var actionResult = new LightActionResult(Result.Ok(), serializerOptions: CreateSerializerOptions());

        var act = async () => await actionResult.ExecuteResultAsync(CreateActionContext(provider));

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage($"Could not resolve 'JsonTypeInfo<{typeof(HttpResultForWriting)}>'*");
    }

    [Fact]
    public async Task ExecuteResultAsyncShouldNameTheUnresolvedGenericWrapperType()
    {
        await using var provider = CreateServiceProvider();
        var actionResult = new LightActionResult<string>(
            Result<string>.Ok("hello"),
            serializerOptions: CreateSerializerOptions()
        );

        var act = async () => await actionResult.ExecuteResultAsync(CreateActionContext(provider));

        await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage($"Could not resolve 'JsonTypeInfo<{typeof(HttpResultForWriting<string>)}>'*");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddPortableResultsForMvc();
        return services.BuildServiceProvider();
    }

    private static ActionContext CreateActionContext(IServiceProvider provider) =>
        new (
            new DefaultHttpContext
            {
                RequestServices = provider,
                Response = { Body = new MemoryStream() }
            },
            new RouteData(),
            new ActionDescriptor()
        );

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
