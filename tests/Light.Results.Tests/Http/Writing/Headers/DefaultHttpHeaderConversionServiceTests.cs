using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Http.Writing.Headers;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Light.Results.Tests.Http.Writing.Headers;

public sealed class DefaultHttpHeaderConversionServiceTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenConvertersAreNull()
    {
        Action act = () => _ = new DefaultHttpHeaderConversionService(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PrepareHttpHeader_ShouldUseRegisteredConverter()
    {
        var converter = new TraceIdConverter();
        var converters = new Dictionary<string, HttpHeaderConverter>(StringComparer.OrdinalIgnoreCase)
        {
            ["traceId"] = converter
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        var service = new DefaultHttpHeaderConversionService(converters);
        var metadataValue = MetadataValue.FromString("abc");

        var header = service.PrepareHttpHeader("traceId", metadataValue);

        header.Key.Should().Be("X-Trace-Id");
        header.Value.ToString().Should().Be("abc");
    }

    [Fact]
    public void PrepareHttpHeader_ShouldFallbackToMetadataKeyAndStringValue_WhenNoConverterIsRegistered()
    {
        var service = new DefaultHttpHeaderConversionService(
            new Dictionary<string, HttpHeaderConverter>().ToFrozenDictionary()
        );

        var header = service.PrepareHttpHeader("count", MetadataValue.FromInt64(42));

        header.Key.Should().Be("count");
        header.Value.ToString().Should().Be("42");
    }

    private sealed class TraceIdConverter : HttpHeaderConverter
    {
        public TraceIdConverter() : base(["traceId"]) { }

        public override KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value)
        {
            value.TryGetString(out var traceId);
            return new KeyValuePair<string, StringValues>("X-Trace-Id", traceId);
        }
    }
}
