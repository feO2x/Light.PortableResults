using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class MapErrorTests
{
    [Fact]
    public void MapError_OnGenericResult_OnFailure_ShouldTransformErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Original", Code = "E001" });

        var mapped = result.MapError(e => new Error { Message = "Transformed: " + e.Message, Code = e.Code });

        mapped.IsValid.Should().BeFalse();
        mapped.FirstError.Message.Should().Be("Transformed: Original");
        mapped.FirstError.Code.Should().Be("E001");
    }

    [Fact]
    public void MapError_OnGenericResult_OnSuccess_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var mapped = result.MapError(_ => new Error { Message = "Transformed" });

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be(42);
    }

    [Fact]
    public void MapError_OnGenericResult_WithMultipleErrors_ShouldTransformAll()
    {
        var result = Result<int>.Fail(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });

        var mapped = result.MapError(e => new Error { Message = "T:" + e.Message });

        mapped.IsValid.Should().BeFalse();
        mapped.Errors.Should().HaveCount(2);
        mapped.Errors[0].Message.Should().Be("T:E1");
        mapped.Errors[1].Message.Should().Be("T:E2");
    }

    [Fact]
    public void MapError_OnGenericResult_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Fail(new Error { Message = "Error" }, metadata);

        var mapped = result.MapError(_ => new Error { Message = "Transformed" });

        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void MapError_OnNonGenericResult_OnFailure_ShouldTransformErrors()
    {
        var result = Result.Fail(new Error { Message = "Original" });

        var mapped = result.MapError(e => new Error { Message = "Transformed: " + e.Message });

        mapped.IsValid.Should().BeFalse();
        mapped.FirstError.Message.Should().Be("Transformed: Original");
    }

    [Fact]
    public void MapError_OnNonGenericResult_OnSuccess_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var mapped = result.MapError(_ => new Error { Message = "Transformed" });

        mapped.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task MapErrorAsync_OnGenericResult_OnFailure_ShouldTransformErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Original" });

        var mapped = await result.MapErrorAsync(
            e =>
                new ValueTask<Error>(new Error { Message = "Transformed: " + e.Message })
        );

        mapped.IsValid.Should().BeFalse();
        mapped.FirstError.Message.Should().Be("Transformed: Original");
    }

    [Fact]
    public async Task MapErrorAsync_OnGenericResult_OnSuccess_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var mapped = await result.MapErrorAsync(
            e =>
                new ValueTask<Error>(new Error { Message = "Transformed: " + e.Message })
        );

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be(42);
    }

    [Fact]
    public async Task MapErrorAsync_OnNonGenericResult_OnFailure_ShouldTransformErrors()
    {
        var result = Result.Fail(new Error { Message = "Original" });

        var mapped = await result.MapErrorAsync(
            e =>
                new ValueTask<Error>(new Error { Message = "Transformed: " + e.Message })
        );

        mapped.IsValid.Should().BeFalse();
        mapped.FirstError.Message.Should().Be("Transformed: Original");
    }

    [Fact]
    public async Task MapErrorAsync_OnNonGenericResult_OnSuccess_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var mapped = await result.MapErrorAsync(
            e =>
                new ValueTask<Error>(new Error { Message = "Transformed: " + e.Message })
        );

        mapped.IsValid.Should().BeTrue();
    }
}
