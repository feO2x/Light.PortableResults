using System;
using FluentAssertions;
using Light.Results.FunctionalExtensions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

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
}
