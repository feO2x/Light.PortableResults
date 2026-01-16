using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

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
}
