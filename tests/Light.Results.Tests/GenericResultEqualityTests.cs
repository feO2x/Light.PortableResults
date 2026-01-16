using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class GenericResultEqualityTests
{
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
}
