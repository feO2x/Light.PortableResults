using System.Linq;
using System.Reflection;
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
    public void PublicAddErrorSurface_ShouldExposeOnlySupportedOverloads()
    {
        var methods = typeof(Check<string>)
           .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
           .Where(method => method.Name == nameof(Check<>.AddError))
           .OrderBy(method => method.GetParameters().Length)
           .ToArray();

        methods.Should().HaveCount(3);
        methods.Should().OnlyContain(method => !method.IsGenericMethod);
        var firstParameterTypes = methods.Select(method => method.GetParameters()[0].ParameterType).ToArray();
        firstParameterTypes.Should().Contain(typeof(Error));
        firstParameterTypes.Should().Contain(typeof(string));
        firstParameterTypes.Should().Contain(typeof(ValidationErrorDefinition));

        methods.Should().NotContain(
            method => method.GetParameters()[0].ParameterType == typeof(ValidationErrorMessage)
        );
        methods.Should().NotContain(
            method => method.GetParameters()[0].ParameterType == typeof(IValidationErrorMessageTemplate)
        );

        var stringOverload = methods.Single(method => method.GetParameters()[0].ParameterType == typeof(string));
        stringOverload.GetParameters().Select(parameter => (parameter.Name, parameter.ParameterType)).Should().Equal(
            ("message", typeof(string)),
            ("code", typeof(string)),
            ("metadata", typeof(MetadataObject?)),
            ("target", typeof(ValidationTarget?)),
            ("category", typeof(ErrorCategory)),
            ("respectShortCircuit", typeof(bool))
        );
    }

    [Fact]
    public void AddErrorError_ShouldPreserveExplicitTarget()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
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
        var context = new DefaultValidationContextFactory().CreateValidationContext();
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
        var context = new DefaultValidationContextFactory().CreateValidationContext();
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
        var context = new DefaultValidationContextFactory().CreateValidationContext();
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
}
