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
           .WithMessage("*invalid while carrying no errors*default instance*Result.Ok or Result.Fail*");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldRejectDefaultResultWithNullableValueTypeValue()
    {
        var result = default(Result<int?>);

        var act = () => result.MustNotBeDefaultInstance();

        act.Should()
           .Throw<ArgumentException>()
           .WithParameterName("result")
           .WithMessage("*invalid while carrying no errors*default instance*Result.Ok or Result.Fail*");
    }

    [Fact]
    public void MustNotBeDefaultInstanceShouldRejectAnyResultObjectThatIsInvalidWithoutErrors()
    {
        var result = new ResultObjectStub(isValid: false, default, nonDefaultMarker: 1);

        result.Should().NotBe(default(ResultObjectStub));
        var act = () => result.MustNotBeDefaultInstance();

        act.Should()
           .Throw<ArgumentException>()
           .WithParameterName("result")
           .WithMessage("*invalid while carrying no errors*default instance*Result.Ok or Result.Fail*");
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
        private readonly int _nonDefaultMarker;

        public ResultObjectStub(bool isValid, Errors errors, int nonDefaultMarker)
        {
            IsValid = isValid;
            Errors = errors;
            _nonDefaultMarker = nonDefaultMarker;
        }

        public bool IsValid { get; }
        public Errors Errors { get; }
        public bool HasValue => _nonDefaultMarker < 0;
        public MetadataObject? Metadata => null;
    }
}
