using System;
using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class ResultAdditionalTests
{
    [Fact]
    public void Value_OnFailure_ShouldThrow()
    {
        var result = Result<int>.Fail(new Error { Message = "Failed" });

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot access Value on a failed Result*");
    }

    [Fact]
    public void FirstError_OnSuccess_ShouldThrow()
    {
        var result = Result<int>.Ok(42);

        var act = () => result.FirstError;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot access errors on a successful Result*");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWith3Errors_ShouldReturnNumberOfErrors()
    {
        var errors = new Error[]
        {
            new () { Message = "Error 1" },
            new () { Message = "Error 2" },
            new () { Message = "Error 3" }
        };
        var result = Result<string>.Fail(errors);

        result.DebuggerDisplay.Should().Be("Fail(3 errors)");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWithSingleError_ShouldReturnSingleErrorMessage()
    {
        var error = new Error { Message = "Failed" };
        var result = Result<string>.Fail(error);

        result.DebuggerDisplay.Should().Be($"Fail(single error: '{error.Message}')");
    }

    [Fact]
    public void DebuggerDisplay_OnSuccess_ShouldReturnValue()
    {
        var result = Result<string>.Ok("Success");

        result.DebuggerDisplay.Should().Be("OK('Success')");
    }
}
