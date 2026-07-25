using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class MetadataKindTests
{
    // MetadataKindExtensions.IsPrimitive decides membership of the primitive set purely by ordering
    // (kind < MetadataKind.Array). A new member declared on the wrong side of that boundary compiles cleanly
    // and silently changes behavior, thus every declared member is pinned to its expected classification here.
    private static readonly Dictionary<MetadataKind, bool> ExpectedClassifications = new ()
    {
        [MetadataKind.Null] = true,
        [MetadataKind.Boolean] = true,
        [MetadataKind.Int64] = true,
        [MetadataKind.Double] = true,
        [MetadataKind.String] = true,
        [MetadataKind.Decimal] = true,
        [MetadataKind.Array] = false,
        [MetadataKind.Object] = false
    };

    [Fact]
    public void EveryDeclaredKindShouldHaveAnExpectedClassification()
    {
        Enum.GetValues<MetadataKind>().Should().BeEquivalentTo(ExpectedClassifications.Keys);
    }

    [Fact]
    public void EveryDeclaredKindShouldBeClassifiedCorrectly()
    {
        foreach (var kind in Enum.GetValues<MetadataKind>())
        {
            ExpectedClassifications.Should().ContainKey(kind);
            kind.IsPrimitive()
               .Should()
               .Be(
                    ExpectedClassifications[kind],
                    "the classification of '{0}' must not change silently",
                    kind
                );
        }
    }

    // The reserved range only pays off if no primitive kind is declared at 200 or above and no complex kind
    // below 200 - the gap itself cannot enforce that.
    [Fact]
    public void PrimitiveKindsShouldStayWithinTheReservedRange()
    {
        var primitiveKinds = Enum.GetValues<MetadataKind>().Where(static kind => kind.IsPrimitive());

        primitiveKinds.Should().OnlyContain(kind => (byte) kind < 200);
    }

    [Fact]
    public void ComplexKindsShouldStartAtTheReservedBoundary()
    {
        var complexKinds = Enum.GetValues<MetadataKind>().Where(static kind => !kind.IsPrimitive());

        complexKinds.Should().OnlyContain(kind => (byte) kind >= 200);
    }
}
