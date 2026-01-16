using FluentAssertions;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests;

public sealed class HttpStatusCodeInfoTests
{
    [Theory]
    [InlineData(400, "https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    [InlineData(401, "https://tools.ietf.org/html/rfc9110#section-15.5.2")]
    [InlineData(403, "https://tools.ietf.org/html/rfc9110#section-15.5.4")]
    [InlineData(404, "https://tools.ietf.org/html/rfc9110#section-15.5.5")]
    [InlineData(408, "https://tools.ietf.org/html/rfc9110#section-15.5.9")]
    [InlineData(409, "https://tools.ietf.org/html/rfc9110#section-15.5.10")]
    [InlineData(410, "https://tools.ietf.org/html/rfc9110#section-15.5.11")]
    [InlineData(412, "https://tools.ietf.org/html/rfc9110#section-15.5.13")]
    [InlineData(413, "https://tools.ietf.org/html/rfc9110#section-15.5.14")]
    [InlineData(414, "https://tools.ietf.org/html/rfc9110#section-15.5.15")]
    [InlineData(415, "https://tools.ietf.org/html/rfc9110#section-15.5.16")]
    [InlineData(422, "https://tools.ietf.org/html/rfc9110#section-15.5.21")]
    [InlineData(429, "https://tools.ietf.org/html/rfc6585#section-4")]
    [InlineData(451, "https://datatracker.ietf.org/doc/html/rfc7725#section-3")]
    [InlineData(500, "https://tools.ietf.org/html/rfc9110#section-15.6.1")]
    [InlineData(501, "https://tools.ietf.org/html/rfc9110#section-15.6.2")]
    [InlineData(502, "https://tools.ietf.org/html/rfc9110#section-15.6.3")]
    [InlineData(503, "https://tools.ietf.org/html/rfc9110#section-15.6.4")]
    [InlineData(504, "https://tools.ietf.org/html/rfc9110#section-15.6.5")]
    public void GetTypeUriReturnsCorrectUri(int statusCode, string expectedUri)
    {
        var result = HttpStatusCodeInfo.GetTypeUri(statusCode);

        result.Should().Be(expectedUri);
    }

    [Fact]
    public void GetTypeUri_UnknownStatusCode_ReturnsFallbackUri()
    {
        var result = HttpStatusCodeInfo.GetTypeUri(999);

        result.Should().Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
    }

    [Theory]
    [InlineData(400, "Bad Request")]
    [InlineData(401, "Unauthorized")]
    [InlineData(403, "Forbidden")]
    [InlineData(404, "Not Found")]
    [InlineData(408, "Request Timeout")]
    [InlineData(409, "Conflict")]
    [InlineData(410, "Gone")]
    [InlineData(412, "Precondition Failed")]
    [InlineData(413, "Content Too Large")]
    [InlineData(414, "URI Too Long")]
    [InlineData(415, "Unsupported Media Type")]
    [InlineData(422, "Unprocessable Entity")]
    [InlineData(429, "Too Many Requests")]
    [InlineData(451, "Unavailable For Legal Reasons")]
    [InlineData(500, "Internal Server Error")]
    [InlineData(501, "Not Implemented")]
    [InlineData(502, "Bad Gateway")]
    [InlineData(503, "Service Unavailable")]
    [InlineData(504, "Gateway Timeout")]
    public void GetTitle_ReturnsCorrectTitle(int statusCode, string expectedTitle)
    {
        var result = HttpStatusCodeInfo.GetTitle(statusCode);

        result.Should().Be(expectedTitle);
    }

    [Fact]
    public void GetTitle_UnknownStatusCode_ReturnsFallbackTitle()
    {
        var result = HttpStatusCodeInfo.GetTitle(999);

        result.Should().Be("Internal Server Error");
    }
}
