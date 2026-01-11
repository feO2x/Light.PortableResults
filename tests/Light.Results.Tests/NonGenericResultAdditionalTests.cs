using FluentAssertions;
using Light.Results.Metadata;

namespace Light.Results.Tests;

public sealed class NonGenericResultAdditionalTests
{
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
}
