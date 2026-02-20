using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.CloudEvents.Writing.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization;
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
        var options = provider.GetRequiredService<LightResultsCloudEventsWriteOptions>();

        options.Should().NotBeNull();
        options.Source.Should().BeNull();
        options.MetadataSerializationMode.Should().Be(MetadataSerializationMode.Always);
        options.ConversionService.Should().BeSameAs(DefaultCloudEventsAttributeConversionService.Instance);
        options.SerializerOptions.Should().BeSameAs(Module.DefaultSerializerOptions);
    }

    [Fact]
    public void AddDefaultLightResultsCloudEventWriteJsonConverters_ShouldRegisterEnvelopeConverters()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        serializerOptions.AddDefaultLightResultsCloudEventWriteJsonConverters();

        serializerOptions.Converters.Should()
           .ContainSingle(converter => converter is CloudEventEnvelopeForWritingJsonConverter);
        serializerOptions.Converters.Should()
           .ContainSingle(converter => converter is CloudEventEnvelopeForWritingJsonConverterFactory);
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeConversionService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("traceid"));
        services.AddLightResultsCloudEventAttributeConversionService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var converters = provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeConverter>>();

        converters.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventsAttributeConversionService>()
           .Should().BeOfType<DefaultCloudEventsAttributeConversionService>();
    }

    [Fact]
    public void AddLightResultsCloudEventAttributeConversionService_ShouldThrow_WhenDuplicateKeysExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("duplicate"));
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("duplicate"));
        services.AddLightResultsCloudEventAttributeConversionService();

        using var provider = services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeConverter>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'");
    }

    private sealed class TestConverter : CloudEventsAttributeConverter
    {
        public TestConverter(string metadataKey) : base(ImmutableArray.Create(metadataKey)) { }

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new (metadataKey, value);
    }
}
