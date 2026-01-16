using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class FailIfTests
{
    [Fact]
    public void FailIf_OnGenericResult_WhenPredicateTrue_ShouldReturnFailure()
    {
        var result = Result<int>.Ok(42);

        var failed = result.FailIf(v => v > 40, new Error { Message = "Too large" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Too large");
    }

    [Fact]
    public void FailIf_OnGenericResult_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var failed = result.FailIf(v => v > 50, new Error { Message = "Too large" });

        failed.IsValid.Should().BeTrue();
        failed.Value.Should().Be(42);
    }

    [Fact]
    public void FailIf_OnGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var failed = result.FailIf(_ => true, new Error { Message = "New error" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public void FailIf_OnGenericResult_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Ok(42, metadata);

        var failed = result.FailIf(v => v > 40, new Error { Message = "Too large" });

        failed.Metadata.Should().NotBeNull();
        failed.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void FailIf_WithErrorFactory_ShouldUseFactory()
    {
        var result = Result<int>.Ok(42);

        var failed = result.FailIf(v => v > 40, v => new Error { Message = $"Value {v} is too large" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Value 42 is too large");
    }

    [Fact]
    public void FailIf_WithErrorFactory_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var failed = result.FailIf(v => v > 50, v => new Error { Message = $"Value {v} is too large" });

        failed.IsValid.Should().BeTrue();
        failed.Value.Should().Be(42);
    }

    [Fact]
    public void FailIf_WithErrorFactory_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var failed = result.FailIf(_ => true, _ => new Error { Message = "New error" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public void FailIf_OnNonGenericResult_WhenPredicateTrue_ShouldReturnFailure()
    {
        var result = Result.Ok();

        var failed = result.FailIf(() => true, new Error { Message = "Condition failed" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Condition failed");
    }

    [Fact]
    public void FailIf_OnNonGenericResult_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var failed = result.FailIf(() => false, new Error { Message = "Condition failed" });

        failed.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FailIf_OnNonGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result.Fail(new Error { Message = "Original error" });

        var failed = result.FailIf(() => true, new Error { Message = "New error" });

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task FailIfAsync_OnGenericResult_WhenPredicateTrue_ShouldReturnFailure()
    {
        var result = Result<int>.Ok(42);

        var failed = await result.FailIfAsync(
            v => new ValueTask<bool>(v > 40),
            new Error { Message = "Too large" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Too large");
    }

    [Fact]
    public async Task FailIfAsync_OnGenericResult_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var failed = await result.FailIfAsync(
            v => new ValueTask<bool>(v > 50),
            new Error { Message = "Too large" }
        );

        failed.IsValid.Should().BeTrue();
        failed.Value.Should().Be(42);
    }

    [Fact]
    public async Task FailIfAsync_WithErrorFactory_ShouldUseFactory()
    {
        var result = Result<int>.Ok(42);

        var failed = await result.FailIfAsync(
            v => new ValueTask<bool>(v > 40),
            v => new Error { Message = $"Value {v} is too large" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Value 42 is too large");
    }

    [Fact]
    public async Task FailIfAsync_WithErrorFactory_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var failed = await result.FailIfAsync(
            v => new ValueTask<bool>(v > 50),
            v => new Error { Message = $"Value {v} is too large" }
        );

        failed.IsValid.Should().BeTrue();
        failed.Value.Should().Be(42);
    }

    [Fact]
    public async Task FailIfAsync_WithErrorFactory_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var failed = await result.FailIfAsync(
            _ => new ValueTask<bool>(true),
            _ => new Error { Message = "New error" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task FailIfAsync_OnGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var failed = await result.FailIfAsync(
            _ => new ValueTask<bool>(true),
            new Error { Message = "New error" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task FailIfAsync_OnNonGenericResult_WhenPredicateTrue_ShouldReturnFailure()
    {
        var result = Result.Ok();

        var failed = await result.FailIfAsync(
            () => new ValueTask<bool>(true),
            new Error { Message = "Condition failed" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Condition failed");
    }

    [Fact]
    public async Task FailIfAsync_OnNonGenericResult_WhenPredicateFalse_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var failed = await result.FailIfAsync(
            () => new ValueTask<bool>(false),
            new Error { Message = "Condition failed" }
        );

        failed.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FailIfAsync_OnNonGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result.Fail(new Error { Message = "Original error" });

        var failed = await result.FailIfAsync(
            () => new ValueTask<bool>(true),
            new Error { Message = "New error" }
        );

        failed.IsValid.Should().BeFalse();
        failed.FirstError.Message.Should().Be("Original error");
    }
}
