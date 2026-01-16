using System;
using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class ErrorsEnumeratorCurrentTests
{
    [Fact]
    public void Enumerator_Current_BeforeMoveNext_SingleError_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var enumerator = errors.GetEnumerator();

        var act = () => enumerator.Current;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*before the first element or after the last element*");
    }

    [Fact]
    public void Enumerator_Current_AfterLastElement_SingleError_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var enumerator = errors.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();

        var act = () => enumerator.Current;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*before the first element or after the last element*");
    }

    [Fact]
    public void Enumerator_Reset_SingleError_ShouldAllowReenumeration()
    {
        var error = new Error { Message = "Error" };
        var errors = new Errors(error);
        var enumerator = errors.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Current.Should().Be(error);
        enumerator.Reset();
        enumerator.MoveNext();

        enumerator.Current.Should().Be(error);
    }

    [Fact]
    public void Enumerator_MoveNext_MultipleErrors_ShouldReturnFalseAfterLastElement()
    {
        var errors = new Errors(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });
        var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_MoveNext_SingleError_ShouldReturnFalseAfterLastElement()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_MoveNext_DefaultErrors_ShouldReturnFalse()
    {
        var errors = default(Errors);
        var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }
}
