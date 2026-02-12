using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Headers;
using Light.Results.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.Results.Tests.Http.Writing;

public sealed class ModuleTests
{
    [Fact]
    public void AddLightResultsHttpHeaderConversionService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("x-trace"));
        services.AddLightResultsHttpHeaderConversionService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var converters = provider.GetRequiredService<FrozenDictionary<string, HttpHeaderConverter>>();

        converters.ContainsKey("X-TRACE").Should().BeTrue();
    }

    [Fact]
    public void AddLightResultsHttpHeaderConversionService_ShouldThrow_WhenDuplicateKeysExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("duplicate"));
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("duplicate"));
        services.AddLightResultsHttpHeaderConversionService();

        using var provider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Action act = () => provider.GetRequiredService<FrozenDictionary<string, HttpHeaderConverter>>();
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'*");
    }

    [Fact]
    public void AddDefaultLightResultsJsonConverters_ShouldThrow_WhenSerializerOptionsIsNull()
    {
        var act = () => Module.AddDefaultLightResultsHttpWriteJsonConverters(null!, new LightResultsHttpWriteOptions());

        act.Should().Throw<ArgumentNullException>().Where(x => x.ParamName == "serializerOptions");
    }

    [Fact]
    public void AddDefaultLightResultsJsonConverters_ShouldThrow_WhenOptionsIsNull()
    {
        var act = () => new JsonSerializerOptions().AddDefaultLightResultsHttpWriteJsonConverters(null!);

        act.Should().Throw<ArgumentNullException>().Where(x => x.ParamName == "options");
    }

    private sealed class TestHeaderConverter : HttpHeaderConverter
    {
        public TestHeaderConverter(string metadataKey) : base([metadataKey]) { }

        public override KeyValuePair<string, StringValues> PrepareHttpHeader(
            string metadataKey,
            MetadataValue value
        ) =>
            new (metadataKey, value.ToString());
    }
}
