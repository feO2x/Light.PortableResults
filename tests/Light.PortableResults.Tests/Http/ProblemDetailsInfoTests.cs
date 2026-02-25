using System;
using System.Net;
using FluentAssertions;
using Light.PortableResults.Http;
using Xunit;

namespace Light.PortableResults.Tests.Http;

public sealed class ProblemDetailsInfoTests
{
    [Fact]
    public void CreateDefault_WithValidationError_ShouldReturnCorrectInfo()
    {
        var errors = new Errors(new Error { Message = "Validation failed", Category = ErrorCategory.Validation });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.1");
        problemDetails.Status.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Title.Should().Be("Bad Request");
        problemDetails.Detail.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public void CreateDefault_WithNotFoundError_ShouldReturnCorrectInfo()
    {
        var errors = new Errors(new Error { Message = "Resource not found", Category = ErrorCategory.NotFound });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.5");
        problemDetails.Status.Should().Be(HttpStatusCode.NotFound);
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Be("The requested resource was not found.");
    }

    [Fact]
    public void CreateDefault_WithUnauthorizedError_ShouldReturnCorrectInfo()
    {
        var errors = new Errors(new Error { Message = "Unauthorized access", Category = ErrorCategory.Unauthorized });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.5.2");
        problemDetails.Status.Should().Be(HttpStatusCode.Unauthorized);
        problemDetails.Title.Should().Be("Unauthorized");
        problemDetails.Detail.Should().Be("Authentication is required to access this resource.");
    }

    [Fact]
    public void CreateDefault_WithEmptyErrors_ShouldThrowArgumentException()
    {
        var errors = default(Errors);

        var act = () => ProblemDetailsInfo.CreateDefault(errors);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("errors")
           .WithMessage("*must contain at least one error*");
    }

    [Fact]
    public void CreateDefault_WithFirstCategoryIsLeadingCategory_ShouldUseFirstError()
    {
        var errorsArray = new[]
        {
            new Error { Message = "Validation failed", Category = ErrorCategory.Validation },
            new Error { Message = "Not found", Category = ErrorCategory.NotFound }
        };
        var errors = new Errors(errorsArray);

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors, firstCategoryIsLeadingCategory: true);

        problemDetails.Status.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Title.Should().Be("Bad Request");
    }

    [Fact]
    public void CreateDefault_WithoutFirstCategoryIsLeadingCategory_ShouldUseCommonCategoryIfAllMatch()
    {
        var errorsArray = new[]
        {
            new Error { Message = "Validation error 1", Category = ErrorCategory.Validation },
            new Error { Message = "Validation error 2", Category = ErrorCategory.Validation }
        };
        var errors = new Errors(errorsArray);

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors, firstCategoryIsLeadingCategory: false);

        problemDetails.Status.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Title.Should().Be("Bad Request");
    }

    [Fact]
    public void CreateDefault_WithMixedCategories_AndFirstCategoryIsLeadingCategoryFalse_ShouldUseUnclassified()
    {
        var errorsArray = new[]
        {
            new Error { Message = "Validation failed", Category = ErrorCategory.Validation },
            new Error { Message = "Not found", Category = ErrorCategory.NotFound }
        };
        var errors = new Errors(errorsArray);

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors, firstCategoryIsLeadingCategory: false);

        problemDetails.Status.Should().Be(HttpStatusCode.InternalServerError);
        problemDetails.Title.Should().Be("Internal Server Error");
    }

    [Fact]
    public void CreateDefault_WithInternalError_ShouldReturnCorrectInfo()
    {
        var errors = new Errors(new Error { Message = "Internal error", Category = ErrorCategory.InternalError });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Type.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
        problemDetails.Status.Should().Be(HttpStatusCode.InternalServerError);
        problemDetails.Title.Should().Be("Internal Server Error");
        problemDetails.Detail.Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public void ProblemDetailsInfo_CanBeCreatedWithOptionalProperties()
    {
        var problemDetails = new ProblemDetailsInfo
        {
            Type = "https://example.com/problem",
            Status = HttpStatusCode.BadRequest,
            Title = "Test Problem",
            Detail = "Detailed description",
            Instance = "/requests/12345"
        };

        problemDetails.Type.Should().Be("https://example.com/problem");
        problemDetails.Status.Should().Be(HttpStatusCode.BadRequest);
        problemDetails.Title.Should().Be("Test Problem");
        problemDetails.Detail.Should().Be("Detailed description");
        problemDetails.Instance.Should().Be("/requests/12345");
    }

    [Fact]
    public void ProblemDetailsInfo_DetailAndInstance_CanBeNull()
    {
        var problemDetails = new ProblemDetailsInfo
        {
            Type = "https://example.com/problem",
            Status = HttpStatusCode.NotFound,
            Title = "Not Found"
        };

        problemDetails.Detail.Should().BeNull();
        problemDetails.Instance.Should().BeNull();
    }

    [Theory]
    [InlineData(ErrorCategory.PaymentRequired)]
    [InlineData(ErrorCategory.Forbidden)]
    [InlineData(ErrorCategory.MethodNotAllowed)]
    [InlineData(ErrorCategory.NotAcceptable)]
    [InlineData(ErrorCategory.Timeout)]
    [InlineData(ErrorCategory.Conflict)]
    [InlineData(ErrorCategory.Gone)]
    [InlineData(ErrorCategory.LengthRequired)]
    [InlineData(ErrorCategory.PreconditionFailed)]
    [InlineData(ErrorCategory.ContentTooLarge)]
    [InlineData(ErrorCategory.UriTooLong)]
    [InlineData(ErrorCategory.UnsupportedMediaType)]
    [InlineData(ErrorCategory.RequestedRangeNotSatisfiable)]
    [InlineData(ErrorCategory.ExpectationFailed)]
    [InlineData(ErrorCategory.MisdirectedRequest)]
    [InlineData(ErrorCategory.UnprocessableContent)]
    [InlineData(ErrorCategory.Locked)]
    [InlineData(ErrorCategory.FailedDependency)]
    [InlineData(ErrorCategory.UpgradeRequired)]
    [InlineData(ErrorCategory.PreconditionRequired)]
    [InlineData(ErrorCategory.TooManyRequests)]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge)]
    [InlineData(ErrorCategory.UnavailableForLegalReasons)]
    [InlineData(ErrorCategory.NotImplemented)]
    [InlineData(ErrorCategory.BadGateway)]
    [InlineData(ErrorCategory.ServiceUnavailable)]
    [InlineData(ErrorCategory.GatewayTimeout)]
    [InlineData(ErrorCategory.InsufficientStorage)]
    public void CreateDefault_WithVariousCategories_ShouldReturnValidProblemDetails(ErrorCategory category)
    {
        var errors = new Errors(new Error { Message = "Test error", Category = category });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Type.Should().NotBeNullOrEmpty();
        problemDetails.Title.Should().NotBeNullOrEmpty();
        problemDetails.Detail.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateDefault_WithUnclassifiedError_ShouldReturnInternalServerError()
    {
        var errors = new Errors(new Error { Message = "Unclassified error", Category = ErrorCategory.Unclassified });

        var problemDetails = ProblemDetailsInfo.CreateDefault(errors);

        problemDetails.Status.Should().Be(HttpStatusCode.InternalServerError);
        problemDetails.Title.Should().Be("Internal Server Error");
    }
}
