using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Normalization;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationInfrastructureEdgeCaseTests
{
    [Fact]
    public void ValidationTargets_Compose_ShouldThrow_WhenPrefixIsNull()
    {
        Action act = () => ValidationTargets.Compose(null!, "child");
        act.Should().Throw<ArgumentNullException>().WithParameterName("prefix");
    }

    [Fact]
    public void ValidationTargets_Compose_ShouldThrow_WhenTargetIsNull()
    {
        Action act = () => ValidationTargets.Compose("prefix", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("target");
    }

    [Fact]
    public void ValidationTargets_Compose_ShouldReturnTarget_WhenPrefixIsEmpty() =>
        ValidationTargets.Compose(string.Empty, "child").Should().Be("child");

    [Fact]
    public void ValidationTargets_Compose_ShouldReturnPrefix_WhenTargetIsEmpty() =>
        ValidationTargets.Compose("prefix", string.Empty).Should().Be("prefix");

    [Fact]
    public void ValidationTargets_Compose_ShouldHandleBracketComposition() =>
        ValidationTargets.Compose("items", "[0]").Should().Be("items[0]");

    [Fact]
    public void ValidationTargets_IsSimpleIdentifier_ShouldReturnTrue_WhenTargetIsEmpty() =>
        ValidationTargets.IsSimpleIdentifier(string.Empty).Should().BeTrue();

    [Fact]
    public void ValidationTargets_IsSimpleIdentifier_ShouldReturnFalse_WhenTargetContainsBrackets() =>
        ValidationTargets.IsSimpleIdentifier("items[0]").Should().BeFalse();

    [Fact]
    public void ValidationTarget_IsDefault_ShouldBeTrue_ForDefaultInstance() =>
        default(ValidationTarget).IsDefault.Should().BeTrue();

    [Fact]
    public void ValidationTarget_IsNormalized_ShouldBeFalse_ForDefaultInstance() =>
        default(ValidationTarget).IsNormalized.Should().BeFalse();

    [Fact]
    public void ValidationTarget_Constructor_ShouldThrow_WhenInputIsNull()
    {
        Action act = () => _ = new ValidationTarget(null!, ValidationTargetSemantics.Relative);
        act.Should().Throw<ArgumentNullException>().WithParameterName("input");
    }

    [Fact]
    public void ValidationTarget_Constructor_ShouldThrow_WhenSemanticsIsInvalid()
    {
        Action act = () => _ = new ValidationTarget(
            "field",
            (ValidationTargetSemantics) 99,
            isNormalized: true
        );
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("semantics");
    }

    [Fact]
    public void ValidationState_TryGetItem_ShouldReturnFalse_WhenKeyIsMissing()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var missingIntKey = new ValidationContextKey<int>("missing-number");
        state.TryGetItem(missingIntKey, out var missingNumber).Should().BeFalse();
        missingNumber.Should().Be(0);
    }

    [Fact]
    public void ValidationState_TryGetItem_ShouldReturnTrue_WhenItemIsPresent()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var stringKey = new ValidationContextKey<string>("tenant");
        state.SetItem(stringKey, "checkout");
        state.TryGetItem(stringKey, out var tenant).Should().BeTrue();
        tenant.Should().Be("checkout");
    }

    [Fact]
    public void ValidationState_TryGetItem_ShouldHandleNullValues()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var nullValueKey = new ValidationContextKey<string?>("optional");
        state.SetItem(nullValueKey, null);
        state.TryGetItem(nullValueKey, out var optional).Should().BeTrue();
        optional.Should().BeNull();
    }

    [Fact]
    public void ValidationState_RemoveItem_ShouldReturnTrue_WhenItemExists()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var stringKey = new ValidationContextKey<string>("tenant");
        state.SetItem(stringKey, "checkout");
        state.RemoveItem(stringKey).Should().BeTrue();
    }

    [Fact]
    public void ValidationState_RemoveItem_ShouldReturnFalse_WhenItemDoesNotExist()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var stringKey = new ValidationContextKey<string>("tenant");
        state.RemoveItem(stringKey).Should().BeFalse();
    }

    [Fact]
    public void ValidationState_Errors_ShouldContainAddedError()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var error = new Error { Message = "invalid", Code = "Invalid", Target = "field" };
        state.AddError(error);
        state.Errors.Should().Equal(new Errors(error));
    }

    [Fact]
    public void ValidationState_TryGetErrorsSince_ShouldReturnAllErrors_WhenStartingFromZero()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var error = new Error { Message = "invalid", Code = "Invalid", Target = "field" };
        state.AddError(error);
        state.TryGetErrorsSince(0, out var errorsSinceStart).Should().BeTrue();
        errorsSinceStart.Should().Equal(new Errors(error));
    }

    [Fact]
    public void ValidationState_TryGetErrorsSince_ShouldThrow_WhenStartingCountIsInvalid()
    {
        var state = new ValidationState(new ValidationContextOptions());
        Action act = () => state.TryGetErrorsSince(2, out _);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("startingErrorCount");
    }

    [Fact]
    public void ValidationState_AddError_ShouldThrow_WhenErrorIsDefault()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var act = () => state.AddError(default);
        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void ValidationContext_RemoveItem_ShouldReturnFalse_WhenKeyIsMissing()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var missingKey = new ValidationContextKey<string?>("missing");
        context.RemoveItem(missingKey).Should().BeFalse();
    }

    [Fact]
    public void ValidationContext_SetItem_ShouldBeObservableInParentContext()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var tenantKey = new ValidationContextKey<string?>("tenant");
        childContext.SetItem(tenantKey, "checkout");
        context.TryGetItem(tenantKey, out var initialTenant).Should().BeTrue();
        initialTenant.Should().Be("checkout");
    }

    [Fact]
    public void ValidationContext_GetRequiredItem_ShouldReturnStoredValue()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var tenantKey = new ValidationContextKey<string?>("tenant");
        context.SetItem(tenantKey, "catalog");
        childContext.GetRequiredItem(tenantKey).Should().Be("catalog");
    }

    [Fact]
    public void ValidationContext_RemoveItem_ShouldReturnTrue_WhenItemExists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var tenantKey = new ValidationContextKey<string?>("tenant");
        context.SetItem(tenantKey, "catalog");
        context.RemoveItem(tenantKey).Should().BeTrue();
    }

    [Fact]
    public void ValidationContext_TryGetItem_ShouldReturnFalse_AfterRemoval()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var tenantKey = new ValidationContextKey<string?>("tenant");
        context.SetItem(tenantKey, "catalog");
        context.RemoveItem(tenantKey);
        context.TryGetItem(tenantKey, out var removedTenant).Should().BeFalse();
        removedTenant.Should().BeNull();
    }

    [Fact]
    public void ValidationContext_GetRequiredItem_ShouldThrow_WhenKeyIsMissing()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var tenantKey = new ValidationContextKey<string?>("tenant");
        Action act = () => context.GetRequiredItem(tenantKey);
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ValidationContext_SetItem_ShouldThrow_WhenKeyIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var act = () => context.SetItem<string>(null!, "value");
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void ValidationContext_TryGetItem_ShouldThrow_WhenKeyIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        Action act = () => context.TryGetItem<string>(null!, out _);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void ValidationContext_GetRequiredItem_ShouldThrow_WhenKeyIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        Action act = () => context.GetRequiredItem<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void ValidationContext_RemoveItem_ShouldThrow_WhenKeyIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        Action act = () => context.RemoveItem<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void ValidationContext_Constructor_ShouldThrow_WhenStateIsNull()
    {
        Action act = () => _ = new ValidationContext(null!, string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("state");
    }

    [Fact]
    public void ValidationContext_Constructor_ShouldThrow_WhenTargetPrefixIsNull()
    {
        var state = new ValidationState(new ValidationContextOptions());
        Action act = () => _ = new ValidationContext(state, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("targetPrefix");
    }

    [Fact]
    public void ValidationContext_ThrowIfDefault_ShouldThrow_ForDefaultInstance()
    {
        ValidationContext context = default;
        var act = () => context.ThrowIfDefault();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*must not be the default instance*");
    }

    [Fact]
    public void ValidationContext_TargetPrefix_ShouldThrow_ForDefaultInstance()
    {
        ValidationContext context = default;
        Action act = () => _ = context.TargetPrefix;
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*must not be the default instance*");
    }

    [Fact]
    public void ValidationContext_Check_ShouldThrow_WhenTargetIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("value", target: null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("target");
    }

    [Fact]
    public void ValidationContext_AddError_ShouldThrow_WhenMessageIsNull()
    {
        var context = CreateContext();
        var act = () => context.AddError(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    [Fact]
    public void ValidationContext_AddError_ShouldThrow_WhenErrorIsDefault()
    {
        var context = CreateContext();
        var act = () => context.AddError(default);
        act.Should().Throw<ArgumentException>().WithParameterName("error");
    }

    [Fact]
    public void ValidationContext_NormalizeTarget_ShouldThrow_WhenTargetIsDefault()
    {
        var context = CreateContext();
        Action act = () => context.NormalizeTarget(default);
        act.Should().Throw<ArgumentException>().WithParameterName("target");
    }

    [Fact]
    public void ValidationContext_TryCreateAutomaticNullError_ShouldThrow_WhenDisplayNameIsNull()
    {
        var context = CreateContext();
        Action act = () => context.TryCreateAutomaticNullError<string?>(
            null,
            ValidationTarget.CallerExpression("request.Name"),
            null!,
            out _
        );
        act.Should().Throw<ArgumentNullException>().WithParameterName("displayName");
    }

    private static ValidationContext CreateContext() =>
        new (new ValidationState(new ValidationContextOptions()), string.Empty);

    [Fact]
    public void ValidationState_TryGetItem_ShouldReturnFalse_WhenStoredValueCannotBeCastToKeyType()
    {
        var state = new ValidationState(new ValidationContextOptions());
        var key = new ValidationContextKey<string>("tenant");

        state.SetItem(key, "checkout");

        var itemsField = typeof(ValidationState).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
        itemsField.Should().NotBeNull();
        var items = (Dictionary<object, object?>) itemsField.GetValue(state)!;
        items[key] = 123;

        state.TryGetItem(key, out var tenant).Should().BeFalse();
        tenant.Should().BeNull();
    }

    [Fact]
    public void ValidationErrorMessageFormatting_FormatParameter_ShouldFormatDecimalWithCultureInfo()
    {
        var context = CreateDeDeContext();
        ValidationErrorMessageFormatting.FormatParameter(1234.5m, context).Should().Be("1234,5");
    }

    [Fact]
    public void ValidationErrorMessageFormatting_FormatParameter_ShouldCallToString_WhenTypeIsUnknown()
    {
        var context = CreateDeDeContext();
        ValidationErrorMessageFormatting.FormatParameter(new PlainValue("alpha"), context).Should().Be("plain:alpha");
    }

    [Fact]
    public void ValidationErrorMessageFormatting_FormatParameter_ShouldReturnEmptyString_WhenValueIsNull()
    {
        var context = CreateDeDeContext();
        ValidationErrorMessageFormatting.FormatParameter<string?>(null, context).Should().BeEmpty();
    }

    [Fact]
    public void TrimStringNormalizer_Normalize_ShouldTrimString() =>
        new TrimStringNormalizer().Normalize("  Alice  ").Should().Be("Alice");

    [Fact]
    public void TrimStringNormalizer_Normalize_ShouldTrimBoxedString() =>
        new TrimStringNormalizer().Normalize((object) "  Alice  ").Should().Be("Alice");

    [Fact]
    public void TrimStringNormalizer_Normalize_ShouldReturnNull_WhenBoxedValueIsNull() =>
        new TrimStringNormalizer().Normalize((object?) null).Should().BeNull();

    [Fact]
    public void TrimStringNormalizer_Normalize_ShouldReturnOriginalValue_WhenValueIsNotAString() =>
        new TrimStringNormalizer().Normalize(42).Should().Be(42);

    private static ReadOnlyValidationContext CreateDeDeContext() =>
        new DefaultValidationContextFactory(
            new ValidationContextOptions() with
            {
                CultureInfo = CultureInfo.GetCultureInfo("de-DE")
            }
        ).CreateValidationContext().AsReadOnly();

    private sealed class PlainValue
    {
        private readonly string _value;

        public PlainValue(string value) => _value = value;

        public override string ToString() => "plain:" + _value;
    }
}
