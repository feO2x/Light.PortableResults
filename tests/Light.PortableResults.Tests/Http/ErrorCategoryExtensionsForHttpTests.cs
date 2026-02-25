using System.Net;
using FluentAssertions;
using Light.PortableResults.Http;
using Xunit;

namespace Light.PortableResults.Tests.Http;

public sealed class ErrorCategoryExtensionsForHttpTests
{
    [Theory]
    [InlineData(ErrorCategory.Validation, "https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    [InlineData(ErrorCategory.Unauthorized, "https://tools.ietf.org/html/rfc9110#section-15.5.2")]
    [InlineData(ErrorCategory.PaymentRequired, "https://tools.ietf.org/html/rfc9110#section-15.5.3")]
    [InlineData(ErrorCategory.Forbidden, "https://tools.ietf.org/html/rfc9110#section-15.5.4")]
    [InlineData(ErrorCategory.NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5")]
    [InlineData(ErrorCategory.MethodNotAllowed, "https://tools.ietf.org/html/rfc9110#section-15.5.6")]
    [InlineData(ErrorCategory.NotAcceptable, "https://tools.ietf.org/html/rfc9110#section-15.5.7")]
    [InlineData(ErrorCategory.Timeout, "https://tools.ietf.org/html/rfc9110#section-15.5.9")]
    [InlineData(ErrorCategory.Conflict, "https://tools.ietf.org/html/rfc9110#section-15.5.10")]
    [InlineData(ErrorCategory.Gone, "https://tools.ietf.org/html/rfc9110#section-15.5.11")]
    [InlineData(ErrorCategory.LengthRequired, "https://tools.ietf.org/html/rfc9110#section-15.5.12")]
    [InlineData(ErrorCategory.PreconditionFailed, "https://tools.ietf.org/html/rfc9110#section-15.5.13")]
    [InlineData(ErrorCategory.ContentTooLarge, "https://tools.ietf.org/html/rfc9110#section-15.5.14")]
    [InlineData(ErrorCategory.UriTooLong, "https://tools.ietf.org/html/rfc9110#section-15.5.15")]
    [InlineData(ErrorCategory.UnsupportedMediaType, "https://tools.ietf.org/html/rfc9110#section-15.5.16")]
    [InlineData(ErrorCategory.RequestedRangeNotSatisfiable, "https://tools.ietf.org/html/rfc9110#section-15.5.17")]
    [InlineData(ErrorCategory.ExpectationFailed, "https://tools.ietf.org/html/rfc9110#section-15.5.18")]
    [InlineData(ErrorCategory.MisdirectedRequest, "https://tools.ietf.org/html/rfc9110#section-15.5.20")]
    [InlineData(ErrorCategory.UnprocessableContent, "https://tools.ietf.org/html/rfc9110#section-15.5.21")]
    [InlineData(ErrorCategory.Locked, "https://tools.ietf.org/html/rfc4918#section-11.3")]
    [InlineData(ErrorCategory.FailedDependency, "https://tools.ietf.org/html/rfc4918#section-11.4")]
    [InlineData(ErrorCategory.UpgradeRequired, "https://tools.ietf.org/html/rfc9110#section-15.5.22")]
    [InlineData(ErrorCategory.PreconditionRequired, "https://tools.ietf.org/html/rfc9110#section-15.5.23")]
    [InlineData(ErrorCategory.TooManyRequests, "https://tools.ietf.org/html/rfc6585#section-4")]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge, "https://tools.ietf.org/html/rfc6585#section-5")]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, "https://datatracker.ietf.org/doc/html/rfc7725#section-3")]
    [InlineData(ErrorCategory.InternalError, "https://tools.ietf.org/html/rfc9110#section-15.6.1")]
    [InlineData(ErrorCategory.NotImplemented, "https://tools.ietf.org/html/rfc9110#section-15.6.2")]
    [InlineData(ErrorCategory.BadGateway, "https://tools.ietf.org/html/rfc9110#section-15.6.3")]
    [InlineData(ErrorCategory.ServiceUnavailable, "https://tools.ietf.org/html/rfc9110#section-15.6.4")]
    [InlineData(ErrorCategory.GatewayTimeout, "https://tools.ietf.org/html/rfc9110#section-15.6.5")]
    [InlineData(ErrorCategory.InsufficientStorage, "https://tools.ietf.org/html/rfc9110#section-15.6.6")]
    public void GetTypeUriReturnsCorrectUri(ErrorCategory category, string expectedUri)
    {
        var result = category.GetTypeUri();

        result.Should().Be(expectedUri);
    }

    [Fact]
    public void GetTypeUri_UnknownStatusCode_ReturnsFallbackUri()
    {
        var result = ((ErrorCategory) 999).GetTypeUri();

        result.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
    }

