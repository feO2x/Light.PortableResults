using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class ElseTests
{
    [Fact]
    public void Else_WithValue_OnSuccess_ShouldReturnOriginalValue()
    {
        var result = Result<int>.Ok(42);

        var value = result.Else(0);

        value.Should().Be(42);
    }

    [Fact]
    public void Else_WithValue_OnFailure_ShouldReturnFallback()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var value = result.Else(99);

        value.Should().Be(99);
    }

    [Fact]
    public void Else_WithFactory_OnSuccess_ShouldReturnOriginalValue()
    {
        var result = Result<int>.Ok(42);

        var value = result.Else(_ => 0);

        value.Should().Be(42);
    }

    [Fact]
    public void Else_WithFactory_OnFailure_ShouldCallFactory()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var value = result.Else(
            e =>
            {
                capturedErrors = e;
                return 99;
            }
        );

        value.Should().Be(99);
        capturedErrors.Should().NotBeNull();
    }

    [Fact]
    public async Task ElseAsync_OnSuccess_ShouldReturnOriginalValue()
    {
        var result = Result<int>.Ok(42);

        var value = await result.ElseAsync(_ => new ValueTask<int>(0));

        value.Should().Be(42);
    }

    [Fact]
    public async Task ElseAsync_OnFailure_ShouldCallFactory()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var value = await result.ElseAsync(
            e =>
            {
                capturedErrors = e;
                return new ValueTask<int>(99);
            }
        );

        value.Should().Be(99);
        capturedErrors.Should().NotBeNull();
    }
}
