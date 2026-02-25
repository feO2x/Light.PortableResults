using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.PortableResults.Tests.Http.Writing;

public sealed class ModuleTests
{
    [Fact]
    public void AddPortableResultsHttpHeaderConversionService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("x-trace"));
        services.AddPortableResultsHttpHeaderConversionService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var converters = provider.GetRequiredService<FrozenDictionary<string, HttpHeaderConverter>>();

        converters.ContainsKey("X-TRACE").Should().BeTrue();
    }

    [Fact]
    public void AddPortableResultsHttpHeaderConversionService_ShouldThrow_WhenDuplicateKeysExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("duplicate"));
        services.AddSingleton<HttpHeaderConverter>(new TestHeaderConverter("duplicate"));
        services.AddPortableResultsHttpHeaderConversionService();

        using var provider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Action act = () => provider.GetRequiredService<FrozenDictionary<string, HttpHeaderConverter>>();
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'*");
    }

    [Fact]
    public void AddDefaultPortableResultsJsonConverters_ShouldThrow_WhenSerializerOptionsIsNull()
    {
        var act = () => Module.AddDefaultPortableResultsHttpWriteJsonConverters(null!);

        act.Should().Throw<ArgumentNullException>().Where(x => x.ParamName == "serializerOptions");
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
