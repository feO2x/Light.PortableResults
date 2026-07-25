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
    // keeps the range 6 - 199 reserved for future primitive kinds and is visible to callers that persisted or
    // transmitted it, and its classification.
    private static readonly Dictionary<MetadataKind, (byte Value, bool IsPrimitive)> ExpectedMembers = new ()
    {
        [MetadataKind.Null] = (0, true),
        [MetadataKind.Boolean] = (1, true),
        [MetadataKind.Int64] = (2, true),
        [MetadataKind.Double] = (3, true),
        [MetadataKind.String] = (4, true),
        [MetadataKind.Decimal] = (5, true),
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
}
