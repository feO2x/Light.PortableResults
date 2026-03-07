using FluentAssertions;
using Light.PortableResults.Validation;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationContextTests
{
    [Fact]
    public void Check_ShouldNormalizeTrimmedStringValues()
    {
        var context = new ValidationContextFactory().CreateValidationContext();
        string? firstName = "  Alice  ";

        var check = context.Check(firstName).NormalizeTargetIfNecessary();

        check.Value.Should().Be("Alice");
        check.Target.Should().Be("firstName");
        check.DisplayName.Should().Be("firstName");
    }

    [Fact]
    public void Check_ShouldNormalizeNullStringsToEmptyString()
    {
        var context = new ValidationContextFactory().CreateValidationContext();
        string? firstName = null;

        var check = context.Check(firstName);

        check.Value.Should().BeEmpty();
    }

    [Fact]
    public void AddError_ShouldMaterializeFlatErrorsAndFailureResult()
    {
        var context = new ValidationContextFactory().CreateValidationContext();

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
}
