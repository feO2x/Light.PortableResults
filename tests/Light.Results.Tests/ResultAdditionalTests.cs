using System;
using FluentAssertions;

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
}
