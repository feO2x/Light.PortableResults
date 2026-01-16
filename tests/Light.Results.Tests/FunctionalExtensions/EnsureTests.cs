using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class EnsureTests
{
    [Fact]
    public void Ensure_OnGenericResult_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var ensured = result.Ensure(v => v > 40, new Error { Message = "Too small" });

        ensured.IsValid.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public void Ensure_OnGenericResult_WhenPredicateFalse_ShouldReturnFailure()
    {
        var result = Result<int>.Ok(42);

        var ensured = result.Ensure(v => v > 50, new Error { Message = "Too small" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Too small");
    }

    [Fact]
    public void Ensure_OnGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var ensured = result.Ensure(_ => false, new Error { Message = "New error" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public void Ensure_OnGenericResult_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Ok(42, metadata);

        var ensured = result.Ensure(v => v < 40, new Error { Message = "Too large" });

        ensured.Metadata.Should().NotBeNull();
        ensured.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Ensure_WithErrorFactory_ShouldUseFactory()
    {
        var result = Result<int>.Ok(42);

        var ensured = result.Ensure(v => v > 50, v => new Error { Message = $"Value {v} is too small" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Value 42 is too small");
    }

    [Fact]
    public void Ensure_WithErrorFactory_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var ensured = result.Ensure(v => v > 40, v => new Error { Message = $"Value {v} is too small" });

        ensured.IsValid.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public void Ensure_WithErrorFactory_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var ensured = result.Ensure(_ => false, _ => new Error { Message = "New error" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public void Ensure_OnNonGenericResult_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var ensured = result.Ensure(() => true, new Error { Message = "Condition failed" });

        ensured.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Ensure_OnNonGenericResult_WhenPredicateFalse_ShouldReturnFailure()
    {
        var result = Result.Ok();

        var ensured = result.Ensure(() => false, new Error { Message = "Condition failed" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Condition failed");
    }

    [Fact]
    public void Ensure_OnNonGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result.Fail(new Error { Message = "Original error" });

        var ensured = result.Ensure(() => false, new Error { Message = "New error" });

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task EnsureAsync_OnGenericResult_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var ensured = await result.EnsureAsync(
            v => new ValueTask<bool>(v > 40),
            new Error { Message = "Too small" }
        );

        ensured.IsValid.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public async Task EnsureAsync_OnGenericResult_WhenPredicateFalse_ShouldReturnFailure()
    {
        var result = Result<int>.Ok(42);

        var ensured = await result.EnsureAsync(
            v => new ValueTask<bool>(v > 50),
            new Error { Message = "Too small" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Too small");
    }

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_ShouldUseFactory()
    {
        var result = Result<int>.Ok(42);

        var ensured = await result.EnsureAsync(
            v => new ValueTask<bool>(v > 50),
            v => new Error { Message = $"Value {v} is too small" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Value 42 is too small");
    }

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result<int>.Ok(42);

        var ensured = await result.EnsureAsync(
            v => new ValueTask<bool>(v > 40),
            v => new Error { Message = $"Value {v} is too small" }
        );

        ensured.IsValid.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public async Task EnsureAsync_WithErrorFactory_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var ensured = await result.EnsureAsync(
            _ => new ValueTask<bool>(false),
            _ => new Error { Message = "New error" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task EnsureAsync_OnGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result<int>.Fail(new Error { Message = "Original error" });

        var ensured = await result.EnsureAsync(
            _ => new ValueTask<bool>(false),
            new Error { Message = "New error" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }

    [Fact]
    public async Task EnsureAsync_OnNonGenericResult_WhenPredicateFalse_ShouldReturnFailure()
    {
        var result = Result.Ok();

        var ensured = await result.EnsureAsync(
            () => new ValueTask<bool>(false),
            new Error { Message = "Condition failed" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Condition failed");
    }

    [Fact]
    public async Task EnsureAsync_OnNonGenericResult_WhenPredicateTrue_ShouldReturnOriginal()
    {
        var result = Result.Ok();

        var ensured = await result.EnsureAsync(
            () => new ValueTask<bool>(true),
            new Error { Message = "Condition failed" }
        );

        ensured.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_OnNonGenericResult_WhenAlreadyFailed_ShouldReturnOriginal()
    {
        var result = Result.Fail(new Error { Message = "Original error" });

        var ensured = await result.EnsureAsync(
            () => new ValueTask<bool>(false),
            new Error { Message = "New error" }
        );

        ensured.IsValid.Should().BeFalse();
        ensured.FirstError.Message.Should().Be("Original error");
    }
}
