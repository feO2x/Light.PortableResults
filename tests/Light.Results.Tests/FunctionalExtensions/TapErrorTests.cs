using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class TapErrorTests
{
    [Fact]
    public void TapError_OnGenericResult_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = result.TapError(e => capturedErrors = e);

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_OnGenericResult_OnSuccess_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Ok(42);

        var tapped = result.TapError(_ => executed = true);

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_OnNonGenericResult_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result.Fail(new Error { Message = "Error" });

        var tapped = result.TapError(e => capturedErrors = e);

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_OnNonGenericResult_OnSuccess_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result.Ok();

        var tapped = result.TapError(_ => executed = true);

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public async Task TapErrorAsync_OnGenericResult_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = await result.TapErrorAsync(
            e =>
            {
                capturedErrors = e;
                return default;
            }
        );

        capturedErrors.Should().NotBeNull();
        tapped.Should().Be(result);
    }

    [Fact]
    public async Task TapErrorAsync_OnNonGenericResult_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result.Fail(new Error { Message = "Error" });

        var tapped = await result.TapErrorAsync(
            e =>
            {
                capturedErrors = e;
                return default;
            }
        );

        capturedErrors.Should().NotBeNull();
        tapped.Should().Be(result);
    }
}
