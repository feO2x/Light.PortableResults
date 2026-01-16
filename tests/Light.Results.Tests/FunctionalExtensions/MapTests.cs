using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class MapTests
{
    [Fact]
    public void Map_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Ok(42);

        var mapped = result.Map(x => x.ToString());

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var mapped = result.Map(x => x.ToString());

        mapped.IsValid.Should().BeFalse();
        mapped.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Map_OnSuccess_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Ok(42, metadata);

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeTrue();
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Map_OnFailure_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Fail(new Error { Message = "Error" }, metadata);

        var mapped = result.Map(x => x.ToString());

        mapped.IsValid.Should().BeFalse();
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public async Task MapAsync_OnSuccess_ShouldTransformValue()
    {
        var result = Result<int>.Ok(42);

        var mapped = await result.MapAsync(x => new ValueTask<string>(x.ToString()));

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public async Task MapAsync_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var mapped = await result.MapAsync(x => new ValueTask<string>(x.ToString()));

        mapped.IsValid.Should().BeFalse();
        mapped.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task MapAsync_OnSuccess_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Ok(42, metadata);

        var mapped = await result.MapAsync(x => new ValueTask<int>(x * 2));

        mapped.IsValid.Should().BeTrue();
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }
}
