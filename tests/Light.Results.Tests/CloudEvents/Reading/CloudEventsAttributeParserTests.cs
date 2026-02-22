using System;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class CloudEventsAttributeParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenMetadataKeyIsInvalid(string? metadataKey)
    {
        Action act = () => _ = new TestParser(metadataKey!, ImmutableArray.Create("traceid"));

        act.Should().Throw<ArgumentException>().Where(exception => exception.ParamName == "metadataKey");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSupportedAttributeNamesAreDefaultOrEmpty()
    {
        Action actWithDefault = () => _ = new TestParser("traceId", default);
        Action actWithEmpty = () => _ = new TestParser("traceId", ImmutableArray<string>.Empty);

        actWithDefault.Should().Throw<ArgumentException>()
           .Where(exception => exception.ParamName == "supportedAttributeNames");
        actWithEmpty.Should().Throw<ArgumentException>()
           .Where(exception => exception.ParamName == "supportedAttributeNames");
    }

    [Fact]
    public void Constructor_ShouldExposeMetadataKeyAndSupportedNames()
    {
        var parser = new TestParser("traceId", ImmutableArray.Create("traceid", "trace-id"));

        parser.MetadataKey.Should().Be("traceId");
        parser.SupportedAttributeNames.Should().Equal("traceid", "trace-id");
    }

    [Fact]
    public void ParseAttribute_ShouldReturnCustomMetadataValue()
    {
        var parser = new TestParser("traceId", ImmutableArray.Create("traceid"));

        var parsed = parser.ParseAttribute(
            "traceid",
            MetadataValue.FromString("abc"),
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        parsed.TryGetString(out var parsedString).Should().BeTrue();
        parsedString.Should().Be("traceid:traceId");
        parsed.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
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
            MetadataValue.FromString($"{attributeName}:{MetadataKey}", annotation);
    }
}
