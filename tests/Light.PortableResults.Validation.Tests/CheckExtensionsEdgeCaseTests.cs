using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CheckExtensionsEdgeCaseTests
{
    [Fact]
    public void ValidateChild_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedStringCheck = context.Check("Alice", target: "name").ShortCircuit();
        var validator = new TrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var sameTypeResult = shortCircuitedStringCheck.ValidateChild(validator);

        sameTypeResult.Should().Be(ValidatedValue<string>.NoValue);
    }

    [Fact]
    public void ValidateChild_WithTransformation_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedAddressCheck =
            context.Check(new AddressDto { ZipCode = "12345" }, target: "address").ShortCircuit();
        var validator = new AddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var transformingResult = shortCircuitedAddressCheck.ValidateChild(validator);

        transformingResult.Should().Be(ValidatedValue<AddressCommand>.NoValue);
    }

    [Fact]
    public void ValidateChild_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedStringCheck = context.Check("Alice", target: "name").ShortCircuit();
        Validator<string> nullStringValidator = null!;

        Action act = () => shortCircuitedStringCheck.ValidateChild(nullStringValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("childValidator");
    }

    [Fact]
    public void ValidateChild_WithTransformation_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedAddressCheck =
            context.Check(new AddressDto { ZipCode = "12345" }, target: "address").ShortCircuit();
        Validator<AddressDto, AddressCommand> nullAddressValidator = null!;

        Action act = () => shortCircuitedAddressCheck.ValidateChild(nullAddressValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("childValidator");
    }

    [Fact]
    public async Task ValidateChildAsync_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedStringCheck = context.Check("Alice", target: "name").ShortCircuit();
        var validator = new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var sameTypeResult = await shortCircuitedStringCheck.ValidateChildAsync(
            validator,
            TestContext.Current.CancellationToken
        );

        sameTypeResult.Should().Be(ValidatedValue<string>.NoValue);
    }

    [Fact]
    public async Task ValidateChildAsync_WithTransformation_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedAddressCheck =
            context.Check(new AddressDto { ZipCode = "12345" }, target: "address").ShortCircuit();
        var validator = new AsyncAddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var transformingResult = await shortCircuitedAddressCheck.ValidateChildAsync(
            validator,
            TestContext.Current.CancellationToken
        );

        transformingResult.Should().Be(ValidatedValue<AddressCommand>.NoValue);
    }

    [Fact]
    public async Task ValidateChildAsync_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedStringCheck = context.Check("Alice", target: "name").ShortCircuit();
        AsyncValidator<string> nullStringValidator = null!;

        Func<Task> act = async () => await shortCircuitedStringCheck.ValidateChildAsync(
            nullStringValidator,
            TestContext.Current.CancellationToken
        );

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("childValidator");
    }

    [Fact]
    public async Task ValidateChildAsync_WithTransformation_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedAddressCheck =
            context.Check(new AddressDto { ZipCode = "12345" }, target: "address").ShortCircuit();
        AsyncValidator<AddressDto, AddressCommand> nullAddressValidator = null!;

        Func<Task> act = async () => await shortCircuitedAddressCheck.ValidateChildAsync(
            nullAddressValidator,
            TestContext.Current.CancellationToken
        );

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("childValidator");
    }

    [Fact]
    public void ValidateItems_WithValidator_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedListCheck = context.Check(new List<string> { "A" }, target: "tags").ShortCircuit();
        var validator = new TrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var validatorResult = shortCircuitedListCheck.ValidateItems(validator);

        validatorResult.Should().Be(ValidatedValue<List<string>>.NoValue);
    }

    [Fact]
    public void ValidateItems_WithTransformingValidator_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedAddressListCheck = context
           .Check(new List<AddressDto> { new () { ZipCode = "12345" } }, target: "addresses")
           .ShortCircuit();
        var validator = new AddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var transformingResult = shortCircuitedAddressListCheck.ValidateItems(validator);

        transformingResult.Should().Be(ValidatedValue<List<AddressCommand>>.NoValue);
    }

    [Fact]
    public void ValidateItems_WithDelegate_ShouldReturnNoValue_WhenCheckIsShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedListCheck = context.Check(new List<string> { "A" }, target: "tags").ShortCircuit();

        var delegateResult = shortCircuitedListCheck.ValidateItems(
            (Action<Check<string>>) (static itemCheck => itemCheck.AddError("not reached", "Never"))
        );

        delegateResult.Should().Be(ValidatedValue<List<string>>.NoValue);
    }

    [Fact]
    public void ValidateItems_WithValidator_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedListCheck = context.Check(new List<string> { "A" }, target: "tags").ShortCircuit();
        Validator<string> nullValidator = null!;

        Action act = () => shortCircuitedListCheck.ValidateItems(nullValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public void ValidateItems_WithDelegate_ShouldThrow_WhenDelegateIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedListCheck = context.Check(new List<string> { "A" }, target: "tags").ShortCircuit();
        Func<Check<string>, ValidatedValue<string>> nullFunc = null!;

        Action act = () => shortCircuitedListCheck.ValidateItems(nullFunc);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validateItem");
    }
}
