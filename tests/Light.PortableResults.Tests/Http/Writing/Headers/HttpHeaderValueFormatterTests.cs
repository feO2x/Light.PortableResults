using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Light.PortableResults.Tests.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Http.Writing.Headers;

public sealed class HttpHeaderValueFormatterTests
{
    [Fact]
    public void FormatShouldReturnCanonicalUnquotedTextForEveryNonNullPrimitiveKind()
    {
        var values = new (MetadataValue Value, MetadataKind Kind, string Expected)[]
        {
            (MetadataValue.FromBoolean(true), MetadataKind.Boolean, "true"),
            (MetadataValue.FromInt64(-42), MetadataKind.Int64, "-42"),
            (
                MetadataValue.FromDouble(BitConverter.Int64BitsToDouble(unchecked((long) 0x44B52D02C7E14AF6UL))),
                MetadataKind.Double,
                "1E+23"
            ),
            (MetadataValue.FromString("plain text"), MetadataKind.String, "plain text"),
            (MetadataValue.FromDecimal(19.50m), MetadataKind.Decimal, "19.50"),
            (MetadataValue.FromUInt64(ulong.MaxValue), MetadataKind.UInt64, "18446744073709551615"),
            (
                MetadataValue.FromSingle(BitConverter.Int32BitsToSingle(unchecked((int) 0x3EA90000U))),
                MetadataKind.Single,
                "0.33007812"
            ),
            (MetadataValue.FromChar('x'), MetadataKind.Char, "x"),
            (
                MetadataValue.FromDateTime(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc)),
                MetadataKind.DateTime,
                "2026-07-26T13:45:30Z"
            ),
            (
                MetadataValue.FromDateTimeOffset(
                    new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2))
                ),
                MetadataKind.DateTimeOffset,
                "2026-07-26T13:45:30+02:00"
            ),
            (
                MetadataValueTestFactory.CreateWithInt64Payload(MetadataKind.DateOnly, 0),
                MetadataKind.DateOnly,
                "0001-01-01"
            ),
            (
                MetadataValueTestFactory.CreateWithInt64Payload(
                    MetadataKind.TimeOnly,
                    new TimeSpan(13, 45, 30).Ticks
                ),
                MetadataKind.TimeOnly,
                "13:45:30"
            ),
            (MetadataValue.FromTimeSpan(TimeSpan.FromSeconds(5)), MetadataKind.TimeSpan, "PT5S"),
            (
                MetadataValue.FromGuid(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")),
                MetadataKind.Guid,
                "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
            ),
            (
                MetadataValue.FromUri(new Uri("https://example.com/items/42")),
                MetadataKind.Uri,
                "https://example.com/items/42"
            )
        };

        foreach (var (value, kind, expected) in values)
        {
            value.Kind.Should().Be(kind);

            var formatted = HttpHeaderValueFormatter.Format(value);

            formatted.Count.Should().Be(1, "{0} is a scalar kind", kind);
            formatted[0].Should().Be(expected, "the {0} header encoding is canonical", kind);
        }

        var array = MetadataValue.FromArray(MetadataArray.Create(values.Select(static item => item.Value).ToArray()));

        var formattedArray = HttpHeaderValueFormatter.Format(array);

        formattedArray.Should().Equal(values.Select(static item => item.Expected));
    }

    [Fact]
    public void FormatShouldReturnNoValuesForTopLevelNull()
    {
        var formatted = HttpHeaderValueFormatter.Format(MetadataValue.Null);

        formatted.Count.Should().Be(0);
    }

    [Fact]
    public void FormatShouldReturnNoValuesForEmptyArray()
    {
        var value = MetadataValue.FromArray(MetadataArray.Empty);

        var formatted = HttpHeaderValueFormatter.Format(value);

        formatted.Count.Should().Be(0);
    }

    [Fact]
    public void FormatShouldReturnOneValueForSingleItemArray()
    {
        var value = MetadataValue.FromArray(MetadataArray.Create(MetadataValue.FromString("only")));

        var formatted = HttpHeaderValueFormatter.Format(value);

        formatted.Count.Should().Be(1);
        formatted[0].Should().Be("only");
    }

    [Fact]
    public void FormatShouldPreserveOrderAndNullPositionsForMultiItemArray()
    {
        var value = MetadataValue.FromArray(
            MetadataArray.Create(
                MetadataValue.FromString("first"),
                MetadataValue.Null,
                MetadataValue.FromDouble(5),
                MetadataValue.FromString("last")
            )
        );

        var formatted = HttpHeaderValueFormatter.Format(value);

        formatted.Count.Should().Be(4);
        formatted.ToArray().Should().Equal("first", "null", "5.0", "last");
    }

    [Fact]
    public void FormatShouldRejectObject()
    {
        var value = MetadataValue.FromObject(MetadataObject.Create(("child", MetadataValue.FromInt64(1))));

        var act = () => HttpHeaderValueFormatter.Format(value);

        act.Should().ThrowExactly<NotSupportedException>().WithMessage("*Object*");
    }

    [Fact]
    public void FormatShouldRejectArraysContainingComplexChildren()
    {
        var complexChildren = new[]
        {
            MetadataValue.FromArray(MetadataArray.Create(MetadataValue.FromInt64(1))),
            MetadataValue.FromObject(MetadataObject.Create(("child", MetadataValue.FromInt64(1))))
        };

        foreach (var complexChild in complexChildren)
        {
            var value = MetadataValue.FromArray(MetadataArray.Create(MetadataValue.FromString("first"), complexChild));
            var act = () => HttpHeaderValueFormatter.Format(value);

            act.Should().ThrowExactly<NotSupportedException>().WithMessage($"*{complexChild.Kind}*");
        }
    }

    [Theory]
    [InlineData(HeaderValueParsingMode.Primitive)]
    [InlineData(HeaderValueParsingMode.StringOnly)]
    public void FormattedMultipleValuesShouldParseAsAnOrderedArray(HeaderValueParsingMode parsingMode)
    {
        var source = MetadataValue.FromArray(
            MetadataArray.Create(
                MetadataValue.FromBoolean(false),
                MetadataValue.FromInt64(7),
                MetadataValue.FromDouble(1.25),
                MetadataValue.FromString("text")
            )
        );
        var formatted = HttpHeaderValueFormatter.Format(source);
        var parser = new DefaultHttpHeaderParsingService(
            AllHeadersSelectionStrategy.Instance,
            headerValueParsingMode: parsingMode
        );
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpHeader;

        var parsed = parser.ParseHeader("X-Multi", formatted.Select(static value => value!).ToArray(), annotation);

        using var builder = MetadataArrayBuilder.Create(4);
        if (parsingMode == HeaderValueParsingMode.Primitive)
        {
            builder.Add(MetadataValue.FromBoolean(false, annotation));
            builder.Add(MetadataValue.FromInt64(7, annotation));
            builder.Add(MetadataValue.FromDouble(1.25, annotation));
            builder.Add(MetadataValue.FromString("text", annotation));
        }
        else
        {
            builder.Add(MetadataValue.FromString("false", annotation));
            builder.Add(MetadataValue.FromString("7", annotation));
            builder.Add(MetadataValue.FromString("1.25", annotation));
            builder.Add(MetadataValue.FromString("text", annotation));
        }

        parsed.Value.Should().Be(MetadataValue.FromArray(builder.Build(), annotation));
    }

    [Fact]
    public void FormattedSingleItemArrayShouldParseAsScalar()
    {
        var source = MetadataValue.FromArray(MetadataArray.Create(MetadataValue.FromString("only")));
        var formatted = HttpHeaderValueFormatter.Format(source);
        var parser = new DefaultHttpHeaderParsingService(AllHeadersSelectionStrategy.Instance);
        const MetadataValueAnnotation annotation = MetadataValueAnnotation.SerializeInHttpHeader;

        var parsed = parser.ParseHeader("X-Single", formatted.Select(static value => value!).ToArray(), annotation);

        parsed.Value.Should().Be(MetadataValue.FromString("only", annotation));
    }

    [Fact]
    public void FormatShouldThrowForUndeclaredMetadataKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();
        Action act = () => _ = HttpHeaderValueFormatter.Format(value);

#if TESTING_NETSTANDARD_ASSET
        act.Should().Throw<InvalidOperationException>();
#else
        act.Should().Throw<SwitchExpressionException>();
#endif
    }
}
