using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class ErrorsAdditionalTests
{
    [Fact]
    public void First_WithEmptyErrors_ShouldThrow()
    {
        var errors = default(Errors);

        var act = () => errors.First;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No errors present");
    }

    [Fact]
    public void Indexer_WithNegativeIndex_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });

        var act = () => errors[-1];

        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Indexer_WithIndexEqualToCount_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });

        var act = () => errors[1];

        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithEmptyMemory_ShouldThrowArgumentException()
    {
        var act = () => new Errors(ReadOnlyMemory<Error>.Empty);

        act.Should().Throw<ArgumentException>().Where(x => x.ParamName == "manyErrors");
    }

    [Fact]
    public void Enumerator_Reset_ShouldResetPosition()
    {
        var errors = new Errors(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.Reset();
        enumerator.MoveNext();

        enumerator.Current.Message.Should().Be("E1");
    }

    [Fact]
    public void Enumerator_Current_ViaIEnumerator_ShouldReturnBoxedValue()
    {
        var errors = new Errors(new Error { Message = "Test" });
        IEnumerator enumerator = errors.GetEnumerator();

        enumerator.MoveNext();

        var current = (Error) enumerator.Current;
        current.Message.Should().Be("Test");
        (enumerator as IDisposable)?.Dispose();
    }

    [Fact]
    public void Enumerator_Dispose_ShouldNotThrow()
    {
        var errors = new Errors(new Error { Message = "Test" });
        var enumerator = errors.GetEnumerator();

        var act = () => enumerator.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Enumerator_SingleError_Current_ShouldReturnError()
    {
        var error = new Error { Message = "Single" };
        var errors = new Errors(error);
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext();

        enumerator.Current.Should().Be(error);
    }

    [Fact]
    public void Enumerator_MultipleErrors_Current_ShouldReturnCorrectError()
    {
        var errors = new Errors(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Current.Message.Should().Be("E1");
        enumerator.MoveNext();
        enumerator.Current.Message.Should().Be("E2");
    }

    [Fact]
    public void IEnumerableGeneric_GetEnumerator_ShouldWork()
    {
        var errors = new Errors(new Error { Message = "Test" });
        IEnumerable<Error> enumerable = errors;

        var list = new List<Error>();
        foreach (var error in enumerable)
        {
            list.Add(error);
        }

        list.Should().ContainSingle();
    }

    [Fact]
    public void GetHashCode_DefaultErrors_ShouldReturnZero()
    {
        var errors = default(Errors);

        errors.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void GetHashCode_SingleError_ShouldReturnConsistentValue()
    {
        var error = new Error { Message = "Test" };
        var errors = new Errors(error);

        var hash1 = errors.GetHashCode();
        var hash2 = errors.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_MultipleErrors_ShouldReturnConsistentValue()
    {
        var errors = new Errors(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });

        var hash1 = errors.GetHashCode();
        var hash2 = errors.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_DifferentErrors_ShouldReturnDifferentValues()
    {
        var errors1 = new Errors(new Error { Message = "E1" });
        var errors2 = new Errors(new Error { Message = "E2" });

        var hash1 = errors1.GetHashCode();
        var hash2 = errors2.GetHashCode();

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Constructor_SingleError_WithDefaultInstance_ShouldThrow()
    {
        var act = () => new Errors(default(Error));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*must not be default instance*")
           .WithParameterName("singleError");
    }

    [Fact]
    public void Constructor_ReadOnlyMemory_WithSingleDefaultError_ShouldThrow()
    {
        var act = () => new Errors(new[] { default(Error) }.AsMemory());

        act.Should().Throw<ArgumentException>()
           .WithMessage("*single error*must not be the default instance*")
           .WithParameterName("manyErrors");
    }

    [Fact]
    public void Constructor_ReadOnlyMemory_WithDefaultErrorInMiddle_ShouldThrow()
    {
        var act = () => new Errors(
            new[] { new Error { Message = "E1" }, default(Error), new Error { Message = "E3" } }.AsMemory()
        );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*error at index 1*must not be the default instance*")
           .WithParameterName("manyErrors");
    }

    [Fact]
    public void Equals_Object_WithNonErrors_ShouldReturnFalse()
    {
        var errors = new Errors(new Error { Message = "Test" });

        // ReSharper disable once SuspiciousTypeConversion.Global -- OK in test scenario
        errors.Equals("not errors").Should().BeFalse();
    }
}
