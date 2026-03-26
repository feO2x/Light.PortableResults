using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public abstract class BaseCheckTest
{
    private static readonly DefaultValidationContextFactory ValidationContextFactory = new ();
    protected ValidationContext ValidationContext { get; } = ValidationContextFactory.CreateValidationContext();
}

public sealed class IsNotNullTests : BaseCheckTest
{
    [Fact]
    public void IsNotNull_ShouldShortCircuitByDefault()
    {
        List<int>? collection = null;

        var check = ValidationContext.Check(collection).IsNotNull();

        check.IsShortCircuited.Should().BeTrue();
    }
}
