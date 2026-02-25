using System;
using FluentAssertions;
using Light.PortableResults.FunctionalExtensions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests;

public sealed class GenericResultTests
{
    [Fact]
    public void Map_OnFailure_ShouldPreserveErrorsAndMetadata()
    {
        var metadata = MetadataObject.Create(("trace", "123"));
        var result = Result<int>.Fail(new Error { Message = "Error" }).ReplaceMetadata(metadata);

        var mapped = result.Map(x => x.ToString());

        mapped.IsValid.Should().BeFalse();
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Map_OnSuccess_ShouldPreserveMetadata()
    {
        var metadata = MetadataObject.Create(("trace", "123"));
        var result = Result<int>.Ok(42, metadata);

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be(84);
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Bind_OnFailure_ShouldPreserveErrorsAndMetadata()
    {
        var metadata = MetadataObject.Create(("trace", "123"));
        var result = Result<int>.Fail(new Error { Message = "Error" }).ReplaceMetadata(metadata);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeFalse();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Bind_OnSuccess_WithNoMetadataOnEither_ShouldReturnInnerResult()
    {
        var result = Result<int>.Ok(42);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeTrue();
        bound.Value.Should().Be("42");
        bound.Metadata.Should().BeNull();
    }

    [Fact]
    public void Bind_OnSuccess_WithMetadataOnOuter_ShouldSetMetadataOnInner()
    {
        var metadata = MetadataObject.Create(("trace", "123"));
        var result = Result<int>.Ok(42).ReplaceMetadata(metadata);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()));

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Bind_OnSuccess_WithMetadataOnBoth_ShouldMergeMetadata()
    {
        var outerMeta = MetadataObject.Create(("outer", "value"));
        var innerMeta = MetadataObject.Create(("inner", "value"));
        var result = Result<int>.Ok(42).ReplaceMetadata(outerMeta);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString()).ReplaceMetadata(innerMeta));

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void Tap_OnSuccess_ShouldExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Ok(42);

        var tapped = result.Tap(_ => executed = true);

        executed.Should().BeTrue();
        tapped.Should().Be(result);
    }

    [Fact]
    public void Tap_OnFailure_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = result.Tap(_ => executed = true);

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_OnFailure_ShouldExecuteAction()
    {
        Errors? capturedErrors = null;
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var tapped = result.TapError(errors => capturedErrors = errors);

        capturedErrors.Should().NotBeNull();
        capturedErrors!.Value.Should().ContainSingle();
        tapped.Should().Be(result);
    }

    [Fact]
    public void TapError_OnSuccess_ShouldNotExecuteAction()
    {
        var executed = false;
        var result = Result<int>.Ok(42);

        var tapped = result.TapError(_ => executed = true);

        executed.Should().BeFalse();
        tapped.Should().Be(result);
    }

    [Fact]
    public void ToString_OnSuccess_ShouldShowValue()
    {
        var result = Result<int>.Ok(42);

        result.ToString().Should().Be("Ok(42)");
    }

