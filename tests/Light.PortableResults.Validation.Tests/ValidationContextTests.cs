using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationContextTests
{
    [Fact]
    public void Check_ShouldNormalizeTrimmedStringValues()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        const string firstName = "  Alice  ";

        var check = context.Check(firstName).NormalizeTargetIfNecessary();

        check.Value.Should().Be("Alice");
        check.Target.Should().Be("firstName");
        check.DisplayName.Should().BeNull();
    }

    [Fact]
    public void Check_DisplayName_ShouldBeNull_WhenNotExplicitlySet()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check("value", target: "myTarget");

        check.DisplayName.Should().BeNull();
    }

    [Fact]
    public void Check_DisplayName_ShouldRetainExplicitValue()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check("value", target: "myTarget", displayName: "My Display Name");

        check.DisplayName.Should().Be("My Display Name");
    }

    [Fact]
    public void Check_DisplayName_ShouldNotChangeAfterNormalization()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check("Alice", target: "person.FirstName")
           .NormalizeTargetIfNecessary();

        check.DisplayName.Should().BeNull();
        check.Target.Should().Be("firstName");
    }

    [Fact]
    public void Check_WithDisplayName_ShouldNotBeAffectedByNormalization()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        var check = context.Check("Alice", target: "person.FirstName", displayName: "Custom Name")
           .NormalizeTargetIfNecessary();

        check.DisplayName.Should().Be("Custom Name");
        check.Target.Should().Be("firstName");
    }

    [Fact]
    public void Check_ShouldNormalizeNullStringsToEmptyString()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        string? firstName = null;

        var check = context.Check(firstName);

        check.Value.Should().BeEmpty();
    }

    [Fact]
    public void Check_ShouldPreserveNullStrings_WhenNoOpNormalizerIsConfigured()
    {
        var options = ValidationContextOptions.Default with
        {
            ValueNormalizer = NoOpValueNormalizer.Instance
        };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();
        string? firstName = null;

        var check = context.Check(firstName);

        check.Value.Should().BeNull();
    }

    [Fact]
    public void Check_ShouldAllowPerCheckStringNormalizerOverride()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        const string firstName = "  Alice  ";

        var check = context.Check(firstName, NoOpValueNormalizer.Instance);

        check.Value.Should().Be("  Alice  ");
    }

    [Fact]
    public void Check_WithExplicitStringTarget_ShouldStillUseCallerExpressionSemantics()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("customer", isNormalized: true);

        var check = childContext.Check("Alice", target: "request.FirstName").NormalizeTargetIfNecessary();

        check.Target.Should().Be("customer.firstName");
    }

    [Fact]
    public void Check_ShouldSupportExplicitRelativeAndAbsoluteTargets()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("customer", isNormalized: true);

        var relativeCheck = childContext
           .Check("Alice", ValidationTarget.Relative("firstName", isNormalized: true))
           .NormalizeTargetIfNecessary();
        var absoluteCheck = childContext
           .Check("Alice", ValidationTarget.Absolute("account.ownerName", isNormalized: true))
           .NormalizeTargetIfNecessary();

        relativeCheck.Target.Should().Be("customer.firstName");
        absoluteCheck.Target.Should().Be("account.ownerName");
    }

    [Fact]
    public void AddError_ShouldMaterializeFlatErrorsAndFailureResult()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.AddError("first name is required", "NotEmpty", ValidationTarget.Relative("firstName", true));
        context.AddError("age must be at least 18", "Adult", ValidationTarget.Relative("age", true));

        context.TryGetErrors(out var errors).Should().BeTrue();
        errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "first name is required",
                        Code = "NotEmpty",
                        Target = "firstName",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "age must be at least 18",
                        Code = "Adult",
                        Target = "age",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );

        var result = context.ToFailureResult();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(errors);
    }

    [Fact]
    public void AddError_WithConstructedError_ShouldNotInferScopedTarget()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);

        childContext.AddError(
            new Error
            {
                Message = "zip code is invalid",
                Code = "InvalidZipCode",
                Category = ErrorCategory.Validation
            }
        );

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zip code is invalid",
                    Code = "InvalidZipCode",
                    Target = null,
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void AddError_WithMessageOverload_ShouldStillComposeScopedTarget()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);

        childContext.AddError(
            "zip code is invalid",
            "InvalidZipCode",
            ValidationTarget.Relative("zipCode", true)
        );

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zip code is invalid",
                    Code = "InvalidZipCode",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ContextItems_ShouldBeSharedAcrossParentAndChildScopes()
    {
        var key = new ValidationContextKey<string>("tenant");
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);

        context.SetItem(key, "parent");
        childContext.TryGetItem(key, out var initialValue).Should().BeTrue();
        initialValue.Should().Be("parent");

        childContext.SetItem(key, "child");
        context.TryGetItem(key, out var updatedValue).Should().BeTrue();
        updatedValue.Should().Be("child");

        childContext.RemoveItem(key).Should().BeTrue();
        context.TryGetItem(key, out _).Should().BeFalse();
    }
}
