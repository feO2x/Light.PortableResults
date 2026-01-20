using FluentAssertions;
using Xunit;

namespace Light.Results.Tests;

public sealed class ErrorCategoryTests
{
    [Theory]
    [InlineData(ErrorCategory.Unclassified, 0)]
    [InlineData(ErrorCategory.Validation, 400)]
    [InlineData(ErrorCategory.Unauthorized, 401)]
    [InlineData(ErrorCategory.PaymentRequired, 402)]
    [InlineData(ErrorCategory.Forbidden, 403)]
    [InlineData(ErrorCategory.NotFound, 404)]
    [InlineData(ErrorCategory.MethodNotAllowed, 405)]
    [InlineData(ErrorCategory.NotAcceptable, 406)]
    [InlineData(ErrorCategory.Timeout, 408)]
    [InlineData(ErrorCategory.Conflict, 409)]
    [InlineData(ErrorCategory.Gone, 410)]
    [InlineData(ErrorCategory.LengthRequired, 411)]
    [InlineData(ErrorCategory.PreconditionFailed, 412)]
    [InlineData(ErrorCategory.ContentTooLarge, 413)]
    [InlineData(ErrorCategory.UriTooLong, 414)]
    [InlineData(ErrorCategory.UnsupportedMediaType, 415)]
    [InlineData(ErrorCategory.RequestedRangeNotSatisfiable, 416)]
    [InlineData(ErrorCategory.ExpectationFailed, 417)]
    [InlineData(ErrorCategory.MisdirectedRequest, 421)]
    [InlineData(ErrorCategory.UnprocessableContent, 422)]
    [InlineData(ErrorCategory.Locked, 423)]
    [InlineData(ErrorCategory.FailedDependency, 424)]
    [InlineData(ErrorCategory.UpgradeRequired, 426)]
    [InlineData(ErrorCategory.PreconditionRequired, 428)]
    [InlineData(ErrorCategory.TooManyRequests, 429)]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge, 431)]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, 451)]
    [InlineData(ErrorCategory.InternalError, 500)]
    [InlineData(ErrorCategory.NotImplemented, 501)]
    [InlineData(ErrorCategory.BadGateway, 502)]
    [InlineData(ErrorCategory.ServiceUnavailable, 503)]
    [InlineData(ErrorCategory.GatewayTimeout, 504)]
    [InlineData(ErrorCategory.InsufficientStorage, 507)]
    public void ErrorCategory_Values_ShouldMatchStatusCodes(ErrorCategory category, int expectedValue) =>
        ((int) category).Should().Be(expectedValue);

    [Fact]
    public void Error_DefaultCategory_ShouldBeUnclassified()
    {
        var error = new Error { Message = "Test message" };

        error.Category.Should().Be(ErrorCategory.Unclassified);
    }
}
