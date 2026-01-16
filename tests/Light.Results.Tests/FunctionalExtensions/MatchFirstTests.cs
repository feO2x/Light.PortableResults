using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class MatchFirstTests
{
    [Fact]
    public void MatchFirst_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Ok(42);

        var output = result.MatchFirst(
            onSuccess: v => $"Value: {v}",
            onError: _ => "Error"
        );

        output.Should().Be("Value: 42");
    }

    [Fact]
    public void MatchFirst_OnGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        var result = Result<int>.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        var output = result.MatchFirst(
            onSuccess: v => $"Value: {v}",
            onError: e => $"Error: {e.Message}"
        );

        output.Should().Be("Error: First");
    }

    [Fact]
    public void MatchFirst_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result.Ok();

        var output = result.MatchFirst(
            onSuccess: () => "Success",
            onError: _ => "Error"
        );

        output.Should().Be("Success");
    }

    [Fact]
    public void MatchFirst_OnNonGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        var result = Result.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        var output = result.MatchFirst(
            onSuccess: () => "Success",
            onError: e => $"Error: {e.Message}"
        );

        output.Should().Be("Error: First");
    }

    [Fact]
    public async Task MatchFirstAsync_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Ok(42);

        var output = await result.MatchFirstAsync(
            onSuccess: v => new ValueTask<string>($"Value: {v}"),
            onError: _ => new ValueTask<string>("Error")
        );

        output.Should().Be("Value: 42");
    }

    [Fact]
    public async Task MatchFirstAsync_OnGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        var result = Result<int>.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        var output = await result.MatchFirstAsync(
            onSuccess: v => new ValueTask<string>($"Value: {v}"),
            onError: e => new ValueTask<string>($"Error: {e.Message}")
        );

        output.Should().Be("Error: First");
    }

    [Fact]
    public async Task MatchFirstAsync_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result.Ok();

        var output = await result.MatchFirstAsync(
            onSuccess: () => new ValueTask<string>("Success"),
            onError: _ => new ValueTask<string>("Error")
        );

        output.Should().Be("Success");
    }

    [Fact]
    public async Task MatchFirstAsync_OnNonGenericResult_OnFailure_ShouldCallErrorHandlerWithFirstError()
    {
        var result = Result.Fail(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        var output = await result.MatchFirstAsync(
            onSuccess: () => new ValueTask<string>("Success"),
            onError: e => new ValueTask<string>($"Error: {e.Message}")
        );

        output.Should().Be("Error: First");
    }
}
