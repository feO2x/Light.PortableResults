using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class TapTests
{
    [Fact]
    public void Tap_OnSuccess_ShouldExecuteAction()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        var tapped = result.Tap(v => capturedValue = v);

        capturedValue.Should().Be(42);
        tapped.Should().Be(result);
    }

    [Fact]
    public void Tap_OnFailure_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = result.Tap(_ => executed = true);

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public async Task TapAsync_OnSuccess_ShouldExecuteAction()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        var tapped = await result.TapAsync(
            v =>
            {
                capturedValue = v;
                return default;
            }
        );

        capturedValue.Should().Be(42);
        tapped.Should().Be(result);
    }

    [Fact]
    public async Task TapAsync_OnFailure_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = await result.TapAsync(
            _ =>
            {
                executed = true;
                return default;
            }
        );

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }
}
