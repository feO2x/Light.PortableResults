using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Xunit;

namespace Light.Results.Tests.FunctionalExtensions;

public sealed class MatchTests
{
    [Fact]
    public void Match_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Ok(42);

        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onError: _ => "Error"
        );

        output.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_OnGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        var result = Result<int>.Fail(new Error { Message = "Test error" });

        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onError: e => $"Error: {e.First.Message}"
        );

        output.Should().Be("Error: Test error");
    }

    [Fact]
    public void Match_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result.Ok();

        var output = result.Match(
            onSuccess: () => "Success",
            onError: _ => "Error"
        );

        output.Should().Be("Success");
    }

    [Fact]
    public void Match_OnNonGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        var result = Result.Fail(new Error { Message = "Test error" });

        var output = result.Match(
            onSuccess: () => "Success",
            onError: e => $"Error: {e.First.Message}"
        );

        output.Should().Be("Error: Test error");
    }

    [Fact]
    public async Task MatchAsync_OnGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result<int>.Ok(42);

        var output = await result.MatchAsync(
            onSuccess: v => new ValueTask<string>($"Value: {v}"),
            onError: _ => new ValueTask<string>("Error")
        );

        output.Should().Be("Value: 42");
    }

    [Fact]
    public async Task MatchAsync_OnGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        var result = Result<int>.Fail(new Error { Message = "Test error" });

        var output = await result.MatchAsync(
            onSuccess: v => new ValueTask<string>($"Value: {v}"),
            onError: e => new ValueTask<string>($"Error: {e.First.Message}")
        );

        output.Should().Be("Error: Test error");
    }

    [Fact]
    public async Task MatchAsync_OnNonGenericResult_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = Result.Ok();

        var output = await result.MatchAsync(
            onSuccess: () => new ValueTask<string>("Success"),
            onError: _ => new ValueTask<string>("Error")
        );

        output.Should().Be("Success");
    }

    [Fact]
    public async Task MatchAsync_OnNonGenericResult_OnFailure_ShouldCallErrorHandler()
    {
        var result = Result.Fail(new Error { Message = "Test error" });

        var output = await result.MatchAsync(
            onSuccess: () => new ValueTask<string>("Success"),
            onError: e => new ValueTask<string>($"Error: {e.First.Message}")
        );

        output.Should().Be("Error: Test error");
    }
}
