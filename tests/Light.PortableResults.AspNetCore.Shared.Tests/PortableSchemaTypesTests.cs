using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.AspNetCore.Shared.Tests;

public sealed class PortableSchemaTypesTests
{
    [Fact]
    public void PortableError_ShouldExposeAssignedValues()
    {
        var sut = new PortableError
        {
            Message = "Validation failed",
            Code = "too_short",
            Target = "name",
            Category = ErrorCategory.Validation,
            Metadata = new { MinLength = 3 }
        };

        sut.Should().BeEquivalentTo(
            new
            {
                Message = "Validation failed",
                Code = "too_short",
                Target = "name",
                Category = ErrorCategory.Validation,
                Metadata = new { MinLength = 3 }
            }
        );
    }

    [Fact]
    public void PortableErrorOfT_ShouldExposeAssignedValues()
    {
        var sut = new PortableError<ErrorMetadata>
        {
            Message = "Validation failed",
            Code = "too_short",
            Target = "name",
            Category = ErrorCategory.Validation,
            Metadata = new ErrorMetadata { MinLength = 3 }
        };

        sut.Should().BeEquivalentTo(
            new PortableError<ErrorMetadata>
            {
                Message = "Validation failed",
                Code = "too_short",
                Target = "name",
                Category = ErrorCategory.Validation,
                Metadata = new ErrorMetadata { MinLength = 3 }
            }
        );
    }

    [Fact]
    public void PortableProblemDetails_ShouldInitializeErrorsAndExposeMetadata()
    {
        var metadata = new ProblemMetadata { TraceId = "trace-42" };
        var error = new PortableError<ErrorMetadata>
        {
            Message = "Not found",
            Code = "not_found",
            Target = "contactId",
            Category = ErrorCategory.NotFound,
            Metadata = new ErrorMetadata { MinLength = 0 }
        };
        var sut = new PortableProblemDetails<ErrorMetadata, ProblemMetadata>
        {
            Title = "Problem",
            Status = 404,
            Errors = new[] { error },
            Metadata = metadata
        };

        sut.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(error);
        sut.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void PortableProblemDetails_DefaultVariantShouldUseObjectMetadataTypes()
    {
        var sut = new PortableProblemDetails();

        sut.Errors.Should().BeEmpty();
        sut.Metadata.Should().BeNull();
    }

    [Fact]
    public void PortableRichValidationProblemDetails_ShouldInitializeErrorsAndExposeMetadata()
    {
        var metadata = new ProblemMetadata { TraceId = "trace-43" };
        var error = new PortableError<ErrorMetadata>
        {
            Message = "Too short",
            Code = "too_short",
            Target = "name",
            Category = ErrorCategory.Validation,
            Metadata = new ErrorMetadata { MinLength = 3 }
        };
        var sut = new PortableRichValidationProblemDetails<ErrorMetadata, ProblemMetadata>
        {
            Title = "Validation failed",
            Status = 400,
            Errors = new[] { error },
            Metadata = metadata
        };

        sut.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(error);
        sut.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void PortableRichValidationProblemDetails_DefaultVariantShouldUseObjectMetadataTypes()
    {
        var sut = new PortableRichValidationProblemDetails();

        sut.Errors.Should().BeEmpty();
        sut.Metadata.Should().BeNull();
    }

    [Fact]
    public void PortableAspNetCoreValidationProblemDetails_ShouldExposeErrorDetailsAndMetadata()
    {
        var errorDetail = new PortableValidationErrorDetail<ErrorMetadata>
        {
            Target = "name",
            Index = 1,
            Code = "too_short",
            Category = ErrorCategory.Validation,
            Metadata = new ErrorMetadata { MinLength = 3 }
        };
        var metadata = new ProblemMetadata { TraceId = "trace-44" };
        var sut = new PortableAspNetCoreValidationProblemDetails<ErrorMetadata, ProblemMetadata>
        {
            ErrorDetails = new[] { errorDetail },
            Metadata = metadata
        };

        sut.ErrorDetails.Should().ContainSingle().Which.Should().BeEquivalentTo(errorDetail);
        sut.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public void PortableAspNetCoreValidationProblemDetails_DefaultVariantShouldUseObjectMetadataTypes()
    {
        var sut = new PortableAspNetCoreValidationProblemDetails();

        sut.ErrorDetails.Should().BeNull();
        sut.Metadata.Should().BeNull();
    }

    [Fact]
    public void PortableValidationErrorDetail_ShouldExposeAssignedValues()
    {
        var sut = new PortableValidationErrorDetail
        {
            Target = "name",
            Index = 2,
            Code = "too_short",
            Category = ErrorCategory.Validation,
            Metadata = new { MinLength = 3 }
        };

        sut.Should().BeEquivalentTo(
            new
            {
                Target = "name",
                Index = 2,
                Code = "too_short",
                Category = ErrorCategory.Validation,
                Metadata = new { MinLength = 3 }
            }
        );
    }

    [Fact]
    public void PortableValidationErrorDetailOfT_ShouldExposeAssignedValues()
    {
        var sut = new PortableValidationErrorDetail<ErrorMetadata>
        {
            Target = "name",
            Index = 2,
            Code = "too_short",
            Category = ErrorCategory.Validation,
            Metadata = new ErrorMetadata { MinLength = 3 }
        };

        sut.Should().BeEquivalentTo(
            new PortableValidationErrorDetail<ErrorMetadata>
            {
                Target = "name",
                Index = 2,
                Code = "too_short",
                Category = ErrorCategory.Validation,
                Metadata = new ErrorMetadata { MinLength = 3 }
            }
        );
    }

    [Fact]
    public void PortableSuccessResponse_ShouldExposeValueAndMetadata()
    {
        var value = new SuccessValue { Name = "Alice" };
        var metadata = new ProblemMetadata { TraceId = "trace-45" };
        var sut = new PortableSuccessResponse<SuccessValue, ProblemMetadata>
        {
            Value = value,
            Metadata = metadata
        };

        sut.Should().BeEquivalentTo(
            new PortableSuccessResponse<SuccessValue, ProblemMetadata>
            {
                Value = value,
                Metadata = metadata
            }
        );
    }

    [Fact]
    public void PortableProblemDetails_GenericErrorsPropertyShouldAcceptReadOnlyCollections()
    {
        IReadOnlyList<PortableError<ErrorMetadata>> errors =
        [
            new()
            {
                Message = "Conflict",
                Code = "duplicate",
                Target = "email",
                Category = ErrorCategory.Conflict,
                Metadata = new ErrorMetadata { MinLength = 0 }
            }
        ];
        var sut = new PortableProblemDetails<ErrorMetadata, ProblemMetadata>
        {
            Errors = errors,
            Metadata = new ProblemMetadata { TraceId = "trace-46" }
        };

        sut.Errors.Should().BeSameAs(errors);
    }

    private sealed class ErrorMetadata
    {
        public int MinLength { get; init; }
    }

    private sealed class ProblemMetadata
    {
        public string TraceId { get; init; } = string.Empty;
    }

    private sealed class SuccessValue
    {
        public string Name { get; init; } = string.Empty;
    }
}
