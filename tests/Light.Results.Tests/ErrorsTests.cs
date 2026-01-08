using System;
using System.Collections;
using System.Linq;
using FluentAssertions;

namespace Light.Results.Tests;

public sealed class ErrorsTests
{
    [Fact]
    public void SingleError_Count_ShouldBeOne()
    {
        var errors = new Errors(new Error { Message = "Error" });

        errors.Count.Should().Be(1);
    }

    [Fact]
    public void SingleError_First_ShouldReturnTheError()
    {
        var error = new Error { Message = "Error" };
        var errors = new Errors(error);

        errors.First.Should().Be(error);
    }

    [Fact]
    public void SingleError_GetEnumerator_ShouldYieldOneError()
    {
        var error = new Error { Message = "Error" };
        var errors = new Errors(error);

        var list = errors.ToList();

        list.Should().ContainSingle().Which.Should().Be(error);
    }

    [Fact]
    public void MultipleErrors_Count_ShouldBeCorrect()
    {
        var errorArray = new[]
            { new Error { Message = "E1" }, new Error { Message = "E2" }, new Error { Message = "E3" } };
        var errors = new Errors(errorArray);

        errors.Count.Should().Be(3);
    }

    [Fact]
    public void MultipleErrors_First_ShouldReturnFirstError()
    {
        var errorArray = new[] { new Error { Message = "E1" }, new Error { Message = "E2" } };
        var errors = new Errors(errorArray);

        errors.First.Message.Should().Be("E1");
    }

    [Fact]
    public void MultipleErrors_GetEnumerator_ShouldYieldAllErrors()
    {
        var errorArray = new[]
            { new Error { Message = "E1" }, new Error { Message = "E2" }, new Error { Message = "E3" } };
        var errors = new Errors(errorArray);

        var list = errors.ToList();

        list.Should().HaveCount(3);
        list[0].Message.Should().Be("E1");
        list[1].Message.Should().Be("E2");
        list[2].Message.Should().Be("E3");
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ShouldWork()
    {
        var errors = new Errors(new Error { Message = "Error" });
        IEnumerable enumerable = errors;

        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        count.Should().Be(1);
    }

    [Fact]
    public void Indexer_WithZeroCount_ShouldThrow()
    {
        var errors = default(Errors);

        var act = () => errors[0];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Indexer_WithOneError_ShouldReturnTheError()
    {
        var error = new Error { Message = "Error" };
        var errors = new Errors(error);

        errors[0].Should().Be(error);
    }

    [Fact]
    public void Indexer_WithMultipleErrors_ShouldReturnCorrectError()
    {
        var errorArray = new[] { new Error { Message = "E1" }, new Error { Message = "E2" } };
        var errors = new Errors(errorArray);

        errors[0].Message.Should().Be("E1");
        errors[1].Message.Should().Be("E2");
    }

    [Fact]
    public void GetEnumerator_WithZeroCount_ShouldYieldNothing()
    {
        var errors = default(Errors);

        var list = errors.ToList();

        list.Should().BeEmpty();
    }

    [Fact]
    public void FromArray_WithSingleError_ShouldUseSingleErrorPath()
    {
        var errorArray = new[] { new Error { Message = "Single" } };
        var errors = new Errors(errorArray);

        errors.Count.Should().Be(1);
        errors.First.Message.Should().Be("Single");
    }
}
