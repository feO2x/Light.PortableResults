using System;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class GenericResultAdditionalTests
{
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
}
