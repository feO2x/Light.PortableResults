using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Http.Reading.Headers;

public sealed class DefaultHttpHeaderParsingServiceTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenSelectionStrategyIsNull()
    {
        Action act = () => _ = new DefaultHttpHeaderParsingService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseHeader_ShouldUseRegisteredParser_AndMetadataKey()
    {
        var parser = new TestParser();
        var registry = HttpHeaderParserRegistry.Create([parser]);
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            registry
        );

        var parsed = service.ParseHeader(
            "X-TraceId",
            ["abc"],
            MetadataValueAnnotation.SerializeInHttpHeader
        );

        var expectedKvp = new KeyValuePair<string, MetadataValue>("traceId", MetadataValue.FromString("abc"));
        parsed.Should().Be(expectedKvp);
    }

    [Fact]
    public void ParseHeader_ShouldThrow_WhenValuesAreNull()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        Action act = () => service.ParseHeader(
            "X-Test",
            null!,
            MetadataValueAnnotation.SerializeInHttpHeader
        );

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ParseHeader_ShouldParseBooleanValue()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        var boolean = service.ParseHeader("X-Bool", ["true"], MetadataValueAnnotation.SerializeInHttpHeader);

        boolean.Value.TryGetBoolean(out var result).Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public void ParseHeader_ShouldParseIntegerValue()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        var integer = service.ParseHeader("X-Int", ["123"], MetadataValueAnnotation.SerializeInHttpHeader);

        integer.Value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(123);
    }

    [Fact]
    public void ParseHeader_ShouldParseDoubleValue()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        var doubleValue = service.ParseHeader("X-Double", ["3.5"], MetadataValueAnnotation.SerializeInHttpHeader);

        doubleValue.Value.TryGetDouble(out var result).Should().BeTrue();
        result.Should().Be(3.5);
    }

    [Fact]
    public void ParseHeader_ShouldPreserveNanStringValue()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        var nan = service.ParseHeader("X-NaN", ["NaN"], MetadataValueAnnotation.SerializeInHttpHeader);

        nan.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("NaN");
    }

    [Fact]
    public void ParseHeader_ShouldPreserveInfinityStringValue()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        var infinity = service.ParseHeader("X-Infinity", ["Infinity"], MetadataValueAnnotation.SerializeInHttpHeader);

        infinity.Value.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("Infinity");
    }

    [Fact]
    public void ParseHeader_ShouldParseMultipleValuesIntoMetadataArray()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers
        );

        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpHeader;
        var parsed = service.ParseHeader(
            "X-Multi",
            ["false", "7", "1.25", "text"],
            annotation
        );

        using var builder = MetadataArrayBuilder.Create();
        builder.Add(MetadataValue.FromBoolean(false, annotation));
        builder.Add(MetadataValue.FromInt64(7, annotation));
        builder.Add(MetadataValue.FromDouble(1.25, annotation));
        builder.Add(MetadataValue.FromString("text", annotation));
        var expected = new KeyValuePair<string, MetadataValue>(
            "X-Multi",
            MetadataValue.FromArray(builder.Build(), annotation)
        );
        parsed.Should().Be(expected);
    }

    [Fact]
    public void ParseHeader_ShouldPreserveStringValues_WhenStringOnlyModeIsUsed()
    {
        var service = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            DefaultHttpHeaderParsingService.EmptyParsers,
            headerValueParsingMode: HeaderValueParsingMode.StringOnly
        );

        var parsed = service.ParseHeader(
            "X-Int",
            ["123"],
            MetadataValueAnnotation.SerializeInHttpHeader
        );

        var expectedKvp = new KeyValuePair<string, MetadataValue>(
            "X-Int",
            MetadataValue.FromString("123", MetadataValueAnnotation.SerializeInHttpHeader)
        );
        parsed.Should().Be(expectedKvp);
    }

    private sealed class TestParser : HttpHeaderParser
    {
        public TestParser() : base("traceId", ImmutableArray.Create("X-TraceId")) { }

        public override MetadataValue ParseHeader(
            string headerName,
            IReadOnlyList<string> values,
            MetadataValueAnnotation annotation
        )
        {
            return MetadataValue.FromString(values[0], annotation);
        }
    }
}
