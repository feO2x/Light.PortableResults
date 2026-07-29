using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class MetadataKindTests
{
    // MetadataKindExtensions.IsPrimitive decides membership of the primitive set purely by ordering
    // (kind < MetadataKind.Array). A new member declared on the wrong side of that boundary compiles cleanly
    // and silently changes behavior, thus every declared member is pinned here - both its numeric value, which
    // keeps the range 16 - 199 reserved for future primitive kinds and is visible to callers that persisted or
    // transmitted it, and its classification.
    private static readonly Dictionary<MetadataKind, (byte Value, bool IsPrimitive)> ExpectedMembers = new ()
    {
        [MetadataKind.Null] = (0, true),
        [MetadataKind.Boolean] = (1, true),
        [MetadataKind.Int64] = (2, true),
        [MetadataKind.Double] = (3, true),
        [MetadataKind.String] = (4, true),
        [MetadataKind.Decimal] = (5, true),
        [MetadataKind.UInt64] = (6, true),
        [MetadataKind.Single] = (7, true),
        [MetadataKind.Char] = (8, true),
        [MetadataKind.DateTime] = (9, true),
        [MetadataKind.DateTimeOffset] = (10, true),
        [MetadataKind.DateOnly] = (11, true),
        [MetadataKind.TimeOnly] = (12, true),
        [MetadataKind.TimeSpan] = (13, true),
        [MetadataKind.Guid] = (14, true),
        [MetadataKind.Uri] = (15, true),
        [MetadataKind.Array] = (200, false),
        [MetadataKind.Object] = (201, false)
    };

    [Fact]
    public void EveryDeclaredKindShouldBePinned()
    {
        Enum.GetValues<MetadataKind>().Should().BeEquivalentTo(ExpectedMembers.Keys);
    }

    [Fact]
    public void EveryDeclaredKindShouldKeepItsNumericValue()
    {
        foreach (var kind in Enum.GetValues<MetadataKind>())
        {
            ExpectedMembers.Should().ContainKey(kind);
            ((byte) kind)
               .Should()
               .Be(
                    ExpectedMembers[kind].Value,
                    "the numeric value of '{0}' must not change silently",
                    kind
                );
        }
    }

    [Fact]
    public void EveryDeclaredKindShouldBeClassifiedCorrectly()
    {
        foreach (var kind in Enum.GetValues<MetadataKind>())
        {
            ExpectedMembers.Should().ContainKey(kind);
            kind.IsPrimitive()
               .Should()
               .Be(
                    ExpectedMembers[kind].IsPrimitive,
                    "the classification of '{0}' must not change silently",
                    kind
                );
        }
    }

    [Theory]
    [InlineData(MetadataKind.Null, MetadataJsonShape.Null)]
    [InlineData(MetadataKind.Boolean, MetadataJsonShape.Boolean)]
    [InlineData(MetadataKind.Int64, MetadataJsonShape.Number)]
    [InlineData(MetadataKind.Double, MetadataJsonShape.Number)]
    [InlineData(MetadataKind.String, MetadataJsonShape.String)]
    [InlineData(MetadataKind.Decimal, MetadataJsonShape.Number)]
    [InlineData(MetadataKind.UInt64, MetadataJsonShape.String)]
    [InlineData(MetadataKind.Single, MetadataJsonShape.Number)]
    [InlineData(MetadataKind.Char, MetadataJsonShape.String)]
    [InlineData(MetadataKind.DateTime, MetadataJsonShape.String)]
    [InlineData(MetadataKind.DateTimeOffset, MetadataJsonShape.String)]
    [InlineData(MetadataKind.DateOnly, MetadataJsonShape.String)]
    [InlineData(MetadataKind.TimeOnly, MetadataJsonShape.String)]
    [InlineData(MetadataKind.TimeSpan, MetadataJsonShape.String)]
    [InlineData(MetadataKind.Guid, MetadataJsonShape.String)]
    [InlineData(MetadataKind.Uri, MetadataJsonShape.String)]
    [InlineData(MetadataKind.Array, MetadataJsonShape.Array)]
    [InlineData(MetadataKind.Object, MetadataJsonShape.Object)]
    public void EveryDeclaredKindShouldHaveTheExpectedJsonShape(
        MetadataKind kind,
        MetadataJsonShape expectedShape
    )
    {
        kind.GetJsonShape().Should().Be(expectedShape);
    }

    // Every kind with the Number shape must name the primitive it is written from, and no other kind may.
    // A mismatch between the two classifications would reach the JSON writer as an unwritable number.
    [Theory]
    [InlineData(MetadataKind.Null, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Boolean, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Int64, MetadataNumberEncoding.Int64)]
    [InlineData(MetadataKind.Double, MetadataNumberEncoding.Double)]
    [InlineData(MetadataKind.String, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Decimal, MetadataNumberEncoding.Decimal)]
    [InlineData(MetadataKind.UInt64, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Single, MetadataNumberEncoding.Single)]
    [InlineData(MetadataKind.Char, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.DateTime, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.DateTimeOffset, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.DateOnly, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.TimeOnly, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.TimeSpan, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Guid, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Uri, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Array, MetadataNumberEncoding.None)]
    [InlineData(MetadataKind.Object, MetadataNumberEncoding.None)]
    public void EveryDeclaredKindShouldHaveTheExpectedNumberEncoding(
        MetadataKind kind,
        MetadataNumberEncoding expectedEncoding
    )
    {
        kind.GetNumberEncoding().Should().Be(expectedEncoding);

        var hasNumberShape = kind.GetJsonShape() == MetadataJsonShape.Number;
        (expectedEncoding != MetadataNumberEncoding.None).Should().Be(hasNumberShape);
    }
}
