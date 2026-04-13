using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CheckAddErrorTests
{
    [Fact]
    public void AddErrorError_ShouldPreserveExplicitTarget()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var check = context.Check("ABC", target: "code");
        var error = new Error
        {
            Message = "Code already exists",
            Code = "Conflict",
            Target = "orders[0].code",
            Category = ErrorCategory.Conflict
        };

        check.AddError(error);

        context.Errors.Should().Equal(new Errors(error));
    }

    [Fact]
    public void AddErrorError_ShouldFillCurrentCheckTarget_WhenErrorHasNoTarget()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var check = context.Check("ABC", target: "code");
        var error = new Error
        {
            Message = "Code already exists",
            Code = "Conflict",
            Category = ErrorCategory.Conflict
        };

        check.AddError(error);

        context.Errors.Should().Equal(
            new Errors(
                error with
                {
                    Target = "code"
                }
            )
        );
    }

    [Fact]
    public void AddErrorString_ShouldSupportCategoryTargetOverrideAndShortCircuitControl()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var check = context.Check("ABC", target: "code").ShortCircuit();

        var skippedCheck = check.AddError(
            "Code already exists",
            code: "Conflict",
            category: ErrorCategory.Conflict
        );
        var forcedCheck = check.AddError(
            "Code already exists",
            code: "Conflict",
            target: ValidationTarget.Absolute("orders[0].code", isNormalized: true),
            category: ErrorCategory.Conflict,
            respectShortCircuit: false
        );

        skippedCheck.IsShortCircuited.Should().BeTrue();
        forcedCheck.IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Code already exists",
                    Code = "Conflict",
                    Target = "orders[0].code",
                    Category = ErrorCategory.Conflict
                }
            )
        );
    }

    [Fact]
    public void AddErrorDefinition_ShouldSupportTemplateBackedDefinitionsAsMigrationPath()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var definition = new TemplateValidationErrorDefinition(
            new ValidationErrorTemplates.DisplayName(" is invalid"),
            code: "InvalidCode",
            category: ErrorCategory.UnprocessableContent
        );

        context.Check("ABC", target: "code", displayName: "Code").AddError(definition);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Code is invalid",
                    Code = "InvalidCode",
                    Target = "code",
                    Category = ErrorCategory.UnprocessableContent
                }
            )
        );
    }

    [Fact]
    public void AddErrorMessage_ShouldSupportMetadataAndTemplateCodes()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var metadata = MetadataObject.Create(("source", "manual"));
        var message = new ValidationErrorMessage("Code is invalid", "validation.code.invalid");

        context.AddError(message, target: ValidationTarget.Relative("code", isNormalized: true), metadata: metadata);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Code is invalid",
                    Code = "validation.code.invalid",
                    Target = "code",
                    Category = ErrorCategory.Validation,
                    Metadata = metadata
                }
            )
        );
    }
}
