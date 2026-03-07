using System;
using FluentAssertions;
using Light.PortableResults.Validation;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationTargetNormalizationTests
{
    [Fact]
    public void DefaultNormalizer_ShouldPreserveMemberPathAndCamelCaseSegments()
    {
        var normalizer = new DefaultValidationTargetNormalizer();

        var normalized = normalizer.Normalize("dto.Address.ZipCode");

        normalized.Should().Be("address.zipCode");
    }

    [Fact]
    public void DefaultNormalizer_ShouldPreserveIndexes()
    {
        var normalizer = new DefaultValidationTargetNormalizer();

        var normalized = normalizer.Normalize("dto.Addresses[0].ZipCode");

        normalized.Should().Be("addresses[0].zipCode");
    }

    [Fact]
    public void DefaultNormalizer_ShouldSupportPreserveCasing()
    {
        var normalizer = new DefaultValidationTargetNormalizer(ValidationTargetCasing.Preserve);

        var normalized = normalizer.Normalize("dto.Address.ZipCode");

        normalized.Should().Be("Address.ZipCode");
    }

    [Fact]
    public void DefaultNormalizer_ShouldCacheRepeatedLookups()
    {
        var normalizer = new DefaultValidationTargetNormalizer();

        var first = normalizer.Normalize("dto.Address.ZipCode");
        var second = normalizer.Normalize("dto.Address.ZipCode");

        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void ValidationTargets_ShouldComposeNestedTargets()
    {
        var addressTarget = ValidationTargets.Compose("address", "zipCode");
        var itemTarget = ValidationTargets.Compose("addresses", "[0]");
        var nestedItemTarget = ValidationTargets.Compose(itemTarget, "zipCode");

        addressTarget.Should().Be("address.zipCode");
        nestedItemTarget.Should().Be("addresses[0].zipCode");
    }
}
