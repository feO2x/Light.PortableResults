using System;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class MetadataExtensionsTests
{
    [Fact]
    public void ClearMetadata_OnResultWithMetadata_ShouldRemoveMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("key", "value"));

        var cleared = result.ClearMetadata();

        cleared.Metadata.Should().BeNull();
        cleared.Value.Should().Be(42);
    }

    [Fact]
    public void ClearMetadata_OnResultWithoutMetadata_ShouldReturnResultWithNullMetadata()
    {
        var result = Result<int>.Ok(42);

        var cleared = result.ClearMetadata();

        cleared.Metadata.Should().BeNull();
        cleared.Value.Should().Be(42);
    }

    [Fact]
    public void ClearMetadata_OnFailedResult_ShouldPreserveErrors()
    {
        var error = new Error { Message = "Test error" };
        var result = Result<int>.Fail(error).MergeMetadata(("context", "test"));

        var cleared = result.ClearMetadata();

        cleared.IsValid.Should().BeFalse();
        cleared.Metadata.Should().BeNull();
        cleared.Errors.Should().ContainSingle().Which.Message.Should().Be("Test error");
    }

    [Fact]
    public void ClearMetadata_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok().MergeMetadata(("key", "value"));

        var cleared = result.ClearMetadata();

        cleared.Metadata.Should().BeNull();
        cleared.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MergeMetadata_WithProperties_ShouldAddToExistingMetadata()
    {
        var result = Result<string>.Ok("test").MergeMetadata(("a", 1));

        var merged = result.MergeMetadata(("b", 2), ("c", 3));

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().HaveCount(3);
        merged.Metadata.Value.TryGetInt64("a", out var a).Should().BeTrue();
        a.Should().Be(1);
        merged.Metadata.Value.TryGetInt64("b", out var b).Should().BeTrue();
        b.Should().Be(2);
        merged.Metadata.Value.TryGetInt64("c", out var c).Should().BeTrue();
        c.Should().Be(3);
    }

    [Fact]
    public void MergeMetadata_WithProperties_OnNullMetadata_ShouldCreateMetadata()
    {
        var result = Result<int>.Ok(42);

        var merged = result.MergeMetadata(("key", "value"));

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().ContainSingle();
        merged.Metadata.Value.TryGetString("key", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public void MergeMetadata_WithMetadataObject_ShouldCombineMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("existing", "old"));
        var additional = MetadataObject.Create(("new", "value"));

        var merged = result.MergeMetadata(additional);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().HaveCount(2);
        merged.Metadata.Value.TryGetString("existing", out var existing).Should().BeTrue();
        existing.Should().Be("old");
        merged.Metadata.Value.TryGetString("new", out var newVal).Should().BeTrue();
        newVal.Should().Be("value");
    }

    [Fact]
    public void MergeMetadata_WithMetadataObject_OnNullMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var merged = result.MergeMetadata(metadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void MergeMetadata_WithStrategy_AddOrReplace_ShouldReplaceExistingKeys()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("key", "old"));
        var additional = MetadataObject.Create(("key", "new"));

        var merged = result.MergeMetadata(additional);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().ContainSingle();
        merged.Metadata.Value.TryGetString("key", out var value).Should().BeTrue();
        value.Should().Be("new");
    }

    [Fact]
    public void MergeMetadata_OnNonGenericResult_ShouldWork()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));

        var merged = result.MergeMetadata(("b", 2));

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().HaveCount(2);
    }

    [Fact]
    public void ReplaceMetadata_WithNull_ShouldClearMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("key", "value"));

        var replaced = result.ReplaceMetadata(null);

        replaced.Metadata.Should().BeNull();
        replaced.Value.Should().Be(42);
    }

    [Fact]
    public void ReplaceMetadata_WithMetadata_ShouldReplaceCompletely()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("old", "value"));
        var newMetadata = MetadataObject.Create(("new", "data"));

        var replaced = result.ReplaceMetadata(newMetadata);

        replaced.Metadata.Should().NotBeNull();
        replaced.Metadata!.Value.Should().ContainSingle();
        replaced.Metadata.Value.TryGetString("new", out var value).Should().BeTrue();
        value.Should().Be("data");
        replaced.Metadata.Value.TryGetString("old", out _).Should().BeFalse();
    }

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
