using System;
using FluentAssertions;
using Light.Results.Http.Reading;
using Xunit;

namespace Light.Results.Tests.Http.Reading;

public sealed class HttpHeaderSelectionStrategiesTests
{
    [Fact]
    public void None_ShouldExcludeAnyHeader()
    {
        HttpHeaderSelectionStrategies.None.ShouldInclude("X-Test").Should().BeFalse();
    }

    [Fact]
    public void All_ShouldIncludeAnyHeader()
    {
        HttpHeaderSelectionStrategies.All.ShouldInclude("X-Test").Should().BeTrue();
    }

    [Fact]
    public void AllowList_ShouldIncludeConfiguredHeaders_CaseInsensitiveByDefault()
    {
        var strategy = HttpHeaderSelectionStrategies.AllowList(["X-Trace"]);

        strategy.ShouldInclude("X-Trace").Should().BeTrue();
        strategy.ShouldInclude("x-trace").Should().BeTrue();
        strategy.ShouldInclude("X-Other").Should().BeFalse();
    }

    [Fact]
    public void AllowList_ShouldHonorConfiguredComparer()
    {
        var strategy = HttpHeaderSelectionStrategies.AllowList(["X-Trace"], StringComparer.Ordinal);

        strategy.ShouldInclude("X-Trace").Should().BeTrue();
        strategy.ShouldInclude("x-trace").Should().BeFalse();
    }

    [Fact]
    public void DenyList_ShouldExcludeConfiguredHeaders_CaseInsensitiveByDefault()
    {
        var strategy = HttpHeaderSelectionStrategies.DenyList(["X-Trace"]);

        strategy.ShouldInclude("X-Trace").Should().BeFalse();
        strategy.ShouldInclude("x-trace").Should().BeFalse();
        strategy.ShouldInclude("X-Other").Should().BeTrue();
    }

    [Fact]
    public void DenyList_ShouldHonorConfiguredComparer()
    {
        var strategy = HttpHeaderSelectionStrategies.DenyList(["X-Trace"], StringComparer.Ordinal);

        strategy.ShouldInclude("X-Trace").Should().BeFalse();
        strategy.ShouldInclude("x-trace").Should().BeTrue();
    }

    [Fact]
    public void AllowList_ShouldThrow_WhenHeaderNamesAreNull()
    {
        Action act = () => HttpHeaderSelectionStrategies.AllowList(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DenyList_ShouldThrow_WhenHeaderNamesAreNull()
    {
        Action act = () => HttpHeaderSelectionStrategies.DenyList(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
