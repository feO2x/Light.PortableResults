using System;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidatedValueTests
{
    [Fact]
    public void Success_ShouldExposeValidatedValue()
    {
        var validatedValue = ValidatedValue.Success("Alice");

        validatedValue.HasValue.Should().BeTrue();
        validatedValue.Value.Should().Be("Alice");
        validatedValue.TryGetValue(out var value).Should().BeTrue();
        value.Should().Be("Alice");
    }

    [Fact]
    public void NoValue_ShouldNotExposeValidatedValue()
    {
        var validatedValue = ValidatedValue<int>.NoValue;

        validatedValue.HasValue.Should().BeFalse();
        validatedValue.TryGetValue(out var value).Should().BeFalse();
        value.Should().Be(0);
        var act = () => _ = validatedValue.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Success_ShouldRejectNullValues()
    {
        var act = () => ValidatedValue.Success<string?>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equality_ShouldComparePresenceAndValue()
    {
        var left = ValidatedValue.Success(42);
        var right = ValidatedValue<int>.Success(42);

        left.Should().Be(right);
        ValidatedValue<int>.NoValue.Should().Be(ValidatedValue<int>.NoValue);
        left.Should().NotBe(ValidatedValue<int>.NoValue);
    }
}
