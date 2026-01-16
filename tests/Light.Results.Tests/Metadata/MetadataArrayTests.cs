using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataArrayTests
{
    [Fact]
    public void Empty_ShouldHaveZeroCount()
    {
        var array = MetadataArray.Empty;

        array.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValues_ShouldStoreAll()
    {
        var array = MetadataArray.Create(1, 2, 3);

        array.Should().HaveCount(3);
        array[0].Should().Be(MetadataValue.FromInt64(1));
        array[1].Should().Be(MetadataValue.FromInt64(2));
        array[2].Should().Be(MetadataValue.FromInt64(3));
    }

    [Fact]
    public void Create_WithNull_ShouldReturnEmpty()
    {
        var array = MetadataArray.Create(null!);

        array.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyArray_ShouldReturnEmpty()
    {
        var array = MetadataArray.Create();

        array.Should().BeEmpty();
    }

    [Fact]
    public void Indexer_OutOfRange_ShouldThrow()
    {
        var array = MetadataArray.Create(1, 2);

        var act1 = () => array[2];
        var act2 = () => array[-1];

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Enumeration_ShouldIterateAllValues()
    {
        var array = MetadataArray.Create("a", "b", "c");
        var count = 0;

        foreach (var item in array)
        {
            count++;
            item.Kind.Should().Be(MetadataKind.String);
        }

        count.Should().Be(3);
    }

    [Fact]
    public void AsSpan_ShouldReturnAllValues()
    {
        var array = MetadataArray.Create(10, 20, 30);
        var span = array.AsSpan();

        span.Length.Should().Be(3);
        span[0].TryGetInt64(out var v0).Should().BeTrue();
        v0.Should().Be(10);
        span[1].TryGetInt64(out var v1).Should().BeTrue();
        v1.Should().Be(20);
        span[2].TryGetInt64(out var v2).Should().BeTrue();
        v2.Should().Be(30);
    }

    [Fact]
    public void ImplicitConversion_ToMetadataValue_ShouldWork()
    {
        var array = MetadataArray.Create(1, 2, 3);
        MetadataValue value = array;

        value.Kind.Should().Be(MetadataKind.Array);
        value.TryGetArray(out var result).Should().BeTrue();
        result.Should().HaveCount(3);
    }
}