    [Fact]
    public void ToString_OnFailure_ShouldShowErrorCodes()
    {
        var result = Result<int>.Fail(new Error { Message = "Message", Code = "ERR001" });

        result.ToString().Should().Contain("Message");
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccess()
    {
        var result = new Result<int>(42);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFailure()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fail_WithEmptyArray_ShouldThrow()
    {
        var act = () => Result<int>.Fail(Array.Empty<Error>());

        act.Should().Throw<ArgumentException>().Where(x => x.ParamName == "manyErrors");
    }

    [Fact]
    public void Fail_WithSingleItemArray_ShouldCreateFailure()
    {
        var result = Result<int>.Fail(new[] { new Error { Message = "Error" } });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Fail_WithMultipleErrors_ShouldCreateFailure()
    {
        var result = Result<int>.Fail(new[] { new Error { Message = "Error1" }, new Error { Message = "Error2" } });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void WithMetadata_OnFailure_ShouldSetMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var withMeta = result.ReplaceMetadata(metadata);

        withMeta.IsValid.Should().BeFalse();
        withMeta.Metadata.Should().NotBeNull();
        withMeta.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void MergeMetadata_WhenNoExistingMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var merged = result.MergeMetadata(metadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Value_OnFailure_ShouldThrow()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" });

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*failed Result*");
    }

    [Fact]
    public void FirstError_OnSuccess_ShouldThrow()
    {
        var result = Result<int>.Ok(42);

        var act = () => result.FirstError;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*successful Result*");
    }

    [Fact]
    public void Errors_OnSuccess_ShouldReturnEmpty()
    {
        var result = Result<int>.Ok(42);

        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldThrow()
    {
        var act = () => new Result<string>(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("value");
    }

    [Fact]
    public void Constructor_WithDefaultErrors_ShouldThrow()
    {
        var act = () => new Result<int>(default(Errors));

        act.Should().Throw<ArgumentException>()
           .WithParameterName("errors")
           .WithMessage("*at least one error*");
    }

    [Fact]
    public void Fail_WithErrorsStruct_ShouldCreateFailure()
    {
        var errors = new Errors(new Error { Message = "Error" });

        var result = Result<int>.Fail(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
    }

    [Fact]
    public void Fail_WithErrorsStruct_AndMetadata_ShouldCreateFailureWithMetadata()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var metadata = MetadataObject.Create(("key", "value"));

        var result = Result<int>.Fail(errors, metadata);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().Equal(metadata);
    }

    [Fact]
    public void ClearMetadata_OnSuccess_ShouldRemoveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Ok(42, metadata);

        var cleared = result.ClearMetadata();

        cleared.IsValid.Should().BeTrue();
        cleared.Value.Should().Be(42);
        cleared.Metadata.Should().BeNull();
    }

    [Fact]
    public void ClearMetadata_OnFailure_ShouldRemoveMetadata()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result = Result<int>.Fail(new Error { Message = "Error" }, metadata);

        var cleared = result.ClearMetadata();

        cleared.IsValid.Should().BeFalse();
        cleared.Metadata.Should().BeNull();
    }

    [Fact]
    public void MergeMetadata_WithTuples_WhenNoExistingMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);

        var merged = result.MergeMetadata(("key", "value"));

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.TryGetString("key", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    [Fact]
    public void MergeMetadata_WithTuples_WhenExistingMetadata_ShouldMerge()
    {
        var metadata = MetadataObject.Create(("existing", "value"));
        var result = Result<int>.Ok(42, metadata);

        var merged = result.MergeMetadata(("new", "value"));

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void MergeMetadata_WithMetadataObject_WhenNoExistingMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var newMetadata = MetadataObject.Create(("key", "value"));

        var merged = result.MergeMetadata(newMetadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().Equal(newMetadata);
    }

    [Fact]
    public void MergeMetadata_WithMetadataObject_WhenExistingMetadata_ShouldMerge()
    {
        var existingMetadata = MetadataObject.Create(("existing", "value"));
        var result = Result<int>.Ok(42, existingMetadata);
        var newMetadata = MetadataObject.Create(("new", "value"));

        var merged = result.MergeMetadata(newMetadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Count.Should().Be(2);
    }

    [Fact]
    public void MergeMetadata_WithPreserveExistingStrategy_ShouldNotOverwrite()
    {
        var existingMetadata = MetadataObject.Create(("key", "original"));
        var result = Result<int>.Ok(42, existingMetadata);
        var newMetadata = MetadataObject.Create(("key", "new"));

        var merged = result.MergeMetadata(newMetadata, MetadataMergeStrategy.PreserveExisting);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.TryGetString("key", out var value).Should().BeTrue();
        value.Should().Be("original");
    }

    [Fact]
    public void DebuggerDisplay_OnSuccess_ShouldShowValue()
    {
        var result = Result<int>.Ok(42);

        result.DebuggerDisplay.Should().Be("OK('42')");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWithSingleError_ShouldShowErrorMessage()
    {
        var result = Result<int>.Fail(new Error { Message = "Something went wrong" });

        result.DebuggerDisplay.Should().Be("Fail(single error: 'Something went wrong')");
    }

    [Fact]
    public void DebuggerDisplay_OnFailureWithMultipleErrors_ShouldShowErrorCount()
    {
        var result = Result<int>.Fail(
            new[]
            {
                new Error { Message = "Error 1" },
                new Error { Message = "Error 2" },
                new Error { Message = "Error 3" }
            }
        );

        result.DebuggerDisplay.Should().Be("Fail(3 errors)");
    }

    [Fact]
    public void Bind_OnSuccess_WithMetadataOnInnerOnly_ShouldReturnInnerMetadata()
    {
        var innerMetadata = MetadataObject.Create(("inner", "value"));
        var result = Result<int>.Ok(42);

        var bound = result.Bind(x => Result<string>.Ok(x.ToString(), innerMetadata));

        bound.IsValid.Should().BeTrue();
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.Should().Equal(innerMetadata);
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FailedResults_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var error = new Error { Message = "Error" };
        var result1 = Result<int>.Fail(error, metadata1);
        var result2 = Result<int>.Fail(error, metadata2);

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_FailedResults_DifferentErrorMetadata_ShouldBeEqual()
    {
        var errorMetadata1 = MetadataObject.Create(("key", "value1"));
        var errorMetadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = errorMetadata1 };
        var error2 = new Error { Message = "Error", Metadata = errorMetadata2 };
        var result1 = Result<int>.Fail(error1);
        var result2 = Result<int>.Fail(error2);

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataTrue_FailedResults_DifferentErrorMetadata_ShouldNotBeEqual()
    {
        var errorMetadata1 = MetadataObject.Create(("key", "value1"));
        var errorMetadata2 = MetadataObject.Create(("key", "value2"));
        var error1 = new Error { Message = "Error", Metadata = errorMetadata1 };
        var error2 = new Error { Message = "Error", Metadata = errorMetadata2 };
        var result1 = Result<int>.Fail(error1);
        var result2 = Result<int>.Fail(error2);

        result1.Equals(result2, compareMetadata: true).Should().BeFalse();
    }

    [Fact]
    public void Equals_TwoSuccessfulResultsWithSameValue_ShouldBeEqual()
    {
        var result1 = Result<int>.Ok(42);
        var result2 = Result<int>.Ok(42);

        (result1 == result2).Should().BeTrue();
        (result1 != result2).Should().BeFalse();
        result1.Should().Be(result2);
    }

    [Fact]
    public void Equals_TwoSuccessfulResultsWithDifferentValues_ShouldNotBeEqual()
    {
        var result1 = Result<int>.Ok(42);
        var result2 = Result<int>.Ok(99);

        (result1 == result2).Should().BeFalse();
        (result1 != result2).Should().BeTrue();
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Equals_TwoFailedResultsWithSameError_ShouldBeEqual()
    {
        var error = new Error { Message = "Error", Code = "ERR001" };
        var result1 = Result<int>.Fail(error);
        var result2 = Result<int>.Fail(error);

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_TwoFailedResultsWithDifferentErrors_ShouldNotBeEqual()
    {
        var result1 = Result<int>.Fail(new Error { Message = "Error1" });
        var result2 = Result<int>.Fail(new Error { Message = "Error2" });

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_SuccessAndFailure_ShouldNotBeEqual()
    {
        var result1 = Result<int>.Ok(42);
        var result2 = Result<int>.Fail(new Error { Message = "Error" });

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithMetadata_SameMetadata_ShouldBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result<int>.Ok(42, metadata);
        var result2 = Result<int>.Ok(42, metadata);

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithMetadata_DifferentMetadata_ShouldNotBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result<int>.Ok(42, metadata1);
        var result2 = Result<int>.Ok(42, metadata2);

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithMetadata_OneWithMetadataOneWithout_ShouldNotBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result<int>.Ok(42, metadata);
        var result2 = Result<int>.Ok(42);

        result1.Equals(result2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_DifferentMetadata_ShouldBeEqual()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result<int>.Ok(42, metadata1);
        var result2 = Result<int>.Ok(42, metadata2);

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCompareMetadataFalse_OneWithMetadataOneWithout_ShouldBeEqual()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result<int>.Ok(42, metadata);
        var result2 = Result<int>.Ok(42);

        result1.Equals(result2, compareMetadata: false).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCustomComparer_ShouldUseComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var result1 = Result<string>.Ok("Hello");
        var result2 = Result<string>.Ok("HELLO");

        result1.Equals(result2, compareMetadata: true, valueComparer: comparer).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCustomComparer_DifferentValues_ShouldNotBeEqual()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var result1 = Result<string>.Ok("Hello");
        var result2 = Result<string>.Ok("World");

        result1.Equals(result2, compareMetadata: true, valueComparer: comparer).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_WithSameResult_ShouldBeEqual()
    {
        var result1 = Result<int>.Ok(42);
        object result2 = Result<int>.Ok(42);

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ObjectOverload_WithNull_ShouldBeFalse()
    {
        var result = Result<int>.Ok(42);

        result.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_WithDifferentType_ShouldBeFalse()
    {
        var result = Result<int>.Ok(42);

        // ReSharper disable once SuspiciousTypeConversion.Global -- OK in this test scenario
        result.Equals("not a result").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameSuccessfulResults_ShouldHaveSameHashCode()
    {
        var result1 = Result<int>.Ok(42);
        var result2 = Result<int>.Ok(42);

        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentSuccessfulResults_ShouldHaveDifferentHashCode()
    {
        var result1 = Result<int>.Ok(42);
        var result2 = Result<int>.Ok(99);

        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithSameMetadata_ShouldHaveSameHashCode()
    {
        var metadata = MetadataObject.Create(("key", "value"));
        var result1 = Result<int>.Ok(42, metadata);
        var result2 = Result<int>.Ok(42, metadata);

        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentMetadata_ShouldHaveDifferentHashCode()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result<int>.Ok(42, metadata1);
        var result2 = Result<int>.Ok(42, metadata2);

        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithIncludeMetadataFalse_DifferentMetadata_ShouldHaveSameHashCode()
    {
        var metadata1 = MetadataObject.Create(("key", "value1"));
        var metadata2 = MetadataObject.Create(("key", "value2"));
        var result1 = Result<int>.Ok(42, metadata1);
        var result2 = Result<int>.Ok(42, metadata2);

        result1.GetHashCode(includeMetadata: false).Should().Be(result2.GetHashCode(includeMetadata: false));
    }

    [Fact]
    public void GetHashCode_WithCustomComparer_ShouldUseComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var result1 = Result<string>.Ok("Hello");
        var result2 = Result<string>.Ok("HELLO");

        result1.GetHashCode(includeMetadata: true, equalityComparer: comparer)
           .Should().Be(result2.GetHashCode(includeMetadata: true, equalityComparer: comparer));
    }

    [Fact]
    public void GetHashCode_FailedResults_ShouldIncludeErrors()
    {
        var error1 = new Error { Message = "Error1" };
        var error2 = new Error { Message = "Error2" };
        var result1 = Result<int>.Fail(error1);
        var result2 = Result<int>.Fail(error2);

        result1.GetHashCode().Should().NotBe(result2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithNullMetadata_ShouldNotThrow()
    {
        var result = Result<int>.Ok(42);

        var act = () => result.GetHashCode();

        act.Should().NotThrow();
    }

    [Fact]
    public void ImplicitConversion_FromErrors_ShouldCreateFailure()
    {
        var errors = new Errors(new Error { Message = "Error" });
        var result = new Result<int>(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
    }

    [Fact]
    public void Equals_WithReferenceTypeValues_ShouldUseDefaultComparer()
    {
        var obj1 = new object();
        var obj2 = new object();
        var result1 = Result<object>.Ok(obj1);
        var result2 = Result<object>.Ok(obj1);
        var result3 = Result<object>.Ok(obj2);

        result1.Equals(result2).Should().BeTrue();
        result1.Equals(result3).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNullableValues_ShouldWork()
    {
        var result1 = Result<int?>.Ok(42);
        var result2 = Result<int?>.Ok(42);

        result1.Equals(result2).Should().BeTrue();
    }

    [Fact]
    public void Ok_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var result = Result<int>.Ok(42);

        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void Ok_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("requestId", "req-123"));
        var result = Result<int>.Ok(42, metadata);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("requestId", out var id).Should().BeTrue();
        id.Should().Be("req-123");
    }

    [Fact]
    public void WithMetadata_Object_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var withMeta = result.ReplaceMetadata(metadata);

        withMeta.Metadata.Should().NotBeNull();
        withMeta.Metadata!.Value.Should().ContainSingle();
    }

    [Fact]
    public void WithMetadata_Properties_ShouldAddMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("a", 1), ("b", 2));

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().HaveCount(2);
    }

    [Fact]
    public void WithMetadata_OnFailure_ShouldPreserveErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("key", "value"));

        result.IsValid.Should().BeFalse();
        result.Metadata.Should().NotBeNull();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void MergeMetadata_ShouldCombineMetadata()
    {
        var result = Result<int>.Ok(42).MergeMetadata(("a", 1));

        var additional = MetadataObject.Create(("b", 2));
        var merged = result.MergeMetadata(additional);

        var expected = MetadataObject.Create(("a", 1), ("b", 2));
        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().Equal(expected);
    }

    [Fact]
    public void MergeMetadata_OnNullMetadata_ShouldSetMetadata()
    {
        var result = Result<int>.Ok(42);
        var metadata = MetadataObject.Create(("key", "value"));

        var merged = result.MergeMetadata(metadata);

        merged.Metadata.Should().NotBeNull();
        merged.Metadata!.Value.Should().ContainSingle();
        merged.Metadata.Value.Should().Equal(metadata);
    }

    [Fact]
    public void Map_ShouldPreserveMetadata()
    {
        var result = Result<int>.Ok(10).MergeMetadata(("source", "test"));

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeTrue();
        mapped.Value.Should().Be(20);
        mapped.Metadata.Should().NotBeNull();
        mapped.Metadata!.Value.TryGetString("source", out var source).Should().BeTrue();
        source.Should().Be("test");
    }

    [Fact]
    public void Map_OnFailure_ShouldPreserveMetadataAndErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "test"));

        var mapped = result.Map(x => x * 2);

        mapped.IsValid.Should().BeFalse();
        mapped.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Bind_ShouldMergeMetadata()
    {
        var result = Result<int>.Ok(10).MergeMetadata(("outer", "a"));

        var bound = result.Bind(
            x => Result<int>.Ok(x * 2).MergeMetadata(("inner", "b"))
        );

        bound.IsValid.Should().BeTrue();
        bound.Value.Should().Be(20);
        bound.Metadata.Should().NotBeNull();
        bound.Metadata!.Value.TryGetString("outer", out var outer).Should().BeTrue();
        outer.Should().Be("a");
        bound.Metadata.Value.TryGetString("inner", out var inner).Should().BeTrue();
        inner.Should().Be("b");
    }

    [Fact]
    public void Bind_OnFailure_ShouldPreserveMetadataAndErrors()
    {
        var result = Result<int>.Fail(new Error { Message = "Error" }).MergeMetadata(("context", "test"));

        var bound = result.Bind(x => Result<int>.Ok(x * 2));

        bound.IsValid.Should().BeFalse();
        bound.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Value_OnFailure_ShouldThrowWithMessage()
    {
        var result = Result<int>.Fail(new Error { Message = "Failed" });

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot access Value on a failed Result*");
    }

    [Fact]
    public void FirstError_OnSuccess_ShouldThrowWithMessage()
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
