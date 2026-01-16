using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class ToMinimalApiResultOfTTests
{
    [Fact]
    public void SuccessResult_ReturnsOkWithValue()
    {
        var result = Result<string>.Ok("Hello");

        var apiResult = result.ToMinimalApiResult();

        apiResult.Should().BeOfType<Ok<string>>();
        var okResult = (Ok<string>) apiResult;
        okResult.Value.Should().Be("Hello");
    }

    [Fact]
    public void FailedResult_ReturnsLightProblemDetails()
    {
        var result = Result<string>.Fail(new Error { Message = "Not found", Category = ErrorCategory.NotFound });

        var apiResult = result.ToMinimalApiResult();

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
        var problemDetails = (LightProblemDetailsResult) apiResult;
        problemDetails.Status.Should().Be(404);
    }

    [Fact]
    public void SuccessResult_WithCustomFactory_InvokesFactory()
    {
        var result = Result<int>.Ok(42);

        var apiResult = result.ToMinimalApiResult(r => TypedResults.Created($"/items/{r.Value}", r.Value));

        apiResult.Should().BeOfType<Created<int>>();
    }

    [Fact]
    public void FailedResult_WithCustomFactory_ReturnsLightProblemDetails()
    {
        var result = Result<int>.Fail(new Error { Message = "Error", Category = ErrorCategory.Validation });

        var apiResult = result.ToMinimalApiResult(value => TypedResults.Created($"/items/{value}", value));

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
    }

    [Fact]
    public void NullOnSuccess_ThrowsArgumentNullException()
    {
        var result = Result<int>.Ok(42);

        var act = () => result.ToMinimalApiResult(onSuccess: null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
