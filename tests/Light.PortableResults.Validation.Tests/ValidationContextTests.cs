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
        check.DisplayName.Should().Be("firstName");
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
            StringValueNormalizer = NoOpStringValueNormalizer.Instance
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

        var check = context.Check(firstName, NoOpStringValueNormalizer.Instance);

        check.Value.Should().Be("  Alice  ");
    }

    [Fact]
    public void AddError_ShouldMaterializeFlatErrorsAndFailureResult()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.AddError("first name is required", "NotEmpty", "firstName");
        context.AddError("age must be at least 18", "Adult", "age");

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

        context.ToErrors().Should().Equal(
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

        childContext.AddError("zip code is invalid", "InvalidZipCode", "zipCode");

        context.ToErrors().Should().Equal(
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
