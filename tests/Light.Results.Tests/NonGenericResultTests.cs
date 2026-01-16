using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class NonGenericResultTests
{
    [Fact]
    public void IsFailure_OnFailure_ShouldBeTrue()
    {
        var result = Result.Fail(new Error { Message = "Error" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Errors_OnFailure_ShouldContainErrors()
    {
        var result = Result.Fail(new Error { Message = "Error" });

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Fail_WithMultipleErrors_ShouldWork()
    {
        var result = Result.Fail(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Ok_WithMetadata_ShouldSetMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result.Ok(metadata);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void WithMetadata_Object_ShouldSetMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result.Ok();

        var withMeta = result.ReplaceMetadata(metadata);

        withMeta.Metadata.Should().NotBeNull();
        withMeta.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void MergeMetadata_ShouldMergeCorrectly()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));
        var additional = MetadataObject.Create(("b", 2));

        var merged = result.MergeMetadata(additional);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void MergeMetadata_WithStrategy_ShouldUseStrategy()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));
        var additional = MetadataObject.Create(("a", 2));

        var merged = result.MergeMetadata(additional, MetadataMergeStrategy.PreserveExisting);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.TryGetInt64("a", out var value).Should().BeTrue();
        value.Should().Be(1);
    }
}
