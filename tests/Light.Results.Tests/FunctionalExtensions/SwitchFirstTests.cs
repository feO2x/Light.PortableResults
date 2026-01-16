using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class SwitchFirstTests
{
    [Fact]
    public void SwitchFirst_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        result.SwitchFirst(
            onSuccess: v => capturedValue = v,
            onError: _ => { }
        );

        capturedValue.Should().Be(42);
    }

    [Fact]
    public void SwitchFirst_OnGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        Error? capturedError = null;
        var result = Result<int>.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        result.SwitchFirst(
            onSuccess: _ => { },
            onError: e => capturedError = e
        );

        capturedError.Should().NotBeNull();
        capturedError!.Value.Message.Should().Be("First");
    }

    [Fact]
    public void SwitchFirst_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var successCalled = false;
        var result = Result.Ok();

        result.SwitchFirst(
            onSuccess: () => successCalled = true,
            onError: _ => { }
        );

        successCalled.Should().BeTrue();
    }

    [Fact]
    public void SwitchFirst_OnNonGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        Error? capturedError = null;
        var result = Result.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        result.SwitchFirst(
            onSuccess: () => { },
            onError: e => capturedError = e
        );

        capturedError.Should().NotBeNull();
        capturedError!.Value.Message.Should().Be("First");
    }

    [Fact]
    public async Task SwitchFirstAsync_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var capturedValue = 0;
        var result = Result<int>.Ok(42);

        await result.SwitchFirstAsync(
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
    public async Task SwitchFirstAsync_OnGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        Error? capturedError = null;
        var result = Result<int>.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        await result.SwitchFirstAsync(
            onSuccess: _ => default,
            onError: e =>
            {
                capturedError = e;
                return default;
            }
        );

        capturedError.Should().NotBeNull();
        capturedError!.Value.Message.Should().Be("First");
    }

    [Fact]
    public async Task SwitchFirstAsync_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var successCalled = false;
        var result = Result.Ok();

        await result.SwitchFirstAsync(
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
    public async Task SwitchFirstAsync_OnNonGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        Error? capturedError = null;
        var result = Result.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        await result.SwitchFirstAsync(
            onSuccess: () => default,
            onError: e =>
            {
                capturedError = e;
                return default;
            }
        );

        capturedError.Should().NotBeNull();
        capturedError!.Value.Message.Should().Be("First");
    }
}
