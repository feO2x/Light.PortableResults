using System;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class DefaultCloudEventAttributeParsingServiceTests
{
    [Fact]
    public void Constructor_ShouldUseDefaultValues()
    {
        var service = new DefaultCloudEventAttributeParsingService();

        service.Parsers.Should().BeSameAs(DefaultCloudEventAttributeParsingService.EmptyParsers);
        service.ConflictStrategy.Should().Be(CloudEventAttributeConflictStrategy.Throw);
        service.MetadataAnnotation.Should().Be(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute);
    }

    [Fact]
    public void ParseExtensionAttribute_ShouldThrow_WhenAttributeNameIsNull()
    {
        var service = new DefaultCloudEventAttributeParsingService();

        Action act = () => service.ParseExtensionAttribute(
            null!,
            MetadataValue.FromString("value"),
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        act.Should().Throw<ArgumentNullException>().Where(exception => exception.ParamName == "attributeName");
    }

    [Fact]
    public void ParseExtensionAttribute_ShouldUseRegisteredParser()
    {
        var parser = new TestParser("traceId", ImmutableArray.Create("traceid"), "parsed");
        var parsers = CloudEventAttributeParserRegistry.Create([parser]);
        var service = new DefaultCloudEventAttributeParsingService(parsers);

        var result = service.ParseExtensionAttribute(
            "traceid",
            MetadataValue.FromString("value"),
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        result.Key.Should().Be("traceId");
        result.Value.TryGetString(out var parsed).Should().BeTrue();
        parsed.Should().Be("parsed:traceid");
        result.Value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void ParseExtensionAttribute_ShouldApplyProvidedAnnotation_ForPrimitiveValuesWithoutParser()
    {
        var service = new DefaultCloudEventAttributeParsingService();

        var result = service.ParseExtensionAttribute(
            "attempt",
            MetadataValue.FromInt64(3),
            MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
        );

        result.Key.Should().Be("attempt");
        result.Value.Kind.Should().Be(MetadataKind.Int64);
        result.Value.Annotation.Should().Be(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute);
        result.Value.TryGetInt64(out var attempt).Should().BeTrue();
        attempt.Should().Be(3);
    }

    [Fact]
    public void ParseExtensionAttribute_ShouldForceCloudEventDataAnnotation_ForComplexValuesWithoutParser()
    {
        var service = new DefaultCloudEventAttributeParsingService();
        var metadataObject = MetadataObject.Create(("child", MetadataValue.FromString("foo")));

        var result = service.ParseExtensionAttribute(
            "context",
            MetadataValue.FromObject(metadataObject),
            MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
        );

        result.Key.Should().Be("context");
        result.Value.Kind.Should().Be(MetadataKind.Object);
        result.Value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void ReadExtensionMetadata_ShouldReturnNull_WhenThereAreNoAttributes()
    {
        var service = new DefaultCloudEventAttributeParsingService();

        var metadata = service.ReadExtensionMetadata(MetadataObject.Empty);

        metadata.Should().BeNull();
    }

    [Fact]
    public void ReadExtensionMetadata_ShouldThrow_WhenConflictsOccurAndStrategyIsThrow()
    {
        var parser1 = new TestParser("traceId", ImmutableArray.Create("traceid"), "first");
        var parser2 = new TestParser("traceId", ImmutableArray.Create("requestid"), "second");
        var parsers = CloudEventAttributeParserRegistry.Create([parser1, parser2]);
        var service = new DefaultCloudEventAttributeParsingService(parsers);
        var extensionAttributes = MetadataObject.Create(
            ("traceid", MetadataValue.FromString("a")),
            ("requestid", MetadataValue.FromString("b"))
        );

        Action act = () => service.ReadExtensionMetadata(extensionAttributes);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already present*");
    }

    [Fact]
    public void ReadExtensionMetadata_ShouldUseLastWriteWins_WhenConfigured()
    {
        var parser1 = new TestParser("traceId", ImmutableArray.Create("traceid"), "first");
        var parser2 = new TestParser("traceId", ImmutableArray.Create("requestid"), "second");
        var parsers = CloudEventAttributeParserRegistry.Create([parser1, parser2]);
        var service = new DefaultCloudEventAttributeParsingService(
            parsers,
            CloudEventAttributeConflictStrategy.LastWriteWins,
            MetadataValueAnnotation.SerializeInCloudEventData
        );
        var extensionAttributes = MetadataObject.Create(
            ("traceid", MetadataValue.FromString("a")),
            ("requestid", MetadataValue.FromString("b"))
        );

        var metadata = service.ReadExtensionMetadata(extensionAttributes);

        metadata.Should().NotBeNull();
        metadata!.Value.TryGetString("traceId", out var value).Should().BeTrue();
        value.Should().Be("second:requestid");
        metadata.Value["traceId"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    private sealed class TestParser : CloudEventAttributeParser
    {
        private readonly string _prefix;

        public TestParser(string metadataKey, ImmutableArray<string> supportedAttributeNames, string prefix) :
            base(metadataKey, supportedAttributeNames) => _prefix = prefix;

        public override MetadataValue ParseAttribute(
            string attributeName,
            MetadataValue value,
            MetadataValueAnnotation annotation
        ) =>
            MetadataValue.FromString($"{_prefix}:{attributeName}", annotation);
    }
}
