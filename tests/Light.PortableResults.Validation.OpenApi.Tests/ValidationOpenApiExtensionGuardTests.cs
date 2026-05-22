using System;
using FluentAssertions;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

public sealed class ValidationOpenApiExtensionGuardTests
{
    [Fact]
    public void RegisterBuiltInValidationErrors_ShouldRejectNullBuilder()
    {
        var act = static () => ((ErrorMetadataContractsBuilder) null!).RegisterBuiltInValidationErrors();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProducesPortableValidationProblemFor_ShouldRejectNullBuilder()
    {
        var act = static () =>
            ((RouteHandlerBuilder) null!).ProducesPortableValidationProblemFor<GeneratedRatingValidator>();

        act.Should().Throw<ArgumentNullException>();
    }
}
