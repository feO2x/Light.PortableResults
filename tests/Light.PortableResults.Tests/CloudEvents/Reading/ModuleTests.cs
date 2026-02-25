using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Reading;

public sealed class ModuleTests
{
    [Fact]
    public void AddPortableResultsCloudEventsReadOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddPortableResultsCloudEventsReadOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PortableResultsCloudEventsReadOptions>();

        options.Should().NotBeNull();
        options.PreferSuccessPayload.Should().Be(PreferSuccessPayload.Auto);
    }

    [Fact]
    public void AddPortableResultsCloudEventsAttributeParsingService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("traceId", ["traceid"]));
        services.AddPortableResultsCloudEventsAttributeParsingService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeParser>>();

        registry.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventsAttributeParsingService>()
           .Should().BeOfType<DefaultCloudEventsAttributeParsingService>();
    }

    [Fact]
    public void AddPortableResultsCloudEventsAttributeParsingService_ShouldThrow_WhenDuplicateAttributesExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("a", ["duplicate"]));
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("b", ["duplicate"]));
        services.AddPortableResultsCloudEventsAttributeParsingService();

        using var provider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Action act = () => provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeParser>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'");
    }

    private sealed class TestParser : CloudEventsAttributeParser
    {
        public TestParser(string metadataKey, ImmutableArray<string> supportedAttributeNames) :
            base(metadataKey, supportedAttributeNames) { }

        public override MetadataValue ParseAttribute(
            string attributeName,
            MetadataValue value,
            MetadataValueAnnotation annotation
        ) =>
            MetadataValue.FromString(attributeName, annotation);
    }
}
