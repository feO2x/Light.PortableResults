using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationTargetNormalizationTests
{
    public static TheoryData<string, ValidationTargetSemantics, ValidationTargetCasing, string> RepresentativeTargets =>
        new ()
        {
            { "ZipCode", ValidationTargetSemantics.CallerExpression, ValidationTargetCasing.CamelCase, "zipCode" },
            { "dto", ValidationTargetSemantics.CallerExpression, ValidationTargetCasing.CamelCase, "dto" },
            {
                "dto.Address.ZipCode",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.CamelCase,
                "address.zipCode"
            },
            {
                "dto.Addresses[0].ZipCode",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.CamelCase,
                "addresses[0].zipCode"
            },
            {
                "  dto. Address . ZipCode  ",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.CamelCase,
                "address.zipCode"
            },
            {
                "dto.@Address.@ZipCode",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.CamelCase,
                "address.zipCode"
            },
            {
                "dto.address.zipCode",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.PascalCase,
                "Address.ZipCode"
            },
            {
                "dto.Address.ZipCode",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.Preserve,
                "Address.ZipCode"
            },
            {
                "dto.Address[0",
                ValidationTargetSemantics.CallerExpression,
                ValidationTargetCasing.CamelCase,
                "address[0"
            },
            { "dto.", ValidationTargetSemantics.CallerExpression, ValidationTargetCasing.CamelCase, string.Empty },
            { " \t ", ValidationTargetSemantics.CallerExpression, ValidationTargetCasing.CamelCase, string.Empty },
            {
                "Address.ZipCode",
                ValidationTargetSemantics.Relative,
                ValidationTargetCasing.CamelCase,
                "address.zipCode"
            },
            {
                "Addresses[0].ZipCode",
                ValidationTargetSemantics.Absolute,
                ValidationTargetCasing.CamelCase,
                "addresses[0].zipCode"
            },
            {
                "  Address . ZipCode  ",
                ValidationTargetSemantics.Relative,
                ValidationTargetCasing.CamelCase,
                "address.zipCode"
            }
        };

    [Theory]
    [MemberData(nameof(RepresentativeTargets))]
    public void DefaultNormalizer_ShouldNormalizeRepresentativeTargets(
        string rawPath,
        ValidationTargetSemantics semantics,
        ValidationTargetCasing casing,
        string expectedTarget
    )
    {
        var normalizer = new DefaultValidationTargetNormalizer(casing);

        var normalized = normalizer.Normalize(rawPath, semantics);

        normalized.Should().Be(expectedTarget);
    }

    [Fact]
    public void DefaultNormalizer_ShouldCacheRepeatedLookups()
    {
        var normalizer = new DefaultValidationTargetNormalizer();

        var first = normalizer.Normalize("dto.Address.ZipCode", ValidationTargetSemantics.CallerExpression);
        var second = normalizer.Normalize("dto.Address.ZipCode", ValidationTargetSemantics.CallerExpression);

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
