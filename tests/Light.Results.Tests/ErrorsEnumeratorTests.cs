using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace Light.Results.Tests;

public sealed class ErrorsEnumeratorTests
{
    [Fact]
    public void Errors_SingleError_ShouldEnumerateCorrectly()
    {
        var errors = new Errors(new Error { Message = "Single error" });

        var list = new List<Error>();
        foreach (var error in errors)
        {
            list.Add(error);
        }

        Error[] expected = [new() { Message = "Single error" }];
        list.Should().Equal(expected);
    }

    [Fact]
    public void Errors_MultipleErrors_ShouldEnumerateCorrectly()
    {
        var errors = new Errors(
            new[]
            {
                new Error { Message = "Error1" }, new Error { Message = "Error2" }, new Error { Message = "Error3" }
            }
        );

        var list = new List<Error>();
        foreach (var error in errors)
        {
            list.Add(error);
        }

        list.Should().HaveCount(3);
        list[0].Message.Should().Be("Error1");
        list[1].Message.Should().Be("Error2");
        list[2].Message.Should().Be("Error3");
    }

    [Fact]
    public void Errors_Indexer_ShouldReturnCorrectError()
    {
        var errors = new Errors(new[] { new Error { Message = "Error1" }, new Error { Message = "Error2" } });

        errors[0].Message.Should().Be("Error1");
        errors[1].Message.Should().Be("Error2");
    }

    [Fact]
    public void Errors_SingleError_Indexer_ShouldReturnCorrectError()
    {
        var errors = new Errors(new Error { Message = "Single error" });

        errors[0].Message.Should().Be("Single error");
    }

    [Fact]
    public void Errors_Count_ShouldReturnCorrectCount()
    {
        var single = new Errors(new Error { Message = "Single" });
        var multiple = new Errors(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });

        single.Count.Should().Be(1);
        multiple.Count.Should().Be(2);
    }

    [Fact]
    public void Errors_LinqOperations_ShouldWork()
    {
        var errors = new Errors(
            new[] { new Error { Message = "Error1", Code = "E1" }, new Error { Message = "Error2", Code = "E2" } }
        );

        var codes = errors.Select(e => e.Code).ToList();

        codes.Should().Equal("E1", "E2");
    }

    [Fact]
    public void Errors_First_ShouldReturnFirstError()
    {
        var errors = new Errors(new[] { new Error { Message = "First" }, new Error { Message = "Second" } });

        errors.First.Message.Should().Be("First");
    }

    [Fact]
    public void Errors_SingleError_First_ShouldReturnTheError()
    {
        var errors = new Errors(new Error { Message = "Only" });

        errors.First.Message.Should().Be("Only");
    }
}
