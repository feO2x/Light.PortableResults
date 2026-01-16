using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class NonGenericResultMetadataTests
{
    [Fact]
    public void Ok_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var result = Result.Ok();

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Ok_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("requestId", "req-456"));
        var result = Result.Ok(metadata);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("requestId", out var id).Should().BeTrue();
        id.Should().Be("req-456");
    }

    [Fact]
    public void WithMetadata_ShouldSetMetadata()
    {
        var result = Result.Ok().MergeMetadata(("key", "value"));

        var expected = MetadataObject.Create(("key", "value"));
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(expected);
    }

    [Fact]
    public void MergeMetadata_ShouldCombineMetadata()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));

        var additional = MetadataObject.Create(("b", 2));
        var merged = result.MergeMetadata(additional);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().HaveCount(2);
        merged.Metadata.Value.Should().Equal(expected);
    }

    [Fact]
    public void Fail_WithMetadata_ShouldPreserveMetadata()
    {
        var result = Result.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "failure"));

        var expected = MetadataObject.Create(("context", "failure"));
        result.IsValid.Should().BeFalse();
        result.Metadata.Should().NotBeNull();
        result.Metadata.Value.Should().Equal(expected);
    }
}
