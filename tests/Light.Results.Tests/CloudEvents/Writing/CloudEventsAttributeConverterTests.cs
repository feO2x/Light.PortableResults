using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Writing;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Writing;

public sealed class CloudEventsAttributeConverterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenSupportedMetadataKeysAreDefaultOrEmpty()
    {
        Action actWithDefault = () => _ = new TestConverter(default);
        Action actWithEmpty = () => _ = new TestConverter(ImmutableArray<string>.Empty);

        actWithDefault.Should().Throw<ArgumentException>()
           .Where(exception => exception.ParamName == "supportedMetadataKeys");
        actWithEmpty.Should().Throw<ArgumentException>()
           .Where(exception => exception.ParamName == "supportedMetadataKeys");
    }

    [Fact]
    public void Constructor_ShouldExposeSupportedMetadataKeys()
    {
        var converter = new TestConverter(["traceId", "requestId"]);

        converter.SupportedMetadataKeys.Should().Equal("traceId", "requestId");
    }

    [Fact]
    public void PrepareCloudEventsAttribute_ShouldReturnConvertedPair()
    {
        var converter = new TestConverter(["traceId"]);

        var pair = converter.PrepareCloudEventsAttribute("traceId", MetadataValue.FromString("abc"));

        pair.Key.Should().Be("ce-traceId");
        pair.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
    }

    private sealed class TestConverter : CloudEventsAttributeConverter
    {
        public TestConverter(ImmutableArray<string> supportedMetadataKeys) : base(supportedMetadataKeys) { }

        public override KeyValuePair<string, MetadataValue> PrepareCloudEventsAttribute(
            string metadataKey,
            MetadataValue value
        ) =>
            new ($"ce-{metadataKey}", value);
    }
}
