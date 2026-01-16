using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

public sealed class ToProblemDetailsTests
{
    public sealed class ForResult
    {
        [Fact]
        public void FailedResult_ProducesCorrectProblemDetails()
        {
            var result = Result.Fail(new Error { Message = "Not found", Category = ErrorCategory.NotFound });

            var problemDetails = result.ToProblemDetails();

            problemDetails.Status.Should().Be(404);
            problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.5");
            problemDetails.Title.Should().Be("Not Found");
            problemDetails.Detail.Should().Be("Not found");
        }

        [Fact]
        public void SuccessResult_ThrowsInvalidOperationException()
        {
            var result = Result.Ok();

            var act = () => result.ToProblemDetails();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Cannot convert a successful result to ProblemDetails.");
        }

        [Fact]
        public void FailedResult_WithMetadata_AddsToExtensions()
        {
            var metadata = MetadataObject.Create(("traceId", "abc123"));
            var result = Result.Fail(new Error { Message = "Error", Category = ErrorCategory.InternalError }, metadata);

            var problemDetails = result.ToProblemDetails();

            problemDetails.Extensions.Should().ContainKey("traceId");
            problemDetails.Extensions["traceId"].Should().Be("abc123");
        }

        [Fact]
        public void FailedResult_ErrorsInExtensions()
        {
            var result = Result.Fail(new Error { Message = "Error 1", Category = ErrorCategory.Validation });

            var problemDetails = result.ToProblemDetails();

            problemDetails.Extensions.Should().ContainKey("errors");
            var errors = problemDetails.Extensions["errors"] as List<object>;
            errors.Should().NotBeNull();
            errors!.Count.Should().Be(1);
        }
    }

    public sealed class ForResultT
    {
        [Fact]
        public void FailedResult_ProducesCorrectProblemDetails()
        {
            var result = Result<string>.Fail(
                new Error { Message = "Validation failed", Category = ErrorCategory.Validation }
            );

            var problemDetails = result.ToProblemDetails();

            problemDetails.Status.Should().Be(400);
            problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
            problemDetails.Title.Should().Be("Bad Request");
            problemDetails.Detail.Should().Be("Validation failed");
        }

        [Fact]
        public void SuccessResult_ThrowsInvalidOperationException()
        {
            var result = Result<string>.Ok("value");

            var act = () => result.ToProblemDetails();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Cannot convert a successful result to ProblemDetails.");
        }

        [Fact]
        public void FailedResult_WithErrorMetadata_IncludesInErrorObject()
        {
            var errorMetadata = MetadataObject.Create(("attemptedValue", 42));
            var result = Result<string>.Fail(
                new Error
                {
                    Message = "Invalid value",
                    Category = ErrorCategory.Validation,
                    Metadata = errorMetadata
                }
            );

            var problemDetails = result.ToProblemDetails();

            var errors = problemDetails.Extensions["errors"] as List<object>;
            errors.Should().NotBeNull();
            var firstError = errors![0] as Dictionary<string, object?>;
            firstError.Should().NotBeNull();
            firstError!["metadata"].Should().NotBeNull();
        }

        [Fact]
        public void FirstCategoryIsLeadingCategory_UsesFirstErrorCategory()
        {
            var result = Result<string>.Fail(
                new Errors(
                    new Error[]
                    {
                        new () { Message = "Error 1", Category = ErrorCategory.NotFound },
                        new () { Message = "Error 2", Category = ErrorCategory.Validation }
                    }
                )
            );

            var problemDetails = result.ToProblemDetails(firstCategoryIsLeadingCategory: true);

            problemDetails.Status.Should().Be(404);
        }
    }
}
