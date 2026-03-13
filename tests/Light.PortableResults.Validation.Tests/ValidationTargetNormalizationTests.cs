using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationTargetNormalizationTests
{
    public static TheoryData<string, ValidationTargetCasing, string> RepresentativeTargets =>
        new ()
        {
            { "ZipCode", ValidationTargetCasing.CamelCase, "zipCode" },
            { "dto", ValidationTargetCasing.CamelCase, "dto" },
            { "dto.Address.ZipCode", ValidationTargetCasing.CamelCase, "address.zipCode" },
            { "dto.Addresses[0].ZipCode", ValidationTargetCasing.CamelCase, "addresses[0].zipCode" },
            { "  dto. Address . ZipCode  ", ValidationTargetCasing.CamelCase, "address.zipCode" },
            { "dto.@Address.@ZipCode", ValidationTargetCasing.CamelCase, "address.zipCode" },
            { "dto.address.zipCode", ValidationTargetCasing.PascalCase, "Address.ZipCode" },
            { "dto.Address.ZipCode", ValidationTargetCasing.Preserve, "Address.ZipCode" },
            { "dto.Address[0", ValidationTargetCasing.CamelCase, "address[0" },
            { "dto.", ValidationTargetCasing.CamelCase, string.Empty },
            { " \t ", ValidationTargetCasing.CamelCase, string.Empty }
        };

    [Theory]
    [MemberData(nameof(RepresentativeTargets))]
    public void DefaultNormalizer_ShouldNormalizeRepresentativeTargets(
        string rawPath,
        ValidationTargetCasing casing,
        string expectedTarget
    )
    {
        var normalizer = new DefaultValidationTargetNormalizer(casing);

        var normalized = normalizer.Normalize(rawPath);

        normalized.Should().Be(expectedTarget);
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
