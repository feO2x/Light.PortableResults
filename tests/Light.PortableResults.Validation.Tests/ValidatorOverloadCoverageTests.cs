using System;
using FluentAssertions;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidatorOverloadCoverageTests
{
    private static TrimmedRequiredTextValidator CreateSameTypeValidator() =>
        new (ValidationWorkflowTestData.ValidationContextFactory);

    private static StringLengthValidator CreateTransformingValidator() =>
        new (ValidationWorkflowTestData.ValidationContextFactory);

    private static ValidationContext CreateContext() =>
        ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

    [Fact]
    public void SameTypeValidator_CheckForErrors_ShouldHandleExplicitTarget()
    {
        var validator = CreateSameTypeValidator();

        var hasErrors = validator.CheckForErrors(
            null!,
            out var failure,
            ValidationTarget.Absolute("payload.name", isNormalized: true),
            "Name"
        );

        hasErrors.Should().BeTrue();
        failure.Errors.Should().ContainSingle(error => error.Target == "payload.name" && error.Code == "NotNull");
    }

    [Fact]
    public void SameTypeValidator_CheckForErrors_ShouldHandleProvidedContext()
    {
        var validator = CreateSameTypeValidator();
        var context = CreateContext();

        var hasErrors = validator.CheckForErrors(
            " ",
            context,
            out var failure,
            displayName: "Name"
        );

        hasErrors.Should().BeTrue();
        failure.Errors.Should().ContainSingle(error => error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void SameTypeValidator_TryValidate_ShouldHandleProvidedContext()
    {
        var validator = CreateSameTypeValidator();
        var context = CreateContext();

        var isValid = validator.TryValidate(
            "  Alice  ",
            context,
            out var validatedAlice,
            out var failure,
            displayName: "Name"
        );

        isValid.Should().BeTrue();
        validatedAlice.Should().Be("Alice");
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SameTypeValidator_TryValidate_ShouldHandleExplicitTarget()
    {
        var validator = CreateSameTypeValidator();

        var isValid = validator.TryValidate(
            "  Bob  ",
            out var validatedBob,
            out var failure,
            ValidationTarget.Absolute("payload.otherName", isNormalized: true),
            "Name"
        );

        isValid.Should().BeTrue();
        validatedBob.Should().Be("Bob");
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SameTypeValidator_CheckForErrors_ShouldHandleSuccessWithContext()
    {
        var validator = CreateSameTypeValidator();
        var context = CreateContext();

        var hasErrors = validator.CheckForErrors(
            "  Alice  ",
            context,
            out var failure,
            ValidationTarget.Relative("name", isNormalized: true),
            "Name"
        );

        hasErrors.Should().BeFalse();
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SameTypeValidator_TryValidate_ShouldHandleFailureWithContext()
    {
        var validator = CreateSameTypeValidator();
        var context = CreateContext();

        var isValid = validator.TryValidate(
            " ",
            context,
            out var validatedValue,
            out var failure,
            ValidationTarget.Relative("name", isNormalized: true),
            "Name"
        );

        isValid.Should().BeFalse();
        validatedValue.Should().BeNull();
        failure.Errors.Should().ContainSingle(error => error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void TransformingValidator_Validate_ShouldHandleProvidedContext()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        var validationResult = validator.Validate(" ", context, displayName: "Text");

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(error => error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void TransformingValidator_CheckForErrors_ShouldHandleExplicitTarget()
    {
        var validator = CreateTransformingValidator();

        var hasErrors = validator.CheckForErrors(
            null!,
            out var failure,
            ValidationTarget.Absolute("payload.length", isNormalized: true),
            "Text"
        );

        hasErrors.Should().BeTrue();
        failure.Errors.Should().ContainSingle(error => error.Target == "payload.length" && error.Code == "NotNull");
    }

    [Fact]
    public void TransformingValidator_CheckForErrors_ShouldHandleProvidedContext()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        var hasErrors = validator.CheckForErrors(
            " ",
            context,
            out var failure,
            displayName: "Text"
        );

        hasErrors.Should().BeTrue();
        failure.Errors.Should().ContainSingle(error => error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void TransformingValidator_TryValidate_ShouldHandleProvidedContext()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        var isValid = validator.TryValidate(
            "  Alice  ",
            context,
            out var validatedAliceLength,
            out var failure,
            displayName: "Text"
        );

        isValid.Should().BeTrue();
        validatedAliceLength.Should().Be(5);
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TransformingValidator_TryValidate_ShouldHandleExplicitTarget()
    {
        var validator = CreateTransformingValidator();

        var isValid = validator.TryValidate(
            "  Bob  ",
            out var validatedBobLength,
            out var failure,
            ValidationTarget.Absolute("payload.otherLength", isNormalized: true),
            "Text"
        );

        isValid.Should().BeTrue();
        validatedBobLength.Should().Be(3);
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TransformingValidator_CheckForErrors_ShouldHandleSuccessWithContext()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        var hasErrors = validator.CheckForErrors(
            "  Alice  ",
            context,
            out var failure,
            ValidationTarget.Relative("text", isNormalized: true),
            "Text"
        );

        hasErrors.Should().BeFalse();
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TransformingValidator_TryValidate_ShouldHandleFailureWithContext()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        var isValid = validator.TryValidate(
            " ",
            context,
            out var validatedLength,
            out var failure,
            ValidationTarget.Relative("text", isNormalized: true),
            "Text"
        );

        isValid.Should().BeFalse();
        validatedLength.Should().Be(0);
        failure.Errors.Should().ContainSingle(error => error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void SameTypeValidator_Validate_ShouldThrow_WhenContextIsNull()
    {
        var validator = CreateSameTypeValidator();

        Action act = () => validator.Validate(
            "Alice",
            default,
            ValidationTarget.Absolute("name", isNormalized: true),
            "Name"
        );

        act.Should().Throw<ArgumentException>().WithParameterName("context");
    }

    [Fact]
    public void TransformingValidator_Validate_ShouldThrow_WhenTargetIsDefault()
    {
        var validator = CreateTransformingValidator();
        var context = CreateContext();

        Action act = () => validator.Validate("Alice", context, (ValidationTarget) default, "Name");

        act.Should().Throw<ArgumentException>().WithParameterName("target");
    }
}
