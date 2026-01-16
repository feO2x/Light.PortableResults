using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class TryGetValueTests
{
    [Fact]
    public void TryGetValue_OnSuccess_ShouldReturnTrueAndValue()
    {
        var result = Result<int>.Ok(42);

        var success = result.TryGetValue(out var value);

        success.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_OnFailure_ShouldReturnFalseAndDefault()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var success = result.TryGetValue(out var value);

        success.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_OnSuccessWithReferenceType_ShouldReturnTrueAndValue()
    {
        var result = Result<string>.Ok("hello");

        var success = result.TryGetValue(out var value);

        success.Should().BeTrue();
        value.Should().Be("hello");
    }

    [Fact]
    public void TryGetValue_OnFailureWithReferenceType_ShouldReturnFalseAndNull()
    {
        var result = Result<string>.Fail(new Error { Message = "Error" });

        var success = result.TryGetValue(out var value);

        success.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetValue_OnSuccessWithNullableValueType_ShouldReturnTrueAndValue()
    {
        var result = Result<int?>.Ok(42);

        var success = result.TryGetValue(out var value);

        success.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_CanBeUsedInPatternMatching()
    {
        var result = Result<int>.Ok(42);

        var message = result.TryGetValue(out var value) ? $"Success: {value}" : $"Failure: {result.FirstError.Message}";

        message.Should().Be("Success: 42");
    }
}
