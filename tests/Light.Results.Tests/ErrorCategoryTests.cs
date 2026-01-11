using FluentAssertions;

namespace Light.Results.Tests;

public sealed class ErrorCategoryTests
{
    [Fact]
    public void ErrorCategory_Unclassified_ShouldBe0() =>
        ((int) ErrorCategory.Unclassified).Should().Be(0);

    [Fact]
    public void ErrorCategory_Validation_ShouldBe400() =>
        ((int) ErrorCategory.Validation).Should().Be(400);

    [Fact]
    public void ErrorCategory_Unauthorized_ShouldBe401() =>
        ((int) ErrorCategory.Unauthorized).Should().Be(401);

    [Fact]
    public void ErrorCategory_Forbidden_ShouldBe403() =>
        ((int) ErrorCategory.Forbidden).Should().Be(403);

    [Fact]
    public void ErrorCategory_NotFound_ShouldBe404() =>
        ((int) ErrorCategory.NotFound).Should().Be(404);

    [Fact]
    public void ErrorCategory_Timeout_ShouldBe408() =>
        ((int) ErrorCategory.Timeout).Should().Be(408);

    [Fact]
    public void ErrorCategory_Conflict_ShouldBe409() =>
        ((int) ErrorCategory.Conflict).Should().Be(409);

    [Fact]
    public void ErrorCategory_Gone_ShouldBe410() =>
        ((int) ErrorCategory.Gone).Should().Be(410);

    [Fact]
    public void ErrorCategory_PreconditionFailed_ShouldBe412() =>
        ((int) ErrorCategory.PreconditionFailed).Should().Be(412);

    [Fact]
    public void ErrorCategory_ContentTooLarge_ShouldBe413() =>
        ((int) ErrorCategory.ContentTooLarge).Should().Be(413);

    [Fact]
    public void ErrorCategory_UriTooLong_ShouldBe414() =>
        ((int) ErrorCategory.UriTooLong).Should().Be(414);

    [Fact]
    public void ErrorCategory_UnsupportedMediaType_ShouldBe415() =>
        ((int) ErrorCategory.UnsupportedMediaType).Should().Be(415);

    [Fact]
    public void ErrorCategory_UnprocessableEntity_ShouldBe422() =>
        ((int) ErrorCategory.UnprocessableEntity).Should().Be(422);

    [Fact]
    public void ErrorCategory_RateLimited_ShouldBe429() =>
        ((int) ErrorCategory.RateLimited).Should().Be(429);

    [Fact]
    public void ErrorCategory_UnavailableForLegaReasons_ShouldBe451() =>
        ((int) ErrorCategory.UnavailableForLegalReasons).Should().Be(451);

    [Fact]
    public void ErrorCategory_InternalError_ShouldBe500() =>
        ((int) ErrorCategory.InternalError).Should().Be(500);

    [Fact]
    public void ErrorCategory_NotImplemented_ShouldBe501() =>
        ((int) ErrorCategory.NotImplemented).Should().Be(501);

    [Fact]
    public void ErrorCategory_BadGateway_ShouldBe502() =>
        ((int) ErrorCategory.BadGateway).Should().Be(502);

    [Fact]
    public void ErrorCategory_ServiceUnavailable_ShouldBe503() =>
        ((int) ErrorCategory.ServiceUnavailable).Should().Be(503);

    [Fact]
    public void ErrorCategory_GatewayTimeout_ShouldBe504() =>
        ((int) ErrorCategory.GatewayTimeout).Should().Be(504);

    [Fact]
    public void Error_DefaultCategory_ShouldBeUnclassified()
    {
        var error = new Error { Message = "Test message" };

        error.Category.Should().Be(ErrorCategory.Unclassified);
    }
}
