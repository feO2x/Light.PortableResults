using System;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class NonGenericResultTests
{
    [Fact]
    public void IsFailure_OnFailure_ShouldBeTrue()
    {
        var result = Result.Fail(new Error { Message = "Error" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Errors_OnFailure_ShouldContainErrors()
    {
        var result = Result.Fail(new Error { Message = "Error" });

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Fail_WithMultipleErrors_ShouldWork()
    {
        var result = Result.Fail(new[] { new Error { Message = "E1" }, new Error { Message = "E2" } });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Ok_WithMetadata_ShouldSetMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result.Ok(metadata);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void WithMetadata_Object_ShouldSetMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result.Ok();

        var withMeta = result.ReplaceMetadata(metadata);

        withMeta.Metadata.Should().NotBeNull();
        withMeta.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void MergeMetadata_ShouldMergeCorrectly()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));
        var additional = MetadataObject.Create(("b", 2));

        var merged = result.MergeMetadata(additional);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void MergeMetadata_WithStrategy_ShouldUseStrategy()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));
        var additional = MetadataObject.Create(("a", 2));

        var merged = result.MergeMetadata(additional, MetadataMergeStrategy.PreserveExisting);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.TryGetInt64("a", out var value).Should().BeTrue();
        value.Should().Be(1);
    }

    [Fact]
    public void Fail_WithErrorsStruct_ShouldCreateFailure()
    {
        var errors = new Errors(new Error { Message = "Error" });

        var result = Result.Fail(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
    }

    [Fact]
    public void Fail_WithErrorsStruct_AndMetadata_ShouldCreateFailureWithMetadata()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result.Fail(errors, metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void TapError_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result.Fail(new Error { Message = "Error" });

        var tapped = result.TapError(errors => capturedErrors = errors);

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
        tapped.Errors.Should().Equal(result.Errors);
    }

    [Fact]
    public void TapError_OnSuccess_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result.Ok();

        var tapped = result.TapError(_ => executed = true);

        executed.Should().BeFalse();
        tapped.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fail_WithMultipleErrors_AndMetadata_ShouldCreateFailureWithMetadata()
    {
        var errorsArray = new[] { new Error { Message = "E1" }, new Error { Message = "E2" } };
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result.Fail(errorsArray, metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Fail_WithSingleError_AndMetadata_ShouldCreateFailureWithMetadata()
    {
        var error = new Error { Message = "Error" };
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result.Fail(error, metadata);

        result.IsValid.Should().BeFalse();
        result.FirstError.Should().Be(error);
        result.Metadata.Should().NotBeNull();
    }

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

    [Fact]
    public void Ok_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var result = Result.Ok();

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Ok_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("requestId", "req-456"));
        var result = Result.Ok(metadata);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("requestId", out var id).Should().BeTrue();
        id.Should().Be("req-456");
    }

    [Fact]
    public void WithMetadata_ShouldSetMetadata()
    {
        var result = Result.Ok().MergeMetadata(("key", "value"));

        var expected = MetadataObject.Create(("key", "value"));
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(expected);
    }

    [Fact]
    public void MergeMetadata_ShouldCombineMetadata()
    {
        var result = Result.Ok().MergeMetadata(("a", 1));

        var additional = MetadataObject.Create(("b", 2));
        var merged = result.MergeMetadata(additional);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().HaveCount(2);
        merged.Metadata.Value.Should().Equal(expected);
    }

    [Fact]
    public void Fail_WithMetadata_ShouldPreserveMetadata()
    {
        var result = Result.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "failure"));

        var expected = MetadataObject.Create(("context", "failure"));
        result.IsValid.Should().BeFalse();
        result.Metadata.Should().NotBeNull();
        result.Metadata.Value.Should().Equal(expected);
    }

    [Fact]
    public void ToString_OnSuccess_ShouldReturnOk()
    {
        var result = Result.Ok();

        result.ToString().Should().Be("OK");
    }

    [Fact]
    public void ToString_OnFailureWithSingleError_ShouldReturnFailWithMessage()
    {
        var result = Result.Fail(new Error { Message = "Something went wrong" });

        result.ToString().Should().Be("Fail(Something went wrong)");
    }

    [Fact]
    public void ToString_OnFailureWithMultipleErrors_ShouldReturnFailWithAllMessages()
    {
        var errors = new Error[]
        {
            new () { Message = "First error" },
            new () { Message = "Second error" }
        };
        var result = Result.Fail(errors);

        result.ToString().Should().Be("Fail(First error, Second error)");
    }
}
