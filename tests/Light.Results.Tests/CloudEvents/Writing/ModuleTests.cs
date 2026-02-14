using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Writing;

public sealed class ModuleTests
{
    [Fact]
    public void AddLightResultsCloudEventWriteOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsCloudEventWriteOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LightResultsCloudEventWriteOptions>();

        options.Should().NotBeNull();
        options.Source.Should().BeNull();
        options.MetadataSerializationMode.Should().Be(SharedJsonSerialization.MetadataSerializationMode.Always);
        options.ConversionService.Should().BeSameAs(DefaultCloudEventAttributeConversionService.Instance);
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeConversionService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventAttributeConverter>(new TestConverter("traceid"));
        services.AddLightResultsCloudEventAttributeConversionService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var converters = provider.GetRequiredService<FrozenDictionary<string, CloudEventAttributeConverter>>();

        converters.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventAttributeConversionService>()
           .Should().BeOfType<DefaultCloudEventAttributeConversionService>();
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeConversionService_ShouldThrow_WhenDuplicateKeysExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventAttributeConverter>(new TestConverter("duplicate"));
        services.AddSingleton<CloudEventAttributeConverter>(new TestConverter("duplicate"));
        services.AddLightResultsCloudEventAttributeConversionService();

        using var provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<FrozenDictionary<string, CloudEventAttributeConverter>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'");
    }

    private sealed class TestConverter : CloudEventAttributeConverter
    {
        public TestConverter(string metadataKey) : base(ImmutableArray.Create(metadataKey)) { }

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new (metadataKey, value);
    }
}
