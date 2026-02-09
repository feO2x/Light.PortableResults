using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Light.Results.Http.Writing.Headers;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.Results.Tests.Http.Writing.Headers;

public sealed class HttpHeaderConverterTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenSupportedMetadataKeysAreDefaultOrEmpty()
    {
        Action defaultAct = () => _ = new TestConverter(default);
        Action emptyAct = () => _ = new TestConverter([]);

        defaultAct.Should().Throw<ArgumentException>();
        emptyAct.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldSetSupportedKeys()
    {
        var converter = new TestConverter(["traceId", "correlationId"]);

        converter.SupportedMetadataKeys.Should().Equal("traceId", "correlationId");
    }

    private sealed class TestConverter : HttpHeaderConverter
    {
        public TestConverter(ImmutableArray<string> supportedMetadataKeys)
            : base(supportedMetadataKeys) { }

        public override KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value) =>
            new (metadataKey, value.ToString());
    }
}