    [Theory]
    [InlineData(ErrorCategory.Validation, "Bad Request")]
    [InlineData(ErrorCategory.Unauthorized, "Unauthorized")]
    [InlineData(ErrorCategory.PaymentRequired, "Payment Required")]
    [InlineData(ErrorCategory.Forbidden, "Forbidden")]
    [InlineData(ErrorCategory.NotFound, "Not Found")]
    [InlineData(ErrorCategory.MethodNotAllowed, "Method Not Allowed")]
    [InlineData(ErrorCategory.NotAcceptable, "Not Acceptable")]
    [InlineData(ErrorCategory.Timeout, "Request Timeout")]
    [InlineData(ErrorCategory.Conflict, "Conflict")]
    [InlineData(ErrorCategory.Gone, "Gone")]
    [InlineData(ErrorCategory.LengthRequired, "Length Required")]
    [InlineData(ErrorCategory.PreconditionFailed, "Precondition Failed")]
    [InlineData(ErrorCategory.ContentTooLarge, "Content Too Large")]
    [InlineData(ErrorCategory.UriTooLong, "URI Too Long")]
    [InlineData(ErrorCategory.UnsupportedMediaType, "Unsupported Media Type")]
    [InlineData(ErrorCategory.RequestedRangeNotSatisfiable, "Range Not Satisfiable")]
    [InlineData(ErrorCategory.ExpectationFailed, "Expectation Failed")]
    [InlineData(ErrorCategory.MisdirectedRequest, "Misdirected Request")]
    [InlineData(ErrorCategory.UnprocessableContent, "Unprocessable Entity")]
    [InlineData(ErrorCategory.Locked, "Locked")]
    [InlineData(ErrorCategory.FailedDependency, "Failed Dependency")]
    [InlineData(ErrorCategory.UpgradeRequired, "Upgrade Required")]
    [InlineData(ErrorCategory.PreconditionRequired, "Precondition Required")]
    [InlineData(ErrorCategory.TooManyRequests, "Too Many Requests")]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge, "Request Header Fields Too Large")]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, "Unavailable For Legal Reasons")]
    [InlineData(ErrorCategory.InternalError, "Internal Server Error")]
    [InlineData(ErrorCategory.NotImplemented, "Not Implemented")]
    [InlineData(ErrorCategory.BadGateway, "Bad Gateway")]
    [InlineData(ErrorCategory.ServiceUnavailable, "Service Unavailable")]
    [InlineData(ErrorCategory.GatewayTimeout, "Gateway Timeout")]
    [InlineData(ErrorCategory.InsufficientStorage, "Insufficient Storage")]
    public void GetTitle_ReturnsCorrectTitle(ErrorCategory category, string expectedTitle)
    {
        var result = category.GetTitle();

        result.Should().Be(expectedTitle);
    }

    [Fact]
    public void GetTitle_UnknownStatusCode_ReturnsFallbackTitle()
    {
        var result = ((ErrorCategory) 999).GetTitle();

        result.Should().Be("Internal Server Error");
    }

    [Theory]
    [InlineData(ErrorCategory.Validation, HttpStatusCode.BadRequest)]
    [InlineData(ErrorCategory.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(ErrorCategory.PaymentRequired, HttpStatusCode.PaymentRequired)]
    [InlineData(ErrorCategory.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(ErrorCategory.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorCategory.MethodNotAllowed, HttpStatusCode.MethodNotAllowed)]
    [InlineData(ErrorCategory.NotAcceptable, HttpStatusCode.NotAcceptable)]
    [InlineData(ErrorCategory.Timeout, HttpStatusCode.RequestTimeout)]
    [InlineData(ErrorCategory.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ErrorCategory.Gone, HttpStatusCode.Gone)]
    [InlineData(ErrorCategory.LengthRequired, HttpStatusCode.LengthRequired)]
    [InlineData(ErrorCategory.PreconditionFailed, HttpStatusCode.PreconditionFailed)]
    [InlineData(ErrorCategory.ContentTooLarge, (HttpStatusCode) 413)]
    [InlineData(ErrorCategory.UriTooLong, (HttpStatusCode) 414)]
    [InlineData(ErrorCategory.UnsupportedMediaType, HttpStatusCode.UnsupportedMediaType)]
    [InlineData(ErrorCategory.RequestedRangeNotSatisfiable, HttpStatusCode.RequestedRangeNotSatisfiable)]
    [InlineData(ErrorCategory.ExpectationFailed, HttpStatusCode.ExpectationFailed)]
    [InlineData(ErrorCategory.MisdirectedRequest, (HttpStatusCode) 421)]
    [InlineData(ErrorCategory.UnprocessableContent, (HttpStatusCode) 422)]
    [InlineData(ErrorCategory.Locked, (HttpStatusCode) 423)]
    [InlineData(ErrorCategory.FailedDependency, (HttpStatusCode) 424)]
    [InlineData(ErrorCategory.UpgradeRequired, HttpStatusCode.UpgradeRequired)]
    [InlineData(ErrorCategory.PreconditionRequired, (HttpStatusCode) 428)]
    [InlineData(ErrorCategory.TooManyRequests, (HttpStatusCode) 429)]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge, (HttpStatusCode) 431)]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, (HttpStatusCode) 451)]
    [InlineData(ErrorCategory.InternalError, HttpStatusCode.InternalServerError)]
    [InlineData(ErrorCategory.NotImplemented, HttpStatusCode.NotImplemented)]
    [InlineData(ErrorCategory.BadGateway, HttpStatusCode.BadGateway)]
    [InlineData(ErrorCategory.ServiceUnavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(ErrorCategory.GatewayTimeout, HttpStatusCode.GatewayTimeout)]
    [InlineData(ErrorCategory.InsufficientStorage, (HttpStatusCode) 507)]
    public static void ToHttpStatusCode_MapsToCorrectHttpStatusCode(
        ErrorCategory category,
        HttpStatusCode expectedStatusCode
    )
    {
        var result = category.ToHttpStatusCode();

        result.Should().Be(expectedStatusCode);
    }

    [Fact]
    public static void ToHttpStatusCode_Unclassified_MapsTo500()
    {
        var result = ErrorCategory.Unclassified.ToHttpStatusCode();

        result.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData(ErrorCategory.Validation, "One or more validation errors occurred.")]
    [InlineData(ErrorCategory.Unauthorized, "Authentication is required to access this resource.")]
    [InlineData(ErrorCategory.PaymentRequired, "Payment is required before the request can be processed.")]
    [InlineData(ErrorCategory.Forbidden, "You do not have permission to access this resource.")]
    [InlineData(ErrorCategory.NotFound, "The requested resource was not found.")]
    [InlineData(ErrorCategory.MethodNotAllowed, "The HTTP method used is not allowed for this resource.")]
    [InlineData(
        ErrorCategory.NotAcceptable,
        "No acceptable representation of the resource could be found for the request."
    )]
    [InlineData(ErrorCategory.Timeout, "The request timed out.")]
    [InlineData(ErrorCategory.Conflict, "The request conflicts with the current state of the resource.")]
    [InlineData(ErrorCategory.Gone, "The requested resource is no longer available.")]
    [InlineData(ErrorCategory.LengthRequired, "The request must include a Content-Length header.")]
    [InlineData(ErrorCategory.PreconditionFailed, "A precondition for the request was not met.")]
    [InlineData(ErrorCategory.ContentTooLarge, "The request content is too large.")]
    [InlineData(ErrorCategory.UriTooLong, "The request URI is too long.")]
    [InlineData(ErrorCategory.UnsupportedMediaType, "The request content type is not supported.")]
    [InlineData(
        ErrorCategory.RequestedRangeNotSatisfiable,
        "The requested range cannot be satisfied for this resource."
    )]
    [InlineData(
        ErrorCategory.ExpectationFailed,
        "The server could not meet the expectation given in the request headers."
    )]
    [InlineData(
        ErrorCategory.MisdirectedRequest,
        "The request was directed to a server that cannot produce a response."
    )]
    [InlineData(ErrorCategory.UnprocessableContent, "The request was well-formed but could not be processed.")]
    [InlineData(ErrorCategory.Locked, "The resource is locked and cannot be modified.")]
    [InlineData(ErrorCategory.FailedDependency, "The request failed because a dependent request did not succeed.")]
    [InlineData(
        ErrorCategory.UpgradeRequired,
        "The client must upgrade to a different protocol to complete the request."
    )]
    [InlineData(ErrorCategory.PreconditionRequired, "A required precondition header is missing from the request.")]
    [InlineData(ErrorCategory.TooManyRequests, "Too many requests. Please try again later.")]
    [InlineData(ErrorCategory.RequestHeaderFieldsTooLarge, "The request headers are too large.")]
    [InlineData(ErrorCategory.UnavailableForLegalReasons, "This resource is unavailable for legal reasons.")]
    [InlineData(ErrorCategory.InternalError, "An unexpected error occurred.")]
    [InlineData(ErrorCategory.NotImplemented, "This functionality is not implemented.")]
    [InlineData(ErrorCategory.BadGateway, "An invalid response was received from an upstream server.")]
    [InlineData(ErrorCategory.ServiceUnavailable, "The service is currently unavailable.")]
    [InlineData(ErrorCategory.GatewayTimeout, "The upstream server did not respond in time.")]
    [InlineData(
        ErrorCategory.InsufficientStorage,
        "The server cannot store the representation needed to complete the request."
    )]
    public void GetDetail_ReturnsCorrectDetail(ErrorCategory category, string expectedDetail)
    {
        var result = category.GetDetail();

        result.Should().Be(expectedDetail);
    }

    [Fact]
    public void GetDetail_UnknownStatusCode_ReturnsFallbackDetail()
    {
        var result = ((ErrorCategory) 999).GetDetail();

        result.Should().Be("An unexpected error occurred.");
    }
}
