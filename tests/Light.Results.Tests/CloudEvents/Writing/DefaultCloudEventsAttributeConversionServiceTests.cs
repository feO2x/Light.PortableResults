using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Writing;

public sealed class DefaultCloudEventsAttributeConversionServiceTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenConvertersAreNull()
    {
        Action act = () => _ = new DefaultCloudEventsAttributeConversionService(null!);

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "converters");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldThrow_WhenMetadataKeyIsNull()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());

        Action act = () => service.PrepareCloudEventsAttribute(null!, MetadataValue.FromString("value"));

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "metadataKey");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldUseRegisteredConverter()
    {
        var converter = new TestConverter(ImmutableArray.Create("traceId"), "traceid");
        var converters = new Dictionary<string, CloudEventsAttributeConverter>(StringComparer.Ordinal)
        {
            ["traceId"] = converter
        }.ToFrozenDictionary(StringComparer.Ordinal);
        var service = new DefaultCloudEventsAttributeConversionService(converters);

        var converted = service.PrepareCloudEventsAttribute("traceId", MetadataValue.FromString("abc"));

        converted.Key.Should().Be("traceid");
        converted.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldAllowStandardAttributesEvenForObjectValues()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());
        var objectValue = MetadataValue.FromObject(MetadataObject.Create(("inner", MetadataValue.FromString("value"))));

        var converted = service.PrepareCloudEventsAttribute("source", objectValue);

        converted.Key.Should().Be("source");
        converted.Value.Should().Be(objectValue);
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldThrow_WhenAttributeNameIsForbidden()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());

        Action act = () => service.PrepareCloudEventsAttribute("data", MetadataValue.FromString("value"));

        act.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "attributeName");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldThrow_WhenExtensionAttributeNameIsInvalid()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());

        Action act = () => service.PrepareCloudEventsAttribute("Trace-Id", MetadataValue.FromString("value"));

        act.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "attributeName");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldThrow_WhenExtensionValueIsComplex()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());
        var objectValue = MetadataValue.FromObject(MetadataObject.Create(("inner", MetadataValue.FromString("value"))));

        Action act = () => service.PrepareCloudEventsAttribute("traceid", objectValue);

        act.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "value");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldAllowPrimitiveExtensionValues()
    {
        var service = new DefaultCloudEventsAttributeConversionService(CreateEmptyConverters());

        var converted = service.PrepareCloudEventsAttribute("traceid", MetadataValue.FromInt64(7));

        converted.Key.Should().Be("traceid");
        converted.Value.Kind.Should().Be(MetadataKind.Int64);
    }

    [Fact]
    public void Instance_ShouldBeUsableWithoutCustomConverters()
    {
        var converted = DefaultCloudEventsAttributeConversionService.Instance.PrepareCloudEventsAttribute(
            "traceid",
            MetadataValue.FromString("abc")
        );

        converted.Key.Should().Be("traceid");
        converted.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
    }

    private static FrozenDictionary<string, CloudEventsAttributeConverter> CreateEmptyConverters()
    {
        return new Dictionary<string, CloudEventsAttributeConverter>(StringComparer.Ordinal).ToFrozenDictionary(
            StringComparer.Ordinal
        );
    }

    private sealed class TestConverter : CloudEventsAttributeConverter
    {
        private readonly string _targetAttributeName;

        public TestConverter(ImmutableArray<string> supportedMetadataKeys, string targetAttributeName) :
            base(supportedMetadataKeys) => _targetAttributeName = targetAttributeName;

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new (_targetAttributeName, value);
    }
}
