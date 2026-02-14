using System;
using System.Collections.Frozen;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class ModuleTests
{
    [Fact]
    public void AddLightResultsCloudEventReadOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsCloudEventReadOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LightResultsCloudEventReadOptions>();

        options.Should().NotBeNull();
        options.PreferSuccessPayload.Should().Be(global::Light.Results.Http.Reading.Json.PreferSuccessPayload.Auto);
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeParsingService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventAttributeParser>(new TestParser("traceId", ImmutableArray.Create("traceid")));
        services.AddLightResultsCloudEventAttributeParsingService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FrozenDictionary<string, CloudEventAttributeParser>>();

        registry.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventAttributeParsingService>()
           .Should().BeOfType<DefaultCloudEventAttributeParsingService>();
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeParsingService_ShouldThrow_WhenDuplicateAttributesExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventAttributeParser>(new TestParser("a", ImmutableArray.Create("duplicate")));
        services.AddSingleton<CloudEventAttributeParser>(new TestParser("b", ImmutableArray.Create("duplicate")));
        services.AddLightResultsCloudEventAttributeParsingService();

        using var provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<FrozenDictionary<string, CloudEventAttributeParser>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'");
    }

    private sealed class TestParser : CloudEventAttributeParser
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
