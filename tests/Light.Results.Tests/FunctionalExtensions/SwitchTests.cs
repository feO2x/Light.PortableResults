using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class SwitchTests
{
    [Fact]
    public void Switch_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        result.Switch(
            onSuccess: v => capturedValue = v,
            onError: _ => { }
        );

        capturedValue.Should().Be(42);
    }

    [Fact]
    public void Switch_OnGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        result.Switch(
            onSuccess: _ => { },
            onError: e => capturedErrors = e
        );

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
    }

    [Fact]
    public void Switch_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var successCalled = false;
        var result = Result.Ok();

        result.Switch(
            onSuccess: () => successCalled = true,
            onError: _ => { }
        );

        successCalled.Should().BeTrue();
    }

    [Fact]
    public void Switch_OnNonGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        Errors? capturedErrors = null;
        var result = Result.Fail(new Error { Message = "Error" });

        result.Switch(
            onSuccess: () => { },
            onError: e => capturedErrors = e
        );

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task SwitchAsync_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        await result.SwitchAsync(
            onSuccess: v =>
            {
                capturedValue = v;
                return default;
            },
            onError: _ => default
        );

        capturedValue.Should().Be(42);
    }

    [Fact]
    public async Task SwitchAsync_OnGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        await result.SwitchAsync(
            onSuccess: _ => default,
            onError: e =>
            {
                capturedErrors = e;
                return default;
            }
        );

        capturedErrors.Should().NotBeNull();
    }

    [Fact]
    public async Task SwitchAsync_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var successCalled = false;
        var result = Result.Ok();

        await result.SwitchAsync(
            onSuccess: () =>
            {
                successCalled = true;
                return default;
            },
            onError: _ => default
        );

        successCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchAsync_OnNonGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        Errors? capturedErrors = null;
        var result = Result.Fail(new Error { Message = "Error" });

        await result.SwitchAsync(
            onSuccess: () => default,
            onError: e =>
            {
                capturedErrors = e;
                return default;
            }
        );

        capturedErrors.Should().NotBeNull();
    }
}
