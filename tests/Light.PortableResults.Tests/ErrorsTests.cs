using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests;

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

        act.Should().Throw<IndexOutOfRangeException>();
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

    [Fact]
    public void Errors_WithSameSingleError_ShouldBeEqual()
    {
        var error = new Error { Message = "Duplicate" };

        var left = new Errors(error);
        var right = new Errors(error);

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

    [Fact]
    public void Errors_WithDifferentCounts_ShouldNotBeEqual()
    {
        var single = new Errors(new Error { Message = "Only" });
        var multiple = new Errors(
            new[]
            {
                new Error { Message = "Only" },
                new Error { Message = "Second" }
            }
        );

        (single == multiple).Should().BeFalse();
        (single != multiple).Should().BeTrue();
    }

    [Fact]
    public void Errors_WithSameMultipleErrors_ShouldBeEqual()
    {
        var errorsArray = new[]
        {
            new Error { Message = "First" },
            new Error { Message = "Second" }
        };

        var left = new Errors(errorsArray);
        var right = new Errors(errorsArray.ToArray());

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
    }

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

    [Fact]
    public void Errors_SingleError_ShouldEnumerateCorrectly()
    {
        var errors = new Errors(new Error { Message = "Single error" });

        var list = new List<Error>();
        foreach (var error in errors)
        {
            list.Add(error);
        }

        Error[] expected = [new () { Message = "Single error" }];
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

    [Fact]
    public void Enumerator_Current_BeforeMoveNext_SingleError_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });
        using var enumerator = errors.GetEnumerator();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => enumerator.Current;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*before the first element or after the last element*");
    }

    [Fact]
    public void Enumerator_Current_AfterLastElement_SingleError_ShouldThrow()
    {
        var errors = new Errors(new Error { Message = "Error" });
        using var enumerator = errors.GetEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => enumerator.Current;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*before the first element or after the last element*");
    }

    [Fact]
    public void Enumerator_Reset_SingleError_CanBeEnumeratedTwice()
    {
        var error = new Error { Message = "Error" };
        var errors = new Errors(error);
        using var enumerator = errors.GetEnumerator();

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
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_MoveNext_SingleError_ShouldReturnFalseAfterLastElement()
    {
        var errors = new Errors(new Error { Message = "Error" });
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_MoveNext_DefaultErrors_ShouldReturnFalse()
    {
        var errors = default(Errors);
        using var enumerator = errors.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataTrue_SameErrors_ShouldBeEqual()
    {
        var error = new Error { Message = "Error" };
        var errors1 = new Errors(error);
        var errors2 = new Errors(error);

        errors1.Equals(errors2, compareMetadata: true).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataTrue_DifferentMetadata_ShouldNotBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = metadata1 };
        var error2 = new Error { Message = "Error", Metadata = metadata2 };
        var errors1 = new Errors(error1);
        var errors2 = new Errors(error2);

        errors1.Equals(errors2, compareMetadata: true).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = metadata1 };
        var error2 = new Error { Message = "Error", Metadata = metadata2 };
        var errors1 = new Errors(error1);
        var errors2 = new Errors(error2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentCounts_ShouldNotBeEqual()
    {
        var errors1 = new Errors(new Error { Message = "Error" });
        var errors2 = new Errors(new[] { new Error { Message = "Error" }, new Error { Message = "Error2" } });

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_BothDefault_ShouldBeEqual()
    {
        var errors1 = default(Errors);
        var errors2 = default(Errors);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_MultipleErrors_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var errorsArray1 = new[]
        {
            new Error { Message = "E1", Metadata = metadata1 },
            new Error { Message = "E2", Metadata = metadata1 }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1", Metadata = metadata2 },
            new Error { Message = "E2", Metadata = metadata2 }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_MultipleErrors_DifferentMessages_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E3" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SameReference_ShouldBeEqual()
    {
        var errorsArray = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray);
        var errors2 = new Errors(errorsArray);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_ShouldUseUnrolledLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex0_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex2_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex3_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex4_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex5_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex6_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E8" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_EightErrors_DifferentAtIndex7_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_ShouldUseUnrolledLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex0_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex2_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "DIFFERENT" },
            new Error { Message = "E4" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FourErrors_DifferentAtIndex3_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwoErrors_ShouldUseRemainingLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwoErrors_DifferentAtIndex1_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_NineErrors_ShouldCoverBothLoops()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_NineErrors_DifferentAtIndex8_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_TwelveErrors_ShouldCoverAllLoops()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SixteenErrors_ShouldCoverTwoIterationsOfEightLoop()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_SixteenErrors_DifferentAtIndex15_ShouldNotBeEqual()
    {
        var errorsArray1 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "DIFFERENT" }
        };
        var errorsArray2 = new[]
        {
            new Error { Message = "E1" },
            new Error { Message = "E2" },
            new Error { Message = "E3" },
            new Error { Message = "E4" },
            new Error { Message = "E5" },
            new Error { Message = "E6" },
            new Error { Message = "E7" },
            new Error { Message = "E8" },
            new Error { Message = "E9" },
            new Error { Message = "E10" },
            new Error { Message = "E11" },
            new Error { Message = "E12" },
            new Error { Message = "E13" },
            new Error { Message = "E14" },
            new Error { Message = "E15" },
            new Error { Message = "E16" }
        };
        var errors1 = new Errors(errorsArray1);
        var errors2 = new Errors(errorsArray2);

        errors1.Equals(errors2, compareMetadata: false).Should().BeFalse();
    }

    [Fact]
    public void GetLeadingCategory_SingleError_ReturnsItsCategory()
    {
        var errors = new Errors(new Error { Message = "Test", Category = ErrorCategory.NotFound });

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.NotFound);
    }

    [Fact]
    public void GetLeadingCategory_MultipleErrorsWithSameCategory_ReturnsThatCategory()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.Validation }
            }
        );

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void GetLeadingCategory_MultipleErrorsWithDifferentCategories_ReturnsUnclassified()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.NotFound }
            }
        );

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.Unclassified);
    }

    [Fact]
    public void GetLeadingCategory_FirstCategoryIsLeadingCategory_ReturnsFirstErrorCategory()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.NotFound }
            }
        );

        var result = errors.GetLeadingCategory(firstCategoryIsLeadingCategory: true);

        result.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void GetLeadingCategory_EmptyErrors_ThrowsInvalidOperationException()
    {
        var errors = default(Errors);

        var act = () => errors.GetLeadingCategory();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Errors collection must contain at least one error.");
    }
}
