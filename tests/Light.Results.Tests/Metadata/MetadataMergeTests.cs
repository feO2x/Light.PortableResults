using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataMergeTests
{
    [Fact]
    public void Merge_EmptyIncoming_ShouldReturnOriginal()
    {
        var original = MetadataObject.Create(("key", "value"));
        var incoming = MetadataObject.Empty;

        var result = original.Merge(incoming);

        result.Should().Equal(original);
    }

    [Fact]
    public void Merge_EmptyOriginal_ShouldReturnIncoming()
    {
        var original = MetadataObject.Empty;
        var incoming = MetadataObject.Create(("key", "value"));

        var result = original.Merge(incoming);

        result.Should().Equal(incoming);
    }

    [Fact]
    public void Merge_AddOrReplace_ShouldAddNewKeys()
    {
        var original = MetadataObject.Create(("a", 1));
        var incoming = MetadataObject.Create(("b", 2));

        var result = original.Merge(incoming);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        result.Should().HaveCount(2);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Merge_AddOrReplace_ShouldReplaceExistingKeys()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var result = original.Merge(incoming);

        result.Should().ContainSingle();
        result.Should().Equal(incoming);
    }

    [Fact]
    public void Merge_AddOrReplace_ShouldMergeNestedObjects()
    {
        var originalNested = MetadataObject.Create(("inner1", "a"), ("inner2", "b"));
        var original = MetadataObject.Create(("nested", originalNested));

        var incomingNested = MetadataObject.Create(("inner2", "c"), ("inner3", "d"));
        var incoming = MetadataObject.Create(("nested", incomingNested));

        var result = original.Merge(incoming);

        result.TryGetObject("nested", out var nested).Should().BeTrue();
        nested.Should().HaveCount(3);
        nested.TryGetString("inner1", out var v1).Should().BeTrue();
        v1.Should().Be("a");
        nested.TryGetString("inner2", out var v2).Should().BeTrue();
        v2.Should().Be("c");
        nested.TryGetString("inner3", out var v3).Should().BeTrue();
        v3.Should().Be("d");
    }

    [Fact]
    public void Merge_AddOrReplace_ShouldReplaceArraysWholesale()
    {
        var original = MetadataObject.Create(("items", MetadataArray.Create(1, 2, 3)));
        var incoming = MetadataObject.Create(("items", MetadataArray.Create(4, 5)));

        var result = original.Merge(incoming);

        result.TryGetArray("items", out var items).Should().BeTrue();
        items.Should().HaveCount(2);
        items[0].TryGetInt64(out var v0).Should().BeTrue();
        v0.Should().Be(4);
        items[1].TryGetInt64(out var v1).Should().BeTrue();
        v1.Should().Be(5);
    }

    [Fact]
    public void Merge_PreserveExisting_ShouldKeepOriginalValues()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var result = original.Merge(incoming, MetadataMergeStrategy.PreserveExisting);

        result.TryGetInt64("key", out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void Merge_PreserveExisting_ShouldAddNewKeys()
    {
        var original = MetadataObject.Create(("a", 1));
        var incoming = MetadataObject.Create(("b", 2));

        var result = original.Merge(incoming, MetadataMergeStrategy.PreserveExisting);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        result.Should().Equal(expected);
    }

    [Fact]
    public void Merge_FailOnConflict_ShouldThrowOnDuplicate()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var act = () => original.Merge(incoming, MetadataMergeStrategy.FailOnConflict);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Merge_FailOnConflict_ShouldSucceedWithoutConflict()
    {
        var original = MetadataObject.Create(("a", 1));
        var incoming = MetadataObject.Create(("b", 2));

        var result = original.Merge(incoming, MetadataMergeStrategy.FailOnConflict);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void With_SingleProperty_ShouldAddOrReplace()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With("b", 2);

        result.Should().HaveCount(2);
        result.TryGetInt64("b", out var b).Should().BeTrue();
        b.Should().Be(2);
    }

    [Fact]
    public void With_MultipleProperties_ShouldAddAll()
    {
        var obj = MetadataObject.Empty;

        var result = obj.With(("a", 1), ("b", 2), ("c", 3));

        result.Should().HaveCount(3);
    }

    [Fact]
    public void With_EmptyProperties_ShouldReturnSame()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With();

        result.Should().BeEquivalentTo(obj);
    }
}
