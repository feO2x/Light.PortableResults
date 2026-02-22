using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class ModuleTests
{
    [Fact]
    public void AddLightResultsCloudEventsReadOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsCloudEventsReadOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LightResultsCloudEventsReadOptions>();

        options.Should().NotBeNull();
        options.PreferSuccessPayload.Should().Be(PreferSuccessPayload.Auto);
    }

    [Fact]
    public void AddLightResultsCloudEventsAttributeParsingService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("traceId", ImmutableArray.Create("traceid")));
        services.AddLightResultsCloudEventsAttributeParsingService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeParser>>();

        registry.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventsAttributeParsingService>()
           .Should().BeOfType<DefaultCloudEventsAttributeParsingService>();
    }

    [Fact]
    public void AddLightResultsCloudEventsAttributeParsingService_ShouldThrow_WhenDuplicateAttributesExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("a", ImmutableArray.Create("duplicate")));
        services.AddSingleton<CloudEventsAttributeParser>(new TestParser("b", ImmutableArray.Create("duplicate")));
        services.AddLightResultsCloudEventsAttributeParsingService();

        using var provider = services.BuildServiceProvider();

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
