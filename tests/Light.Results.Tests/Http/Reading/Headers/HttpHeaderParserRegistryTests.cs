using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.Http.Reading.Headers;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Headers;

public sealed class HttpHeaderParserRegistryTests
{
    [Fact]
    public void Create_ShouldThrow_WhenParsersAreNull()
    {
        Action act = () => HttpHeaderParserRegistry.Create(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldCreateCaseInsensitiveRegistryByDefault()
    {
        var parser = new TestParser("traceId", ["X-Trace"]);

        var registry = HttpHeaderParserRegistry.Create([parser]);

        registry.Should().ContainKey("x-trace");
        registry["x-trace"].Should().BeSameAs(parser);
    }

    [Fact]
    public void Create_ShouldHonorComparer()
    {
        var parser = new TestParser("traceId", ["X-Trace"]);

        var registry = HttpHeaderParserRegistry.Create([parser], StringComparer.Ordinal);

        registry.Should().ContainKey("X-Trace");
        registry.Should().NotContainKey("x-trace");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDuplicateHeaderNameIsRegistered()
    {
        var parser1 = new TestParser("traceId", ["X-Trace"]);
        var parser2 = new TestParser("correlationId", ["X-Trace"]);

        Action act = () => HttpHeaderParserRegistry.Create([parser1, parser2]);

        act.Should().Throw<InvalidOperationException>();
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
