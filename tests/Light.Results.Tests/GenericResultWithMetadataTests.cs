using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class GenericResultWithMetadataTests
{
    [Fact]
    public void Ok_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var result = Result<int>.Ok(42);

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Ok_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("requestId", "req-123"));
        var result = Result<int>.Ok(42, metadata);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("requestId", out var id).Should().BeTrue();
        id.Should().Be("req-123");
    }

    [Fact]
    public void WithMetadata_Object_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var withMeta = result.ReplaceMetadata(metadata);

        withMeta.Metadata.Should().NotBeNull();
        withMeta.Metadata!.Value.Should().ContainSingle();
    }

    [Fact]
    public void WithMetadata_Properties_ShouldAddMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("a", 1), ("b", 2));

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().HaveCount(2);
    }

    [Fact]
    public void WithMetadata_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("key", "value"));

        result.IsValid.Should().BeFalse();
        result.Metadata.Should().NotBeNull();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void MergeMetadata_ShouldCombineMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("a", 1));

        var additional = MetadataObject.Create(("b", 2));
        var merged = result.MergeMetadata(additional);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().Equal(expected);
    }

    [Fact]
    public void MergeMetadata_OnNullMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var merged = result.MergeMetadata(metadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().ContainSingle();
        merged.Metadata.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Map_ShouldPreserveMetadata()
    {
        var result = Result<int>.Ok(10).MergeMetadata(("source", "test"));

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be(20);
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.TryGetString("source", out var source).Should().BeTrue();
        source.Should().Be("test");
    }

    [Fact]
    public void Map_OnFailure_ShouldPreserveMetadata()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "test"));

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeFalse();
        mapped.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Bind_ShouldMergeMetadata()
    {
        var result = Result<int>.Ok(10).MergeMetadata(("outer", "a"));

        var bound = result.Bind(
            x => Result<int>.Ok(x * 2).MergeMetadata(("inner", "b"))
        );

        bound.IsValid.Should().BeTrue();
        bound.Value.Should().Be(20);
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.TryGetString("outer", out var outer).Should().BeTrue();
        outer.Should().Be("a");
        bound.Metadata.Value.TryGetString("inner", out var inner).Should().BeTrue();
        inner.Should().Be("b");
    }

    [Fact]
    public void Bind_OnFailure_ShouldPreserveMetadata()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "test"));

        var bound = result.Bind(x => Result<int>.Ok(x * 2));

        bound.IsValid.Should().BeFalse();
        bound.Metadata.Should().NotBeNull();
    }
}
