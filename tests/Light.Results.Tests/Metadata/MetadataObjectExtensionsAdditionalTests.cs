using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataObjectExtensionsAdditionalTests
{
    [Fact]
    public void MergeIfNeeded_WithNullIncoming_ShouldReturnExisting()
    {
        var existing = MetadataObject.Create(("key", 1));

        var result = MetadataObjectExtensions.MergeIfNeeded(existing, null);

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(existing);
    }

    [Fact]
    public void MergeIfNeeded_WithEmptyIncoming_ShouldReturnExisting()
    {
        var existing = MetadataObject.Create(("key", 1));

        var result = MetadataObjectExtensions.MergeIfNeeded(existing, MetadataObject.Empty);

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(existing);
    }

    [Fact]
    public void MergeIfNeeded_WithNullExisting_ShouldReturnIncoming()
    {
        var incoming = MetadataObject.Create(("key", 1));

        var result = MetadataObjectExtensions.MergeIfNeeded(null, incoming);

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(incoming);
    }

    [Fact]
    public void MergeIfNeeded_WithEmptyExisting_ShouldReturnIncoming()
    {
        var incoming = MetadataObject.Create(("key", 1));

        var result = MetadataObjectExtensions.MergeIfNeeded(MetadataObject.Empty, incoming);

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(incoming);
    }

    [Fact]
    public void MergeIfNeeded_WithSameReference_ShouldReturnExisting()
    {
        var obj = MetadataObject.Create(("key", 1));

        var result = MetadataObjectExtensions.MergeIfNeeded(obj, obj);

        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(obj);
    }

    [Fact]
    public void Merge_WithEmptyIncoming_ShouldReturnOriginal()
    {
        var original = MetadataObject.Create(("key", 1));

        var result = original.Merge(MetadataObject.Empty);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Merge_WithEmptyOriginal_ShouldReturnIncoming()
    {
        var incoming = MetadataObject.Create(("key", 1));

        var result = MetadataObject.Empty.Merge(incoming);

        result.Should().BeEquivalentTo(incoming);
    }

    [Fact]
    public void Merge_WithSameReference_ShouldReturnOriginal()
    {
        var obj = MetadataObject.Create(("key", 1));

        var result = obj.Merge(obj);

        result.Should().BeEquivalentTo(obj);
    }

    [Fact]
    public void Merge_WithPreserveExistingStrategy_ShouldKeepOriginalValues()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var result = original.Merge(incoming, MetadataMergeStrategy.PreserveExisting);

        result.TryGetInt64("key", out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void Merge_WithFailOnConflictStrategy_ShouldThrow()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var act = () => original.Merge(incoming, MetadataMergeStrategy.FailOnConflict);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Duplicate metadata key*");
    }

    [Fact]
    public void Merge_WithInvalidStrategy_ShouldThrow()
    {
        var original = MetadataObject.Create(("key", 1));
        var incoming = MetadataObject.Create(("key", 2));

        var act = () => original.Merge(incoming, (MetadataMergeStrategy) 999);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("strategy");
    }

    [Fact]
    public void Merge_WithNestedObjects_ShouldMergeRecursively()
    {
        var innerOriginal = MetadataObject.Create(("nested", 1));
        var innerIncoming = MetadataObject.Create(("nested", 2), ("new", 3));
        var original = MetadataObject.Create(("obj", innerOriginal));
        var incoming = MetadataObject.Create(("obj", innerIncoming));

        var result = original.Merge(incoming);

        result.TryGetObject("obj", out var merged).Should().BeTrue();
        merged.TryGetInt64("nested", out var value).Should().BeTrue();
        value.Should().Be(2);
        merged.TryGetInt64("new", out var newValue).Should().BeTrue();
        newValue.Should().Be(3);
    }

    [Fact]
    public void With_SingleProperty_ShouldAddProperty()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With("b", 2);

        result.Should().HaveCount(2);
        result.ContainsKey("a").Should().BeTrue();
        result.ContainsKey("b").Should().BeTrue();
    }

    [Fact]
    public void With_MultipleProperties_ShouldAddAllProperties()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With(("b", 2), ("c", 3));

        result.Should().HaveCount(3);
    }

    [Fact]
    public void With_NullProperties_ShouldReturnOriginal()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With(null);

        result.Should().BeEquivalentTo(obj);
    }

    [Fact]
    public void With_EmptyProperties_ShouldReturnOriginal()
    {
        var obj = MetadataObject.Create(("a", 1));

        var result = obj.With();

        result.Should().BeEquivalentTo(obj);
    }
}
