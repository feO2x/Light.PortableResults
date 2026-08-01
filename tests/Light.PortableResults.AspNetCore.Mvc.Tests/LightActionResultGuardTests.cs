using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.AspNetCore.Mvc.Tests;

public sealed class LightActionResultGuardTests
{
    [Fact]
    public async Task ExecuteResultAsync_ShouldRejectNullContext()
    {
        var actionResult = new LightActionResult(Result.Ok());

        var act = async () => await actionResult.ExecuteResultAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
