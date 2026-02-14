using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Writing;

public sealed class CloudEventAttributeConverterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenSupportedMetadataKeysAreDefaultOrEmpty()
    {
        Action actWithDefault = () => _ = new TestConverter(default);
        Action actWithEmpty = () => _ = new TestConverter(ImmutableArray<string>.Empty);

        actWithDefault.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "supportedMetadataKeys");
        actWithEmpty.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "supportedMetadataKeys");
    }

    [Fact]
    public void Constructor_ShouldExposeSupportedMetadataKeys()
    {
        var converter = new TestConverter(ImmutableArray.Create("traceId", "requestId"));

        converter.SupportedMetadataKeys.Should().Equal("traceId", "requestId");
    }

    [Fact]
    public void PrepareCloudEventAttribute_ShouldReturnConvertedPair()
    {
        var converter = new TestConverter(ImmutableArray.Create("traceId"));

        var pair = converter.PrepareCloudEventAttribute("traceId", MetadataValue.FromString("abc"));

        pair.Key.Should().Be("ce-traceId");
        pair.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
    }

    private sealed class TestConverter : CloudEventAttributeConverter
    {
        public TestConverter(ImmutableArray<string> supportedMetadataKeys) : base(supportedMetadataKeys) { }

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new ($"ce-{metadataKey}", value);
    }
}
