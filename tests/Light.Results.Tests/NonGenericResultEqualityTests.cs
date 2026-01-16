using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class NonGenericResultEqualityTests
{
    [Fact]
    public void Equals_TwoSuccessfulResults_ShouldBeEqual()
    {
        var result1 = Result.Ok();
        var result2 = Result.Ok();

        (result1 == result2).Should().BeTrue();
        (result1 != result2).Should().BeFalse();
        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_TwoFailedResultsWithSameError_ShouldBeEqual()
    {
        var error = new Error { Message = "Error", Code = "ERR001" };
        var result1 = Result.Fail(error);
        var result2 = Result.Fail(error);

        (result1 == result2).Should().BeTrue();
        (result1 != result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_TwoFailedResultsWithDifferentErrors_ShouldNotBeEqual()
    {
        var result1 = Result.Fail(new Error { Message = "Error1" });
        var result2 = Result.Fail(new Error { Message = "Error2" });

        (result1 == result2).Should().BeFalse();
        (result1 != result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_SuccessAndFailure_ShouldNotBeEqual()
    {
        var result1 = Result.Ok();
        var result2 = Result.Fail(new Error { Message = "Error" });

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithMetadata_SameMetadata_ShouldBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result.Ok(metadata);
        var result2 = Result.Ok(metadata);

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithMetadata_DifferentMetadata_ShouldNotBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result.Ok(metadata1);
        var result2 = Result.Ok(metadata2);

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithMetadata_OneWithMetadataOneWithout_ShouldNotBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result.Ok(metadata);
        var result2 = Result.Ok();

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result.Ok(metadata1);
        var result2 = Result.Ok(metadata2);

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_OneWithMetadataOneWithout_ShouldBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result.Ok(metadata);
        var result2 = Result.Ok();

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_WithSameResult_ShouldBeEqual()
    {
        var result1 = Result.Ok();
        object result2 = Result.Ok();

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_WithNull_ShouldBeFalse()
    {
        var result = Result.Ok();

        result.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_WithDifferentType_ShouldBeFalse()
    {
        var result = Result.Ok();

        // ReSharper disable once SuspiciousTypeConversion.Global -- OK in this test scenario
        result.Equals("not a result").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameSuccessfulResults_ShouldHaveSameHashCode()
    {
        var result1 = Result.Ok();
        var result2 = Result.Ok();

        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithSameMetadata_ShouldHaveSameHashCode()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result.Ok(metadata);
        var result2 = Result.Ok(metadata);

        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentMetadata_ShouldHaveDifferentHashCode()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result.Ok(metadata1);
        var result2 = Result.Ok(metadata2);

        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithIncludeMetadataFalse_DifferentMetadata_ShouldHaveSameHashCode()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result.Ok(metadata1);
        var result2 = Result.Ok(metadata2);

        result1.GetHashCode(includeMetadata: false).Should().Be(result2.GetHashCode(includeMetadata: false));
    }

    [Fact]
    public void GetHashCode_FailedResults_ShouldIncludeErrors()
    {
        var error1 = new Error { Message = "Error1" };
        var error2 = new Error { Message = "Error2" };
        var result1 = Result.Fail(error1);
        var result2 = Result.Fail(error2);

        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithNullMetadata_ShouldNotThrow()
    {
        var result = Result.Ok();

        var act = () => result.GetHashCode();

        act.Should().NotThrow();
    }

    [Fact]
    public void DefaultConstructor_ShouldCreateSuccessfulResult()
    {
        var result = new Result();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FirstError_OnSuccess_ShouldThrow()
    {
        var result = Result.Ok();

        var act = () => result.FirstError;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*successful Result*");
    }

    [Fact]
    public void FirstError_OnFailure_ShouldReturnFirstError()
    {
        var error = new Error { Message = "Error", Code = "ERR001" };
        var result = Result.Fail(error);

        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void DebuggerDisplay_OnSuccess_ShouldShowOk()
    {
        var result = Result.Ok();

        result.DebuggerDisplay.Should().Be("OK");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWith1Error_ShouldShowSingleErrorMessage()
    {
        var result = Result.Fail(new Error { Message = "Something bad happened" });

        result.DebuggerDisplay.Should().Be("Fail(single error: 'Something bad happened')");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWithSeveralErrors_ShouldShowErrorCount()
    {
        var errors = new Error[]
        {
            new () { Message = "Error 1" },
            new () { Message = "Error 2" },
            new () { Message = "Error 3" }
        };

        var result = Result.Fail(errors);

        result.DebuggerDisplay.Should().Be("Fail(3 errors)");
    }
}
