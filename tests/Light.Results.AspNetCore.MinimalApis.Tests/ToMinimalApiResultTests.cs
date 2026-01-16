using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class ToMinimalApiResultTests
{
    [Fact]
    public void SuccessResult_ReturnsNoContent()
    {
        var result = Result.Ok();

        var apiResult = result.ToMinimalApiResult();

        apiResult.Should().BeOfType<NoContent>();
    }

    [Fact]
    public void FailedResult_ReturnsLightProblemDetails()
    {
        var result = Result.Fail(new Error { Message = "Forbidden", Category = ErrorCategory.Forbidden });

        var apiResult = result.ToMinimalApiResult();

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
        var problemDetails = (LightProblemDetailsResult) apiResult;
        problemDetails.Status.Should().Be(403);
    }

    [Fact]
    public void SuccessResult_WithCustomFactory_InvokesFactory()
    {
        var result = Result.Ok();

        var apiResult = result.ToMinimalApiResult(_ => TypedResults.Accepted("/status"));

        apiResult.Should().BeOfType<Accepted>();
    }

    [Fact]
    public void FailedResult_WithCustomFactory_ReturnsLightProblemDetails()
    {
        var result = Result.Fail(new Error { Message = "Error", Category = ErrorCategory.Conflict });

        var apiResult = result.ToMinimalApiResult(_ => TypedResults.Accepted("/status"));

        apiResult.Should().BeOfType<LightProblemDetailsResult>();
        var problemDetails = (LightProblemDetailsResult) apiResult;
        problemDetails.Status.Should().Be(409);
    }

    [Fact]
    public void NullOnSuccess_ThrowsArgumentNullException()
    {
        var result = Result.Ok();

        var act = () => result.ToMinimalApiResult(onSuccess: null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
