using System;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests;

public sealed class ResultObjectExtensionsTests
{
    [Fact]
    public void MustNotBeDefaultInstanceShouldRejectDefaultResultWithReferenceTypeValue()
    {
        var result = default(Result<string>);

        var act = () => result.MustNotBeDefaultInstance();

        act.Should()
           .Throw<ArgumentException>()
           .WithParameterName("result")
           .WithMessage("*default instance*Result.Ok or Result.Fail*");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldRejectDefaultResultWithNullableValueTypeValue()
    {
        var result = default(Result<int?>);

        var act = () => result.MustNotBeDefaultInstance();

        act.Should().Throw<ArgumentException>().WithParameterName("result");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldRejectAnyResultObjectThatIsInvalidWithoutErrors()
    {
        var result = new ResultObjectStub(isValid: false, default);

        var act = () => result.MustNotBeDefaultInstance();

        act.Should().Throw<ArgumentException>().WithParameterName("result");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldUseTheSpecifiedParameterName()
    {
        var act = () => default(Result<string>).MustNotBeDefaultInstance("envelope");

        act.Should().Throw<ArgumentException>().WithParameterName("envelope");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldReturnSuccessfulResultUnchanged()
    {
        var result = Result<string>.Ok("value");

        var returnValue = result.MustNotBeDefaultInstance();

        returnValue.Should().Be(result);
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldReturnFailedResultUnchanged()
    {
        var result = Result<string>.Fail(new Error { Message = "Something went wrong" });

        var returnValue = result.MustNotBeDefaultInstance();

        returnValue.Should().Be(result);
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldAcceptSuccessfulNonGenericResult()
    {
        var result = Result.Ok();

        var returnValue = result.MustNotBeDefaultInstance();

        returnValue.Should().Be(result);
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldAcceptDefaultNonGenericResultBecauseItIsASuccess()
    {
        var result = default(Result);

        var returnValue = result.MustNotBeDefaultInstance();

        returnValue.Should().Be(Result.Ok());
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldAcceptDefaultResultWithNonNullableValueTypeBecauseItIsASuccess()
    {
        var result = default(Result<int>);

        var returnValue = result.MustNotBeDefaultInstance();

        returnValue.Should().Be(Result<int>.Ok(0));
    }

    private readonly struct ResultObjectStub : IResultObject
    {
        public ResultObjectStub(bool isValid, Errors errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }
        public Errors Errors { get; }
        public bool HasValue => false;
        public MetadataObject? Metadata => null;
    }
}
