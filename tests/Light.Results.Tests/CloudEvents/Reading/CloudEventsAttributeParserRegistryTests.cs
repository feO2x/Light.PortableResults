using System;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class CloudEventsAttributeParserRegistryTests
{
    [Fact]
    public void Create_ShouldThrow_WhenParsersAreNull()
    {
        Action act = () => _ = CloudEventsAttributeParserRegistry.Create(null!);

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "parsers");
    }

    [Fact]
    public void Create_ShouldBuildRegistryAndRespectComparer()
    {
        var parser = new TestParser("traceId", ["traceid"]);

        var registry = CloudEventsAttributeParserRegistry.Create([parser], StringComparer.OrdinalIgnoreCase);

        registry.ContainsKey("TRACEID").Should().BeTrue();
        registry["TRACEID"].Should().BeSameAs(parser);
    }

    [Fact]
    public void Create_ShouldThrow_WhenDuplicateAttributeNamesExist()
    {
        var parser1 = new TestParser("traceId", ["traceid"]);
        var parser2 = new TestParser("requestId", ["traceid"]);

        Action act = () => _ = CloudEventsAttributeParserRegistry.Create([parser1, parser2]);

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add '*traceid*'");
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
