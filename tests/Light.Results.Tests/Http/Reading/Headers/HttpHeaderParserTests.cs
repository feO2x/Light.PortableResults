using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.Http.Reading.Headers;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Headers;

public sealed class HttpHeaderParserTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenMetadataKeyIsNullOrWhitespace()
    {
        Action nullAct = () => _ = new TestParser(null!, ["X-Test"]);
        Action whitespaceAct = () => _ = new TestParser(" ", ["X-Test"]);

        nullAct.Should().Throw<ArgumentException>();
        whitespaceAct.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSupportedHeaderNamesAreDefaultOrEmpty()
    {
        Action defaultAct = () => _ = new TestParser("traceId", default);
        Action emptyAct = () => _ = new TestParser("traceId", []);

        defaultAct.Should().Throw<ArgumentException>();
        emptyAct.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var parser = new TestParser("traceId", ["X-Trace", "X-Correlation"]);

        parser.MetadataKey.Should().Be("traceId");
        parser.SupportedHeaderNames.Should().Equal("X-Trace", "X-Correlation");
    }

    [Fact]
    public void ParseHeader_ShouldReturnMetadataValue()
    {
        var parser = new TestParser("traceId", ["X-Trace"]);

        var value = parser.ParseHeader(
            "X-Trace",
            ["abc"],
            MetadataValueAnnotation.SerializeInHttpHeader
        );

        value.Kind.Should().Be(MetadataKind.String);
        value.TryGetString(out var parsed).Should().BeTrue();
        parsed.Should().Be("abc");
        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
    }

    private sealed class TestParser : HttpHeaderParser
    {
        public TestParser(string metadataKey, ImmutableArray<string> supportedHeaderNames)
            : base(metadataKey, supportedHeaderNames) { }

        public override MetadataValue ParseHeader(
            string headerName,
            IReadOnlyList<string> values,
            MetadataValueAnnotation annotation
        ) =>
            MetadataValue.FromString(values[0], annotation);
    }
}
