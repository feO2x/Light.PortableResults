using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class BindTests
{
    [Fact]
    public void Bind_OnSuccess_ShouldChainToNewResult()
    {
        var result = Result<int>.Ok(42);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    [Fact]
    public void Bind_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeFalse();
        bound.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Bind_OnSuccess_WhenBindReturnsFailure_ShouldReturnFailure()
    {
        var result = Result<int>.Ok(42);

        var bound = result.Bind(_ => Result<string>.Fail(new Error { Message = "Inner error" }));

        bound.IsValid.Should().BeFalse();
        bound.FirstError.Message.Should().Be("Inner error");
    }

    [Fact]
    public void Bind_OnSuccess_WithMetadataOnOuter_ShouldMergeMetadata()
    {
        var outerMeta = MetadataObject.Create(("outer", "value"));
        var result = Result<int>.Ok(42, outerMeta);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value["outer"].Should().NotBeNull();
    }

    [Fact]
    public void Bind_OnSuccess_WithMetadataOnBoth_ShouldMergeMetadata()
    {
        var outerMeta = MetadataObject.Create(("outer", "value"));
        var innerMeta = MetadataObject.Create(("inner", "value"));
        var result = Result<int>.Ok(42, outerMeta);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString(), innerMeta));

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void Bind_OnFailure_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Fail(new Error { Message = "Error" }, metadata);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeFalse();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public async Task BindAsync_OnSuccess_ShouldChainToNewResult()
    {
        var result = Result<int>.Ok(42);

        var bound = await result.BindAsync(
            x =>
                new ValueTask<Result<string>>(Result<string>.Ok(x.ToString()))
        );

        bound.IsValid.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    [Fact]
    public async Task BindAsync_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var bound = await result.BindAsync(
            x =>
                new ValueTask<Result<string>>(Result<string>.Ok(x.ToString()))
        );

        bound.IsValid.Should().BeFalse();
        bound.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task BindAsync_OnSuccess_WithMetadataOnBoth_ShouldMergeMetadata()
    {
        var outerMeta = MetadataObject.Create(("outer", "value"));
        var innerMeta = MetadataObject.Create(("inner", "value"));
        var result = Result<int>.Ok(42, outerMeta);

        var bound = await result.BindAsync(
            x =>
                new ValueTask<Result<string>>(Result<string>.Ok(x.ToString(), innerMeta))
        );

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Count.Should().Be(2);
    }
}
