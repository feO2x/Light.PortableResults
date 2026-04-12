using System;
using FluentAssertions;
using Light.PortableResults.Validation.Normalization;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationInfrastructureWorkflowTests
{
    public static TheoryData<string, ValidationTargetSemantics, string> RepresentativeTargets =>
        new ()
        {
            { "dto.Address.ZipCode", ValidationTargetSemantics.CallerExpression, "address.zipCode" },
            { "dto.Addresses[0].ZipCode", ValidationTargetSemantics.CallerExpression, "addresses[0].zipCode" },
            { "Address.ZipCode", ValidationTargetSemantics.Relative, "address.zipCode" },
            { "Addresses[0].ZipCode", ValidationTargetSemantics.Absolute, "addresses[0].zipCode" }
        };

    [Theory]
    [MemberData(nameof(RepresentativeTargets))]
    public void DefaultNormalizer_ShouldNormalizeRepresentativeTargets(
        string input,
        ValidationTargetSemantics semantics,
        string expected
    )
    {
        var normalizer = new DefaultValidationTargetNormalizer();

        var normalized = normalizer.Normalize(input, semantics);

        normalized.Should().Be(expected);
    }

    [Fact]
    public void ValidationContext_ShouldNormalizeTargetsAcrossScopes()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        var indexedContext = context.ForMember("addresses", isNormalized: true).ForIndex(1);

        var directCheck = context.Check("Alice", target: "request.FirstName").NormalizeTargetIfNecessary();
        var relativeCheck = addressContext
           .Check("12345", ValidationTarget.Relative("zipCode", isNormalized: true))
           .NormalizeTargetIfNecessary();
        var absoluteCheck = indexedContext
           .Check("12345", ValidationTarget.Absolute("orders[0].zipCode", isNormalized: true))
           .NormalizeTargetIfNecessary();

        directCheck.Target.Should().Be("firstName");
        relativeCheck.Target.Should().Be("address.zipCode");
        absoluteCheck.Target.Should().Be("orders[0].zipCode");
    }

    [Fact]
    public void ValidationContext_ShouldNormalizeStringsAndAllowNoOpOverrides()
    {
        var defaultContext = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var noOpContext = new DefaultValidationContextFactory(
            new ValidationContextOptions() with { ValueNormalizer = NoOpValueNormalizer.Instance }
        ).CreateValidationContext();
        string? nullableName = null;

        var normalizedCheck = defaultContext.Check("  Alice  ").NormalizeTargetIfNecessary();
        var normalizedNull = defaultContext.Check(nullableName);
        var preservedCheck = noOpContext.Check("  Alice  ", NoOpValueNormalizer.Instance);
        var preservedNull = noOpContext.Check(nullableName);

        normalizedCheck.Value.Should().Be("Alice");
        normalizedNull.Value.Should().BeEmpty();
        preservedCheck.Value.Should().Be("  Alice  ");
        preservedNull.Value.Should().BeNull();
    }

    [Fact]
    public void ValidationContext_ShouldShareTypedItemsAcrossScopes()
    {
        var tenantKey = new ValidationContextKey<string>("tenant");
        var stringKeyCopy = new ValidationContextKey<string>("tenant");
        var intKey = new ValidationContextKey<int>("tenant");
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);

        context.SetItem(tenantKey, "checkout");
        context.SetItem(intKey, 42);
        childContext.GetRequiredItem(stringKeyCopy).Should().Be("checkout");
        childContext.AsReadOnly().GetRequiredItem(stringKeyCopy).Should().Be("checkout");
        context.GetRequiredItem(intKey).Should().Be(42);
        stringKeyCopy.Should().Be(tenantKey);
        intKey.Should().NotBe(tenantKey);
    }

    [Fact]
    public void ValidationContext_ShouldMaterializeFlatFailureResults()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.AddError(
            "First name is required",
            "NotEmpty",
            ValidationTarget.Relative("firstName", isNormalized: true)
        );
        context.ForMember("address", isNormalized: true)
           .AddError("Zip code is invalid", "InvalidZipCode", ValidationTarget.Relative("zipCode", isNormalized: true));

        context.ToFailureResult().Errors.Should().Equal(
            new Errors(
                new[]
                {
                    ValidationWorkflowTestData.CreateValidationError(
                        "First name is required",
                        "NotEmpty",
                        "firstName"
                    ),
                    ValidationWorkflowTestData.CreateValidationError(
                        "Zip code is invalid",
                        "InvalidZipCode",
                        "address.zipCode"
                    )
                }
            )
        );
    }

    [Fact]
    public void ValidationContext_ShouldShareItemsAcrossMutableAndReadOnlyViews()
    {
        var tenantKey = new ValidationContextKey<string>("tenant");
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var readOnlyContext = context.AsReadOnly();

        context.SetItem(tenantKey, "checkout");
        context.RemoveItem(tenantKey).Should().BeTrue();

        readOnlyContext.TryGetItem(tenantKey, out _).Should().BeFalse();
        context.TryGetItem(tenantKey, out _).Should().BeFalse();
    }

    [Fact]
    public void ValidationContextAndCheck_ShouldSupportConvenienceScopingAndValueApis()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var request = new RegistrationRequest
        {
            FirstName = "Alice",
            Email = "alice@example.com",
            Address = new AddressDto { ZipCode = "12345" }
        };

        var requestScope = context.For(request);
        var addressScope = requestScope.ForCallerExpression("request.Address");
        var zipCodeCheck = addressScope.Check(request.Address.ZipCode, displayName: "Zip code")
           .WithDisplayName("Postal code");
        var childScope = zipCodeCheck.CreateChildContextForMember("metadata", isNormalized: true);
        string zipCode = zipCodeCheck;

        childScope.AddError("Metadata is invalid", "MetadataInvalid");

        requestScope.TargetPrefix.Should().Be("request");
        addressScope.TargetPrefix.Should().Be("request.address");
        zipCodeCheck.DisplayName.Should().Be("Postal code");
        zipCode.Should().Be("12345");
        context.Errors.Should().ContainSingle(error => error.Code == "MetadataInvalid");
        context.Errors[0].Target.Should().Contain("metadata");
    }

    [Fact]
    public void ValidationContext_ToFailureResult_ShouldThrow_WhenNoErrorsArePresent()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        Action act = () => context.ToFailureResult();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*no validation errors are present*");
    }
}
