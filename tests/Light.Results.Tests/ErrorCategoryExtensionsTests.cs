using System;
using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class ErrorCategoryExtensionsTests
{
    [Fact]
    public void GetLeadingCategory_SingleError_ReturnsItsCategory()
    {
        var errors = new Errors(new Error { Message = "Test", Category = ErrorCategory.NotFound });

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.NotFound);
    }

    [Fact]
    public void GetLeadingCategory_MultipleErrorsWithSameCategory_ReturnsThatCategory()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.Validation }
            }
        );

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void GetLeadingCategory_MultipleErrorsWithDifferentCategories_ReturnsUnclassified()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.NotFound }
            }
        );

        var result = errors.GetLeadingCategory();

        result.Should().Be(ErrorCategory.Unclassified);
    }

    [Fact]
    public void GetLeadingCategory_FirstCategoryIsLeadingCategory_ReturnsFirstErrorCategory()
    {
        var errors = new Errors(
            new Error[]
            {
                new () { Message = "Error 1", Category = ErrorCategory.Validation },
                new () { Message = "Error 2", Category = ErrorCategory.NotFound }
            }
        );

        var result = errors.GetLeadingCategory(firstCategoryIsLeadingCategory: true);

        result.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void GetLeadingCategory_EmptyErrors_ThrowsInvalidOperationException()
    {
        var errors = default(Errors);

        var act = () => errors.GetLeadingCategory();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Errors collection must contain at least one error.");
    }

    [Theory]
    [InlineData(ErrorCategory.Validation, 400)]
    [InlineData(ErrorCategory.Unauthorized, 401)]
    [InlineData(ErrorCategory.Forbidden, 403)]
    [InlineData(ErrorCategory.NotFound, 404)]
    [InlineData(ErrorCategory.Timeout, 408)]
    [InlineData(ErrorCategory.Conflict, 409)]
    [InlineData(ErrorCategory.Gone, 410)]
    [InlineData(ErrorCategory.PreconditionFailed, 412)]
    [InlineData(ErrorCategory.ContentTooLarge, 413)]
    [InlineData(ErrorCategory.UriTooLong, 414)]
    [InlineData(ErrorCategory.UnsupportedMediaType, 415)]
    [InlineData(ErrorCategory.UnprocessableEntity, 422)]
    [InlineData(ErrorCategory.RateLimited, 429)]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, 451)]
    [InlineData(ErrorCategory.InternalError, 500)]
    [InlineData(ErrorCategory.NotImplemented, 501)]
    [InlineData(ErrorCategory.BadGateway, 502)]
    [InlineData(ErrorCategory.ServiceUnavailable, 503)]
    [InlineData(ErrorCategory.GatewayTimeout, 504)]
    public void ToHttpStatusCode_MapsToCorrectHttpStatusCode(ErrorCategory category, int expectedStatusCode)
    {
        var result = category.ToHttpStatusCode();

        result.Should().Be(expectedStatusCode);
    }

    [Fact]
    public void ToHttpStatusCode_Unclassified_MapsTo500()
    {
        var result = ErrorCategory.Unclassified.ToHttpStatusCode();

        result.Should().Be(500);
    }
}
