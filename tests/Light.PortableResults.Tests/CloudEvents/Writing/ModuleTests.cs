using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.CloudEvents.Writing.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Writing;

public sealed class ModuleTests
{
    [Fact]
    public void AddPortableResultsCloudEventsWriteOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddPortableResultsCloudEventsWriteOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PortableResultsCloudEventsWriteOptions>();

        options.Should().NotBeNull();
        options.Source.Should().BeNull();
        options.MetadataSerializationMode.Should().Be(MetadataSerializationMode.Always);
        options.ConversionService.Should().BeSameAs(DefaultCloudEventsAttributeConversionService.Instance);
        options.SerializerOptions.Should().BeSameAs(Module.DefaultSerializerOptions);
    }

    [Fact]
    public void AddDefaultPortableResultsCloudEventsWriteJsonConverters_ShouldRegisterEnvelopeConverters()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();

        serializerOptions.Converters.Should()
           .ContainSingle(converter => converter is CloudEventsEnvelopeForWritingJsonConverter);
        serializerOptions.Converters.Should()
           .ContainSingle(converter => converter is CloudEventsEnvelopeForWritingJsonConverterFactory);
    }

    [Fact]
    public void AddPortableResultsCloudEventsAttributeConversionService_ShouldUseComparerForKeys()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("traceid"));
        services.AddPortableResultsCloudEventsAttributeConversionService(StringComparer.OrdinalIgnoreCase);

        using var provider = services.BuildServiceProvider();
        var converters = provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeConverter>>();

        converters.ContainsKey("TRACEID").Should().BeTrue();
        provider.GetRequiredService<ICloudEventsAttributeConversionService>()
           .Should().BeOfType<DefaultCloudEventsAttributeConversionService>();
    }

    [Fact]
    public void AddPortableResultsCloudEventsAttributeConversionService_ShouldThrow_WhenDuplicateKeysExist()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("duplicate"));
        services.AddSingleton<CloudEventsAttributeConverter>(new TestConverter("duplicate"));
        services.AddPortableResultsCloudEventsAttributeConversionService();

        using var provider = services.BuildServiceProvider();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Action act = () => provider.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeConverter>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*duplicate*'");
    }

    private sealed class TestConverter : CloudEventsAttributeConverter
    {
        public TestConverter(string metadataKey) : base(ImmutableArray.Create(metadataKey)) { }

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new (metadataKey, value);
    }
}
