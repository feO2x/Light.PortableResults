using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Normalization;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class BuiltInRuleFamilyWorkflowTests
{
    [Fact]
    public void IsNull_ShouldAddError_WhenValueIsNotNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("abc", NoOpValueNormalizer.Instance, target: "code", displayName: "Code").IsNull();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "code" &&
                error.Code == "Null" &&
                error.Message == "Code must be null"
        );
    }

    [Fact]
    public void IsNotNull_ShouldAddError_WhenValueIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        string? nullableName = null;

        context
           .Check(nullableName, NoOpValueNormalizer.Instance, target: "name", displayName: "Name")
           .IsNotNull("Name is required");

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "name" &&
                error.Code == "NotNull" &&
                error.Message == "Name is required"
        );
    }

    [Fact]
    public void GuardedChecks_ShouldAllowIsNullWithOverrides_WhenValueIsNotNull()
    {
        var context = CreateNullHandlingDisabledContext();

        context
           .Check("abc", target: "code", displayName: "Code")
           .IsNull(new ErrorOverrides { Code = "CodeMustBeNull" });

        context.Errors.Should().ContainSingle(error => error.Target == "code" && error.Code == "CodeMustBeNull");
    }

    [Fact]
    public void GuardedChecks_HasMaxLength_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        string? nullableText = null;

        Action act = () => context
           .Check(nullableText, NoOpValueNormalizer.Instance, target: "name")
           .HasMaxLength(1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-null string*");
    }

    [Fact]
    public void GuardedChecks_HasCount_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        IEnumerable<int> nullableItems = null!;

        Action act = () => context.Check(nullableItems, target: "items").HasCount(1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-null collection*");
    }

    [Fact]
    public void GuardedChecks_IsGreaterThan_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        object? nullableReference = null;

        Action act = () => context.Check(nullableReference, target: "reference").IsGreaterThan(new object());

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-null value*");
    }

    [Fact]
    public void GuardedChecks_HasPrecisionAndScale_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        decimal? nullableAmount = null;

        Action act = () => context
           .Check(nullableAmount, NoOpValueNormalizer.Instance, target: "amount")
           .HasPrecisionAndScale(4, 2);

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-null value*");
    }

    [Fact]
    public void
        GuardedChecks_HasPrecisionAndScaleWithOverride_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        decimal? nullableAmount = null;

        Action act = () => context
           .Check(nullableAmount, NoOpValueNormalizer.Instance, target: "amountOverride")
           .HasPrecisionAndScale(4, 2, new ErrorOverrides { Code = "AmountFormat" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*non-null value*");
    }

    [Fact]
    public void GuardedChecks_HasMinLength_ShouldThrow_WhenMinLengthIsInvalid()
    {
        var context = CreateNullHandlingDisabledContext();

        Action act = () => context.Check("A", target: "code").HasMinLength(-1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*zero or greater*");
    }

    [Fact]
    public void GuardedChecks_HasLengthIn_ShouldThrow_WhenRangeIsInvalid()
    {
        var context = CreateNullHandlingDisabledContext();

        Action act = () => context.Check("A", target: "code").HasLengthIn(3, 2);

        act.Should().Throw<ArgumentException>().WithMessage("*maxLength*minLength*");
    }

    [Fact]
    public void GuardedChecks_HasCount_ShouldThrow_WhenExpectedCountIsInvalid()
    {
        var context = CreateNullHandlingDisabledContext();

        Action act = () => context.Check<string?>("A", target: "code").HasCount(-1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*zero or greater*");
    }

    [Fact]
    public void GuardedChecks_HasPrecisionAndScale_ShouldThrow_WhenPrecisionIsInvalid()
    {
        var context = CreateNullHandlingDisabledContext();

        Action act = () => context.Check(12.34m, target: "amount").HasPrecisionAndScale(0, 1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void GuardedChecks_HasPrecisionAndScale_ShouldThrow_WhenScaleIsInvalid()
    {
        var context = CreateNullHandlingDisabledContext();

        Action act = () => context.Check(12.34m, target: "amount").HasPrecisionAndScale(2, 3);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*greater than precision*");
    }

    [Fact]
    public void GuardedChecks_ShouldShortCircuit_WhenValueIsNull()
    {
        var context = CreateNullHandlingDisabledContext();
        decimal? nullableAmount = null;

        var check = context
           .Check(nullableAmount, NoOpValueNormalizer.Instance, target: "shortCircuitedAmount")
           .ShortCircuit()
           .HasPrecisionAndScale(4, 2);

        check.IsShortCircuited.Should().BeTrue();
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void GuardedChecks_ShouldShortCircuit_WhenValueIsNullWithOverrides()
    {
        var context = CreateNullHandlingDisabledContext();
        decimal? nullableAmount = null;

        var check = context
           .Check(nullableAmount, NoOpValueNormalizer.Instance, target: "shortCircuitedAmountOverride")
           .ShortCircuit()
           .HasPrecisionAndScale(4, 2, new ErrorOverrides { Code = "Unused" });

        check.IsShortCircuited.Should().BeTrue();
        context.HasErrors.Should().BeFalse();
    }

    private static ValidationContext CreateNullHandlingDisabledContext()
    {
        return new DefaultValidationContextFactory(
                new ValidationContextOptions() with
                {
                    ValueNormalizer = NoOpValueNormalizer.Instance,
                    AutomaticNullErrorProvider = NoOpAutomaticNullErrorProvider.Instance
                }
            )
           .CreateValidationContext();
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenStringIsNotEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check<string?>("value", target: "description", displayName: "Description").IsEmpty();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "description" &&
                error.Code == "Empty" &&
                error.Message == "Description must be empty"
        );
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenGuidIsEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(Guid.Empty, target: "id", displayName: "Id").IsNotEmpty();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "id" &&
                error.Code == "NotEmpty" &&
                error.Message == "Id must not be empty"
        );
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenCollectionIsEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IEnumerable<int> items = Array.Empty<int>();

        context.Check(items, displayName: "Items").IsNotEmpty();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "items" &&
                error.Code == "NotEmpty" &&
                error.Message == "Items must not be empty"
        );
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenImmutableArrayIsEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(ImmutableArray<int>.Empty, target: "tags", displayName: "Tags").IsNotEmpty();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "tags" &&
                error.Code == "NotEmpty" &&
                error.Message == "Tags must not be empty"
        );
    }

    [Fact]
    public void IsEqualTo_ShouldSucceed_WhenValuesAreEqual()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check("abc", target: "code", displayName: "Code")
           .IsEqualTo("ABC", StringComparer.OrdinalIgnoreCase);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNotEqualTo_ShouldAddError_WhenValuesAreEqual()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check("abc", target: "code", displayName: "Code")
           .IsNotEqualTo(
                "ABC",
                StringComparer.OrdinalIgnoreCase,
                new ErrorOverrides { Message = "Code must stay different from ABC" }
            );

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "code" &&
                error.Code == "NotEqualTo" &&
                error.Message == "Code must stay different from ABC"
        );
    }

    [Fact]
    public void ComparableChecks_ShouldThrow_WhenValueIsNullAndAutomaticNullHandlingIsDisabled()
    {
        var context = CreateNullHandlingDisabledContext();
        int? nullableAge = null;

        Action act = () => context.Check(nullableAge, displayName: "Age").IsGreaterThan(18);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ComparableChecks_ShouldShortCircuit_WhenValueIsNullAfterIsNotNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        int? nullableAge = null;

        var check = context.Check(nullableAge, displayName: "Age").IsNotNull().IsGreaterThan(18);

        check.IsShortCircuited.Should().BeTrue();
        context.Errors.Should().ContainSingle(error => error.Code == "NotNull");
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_ShouldAddError_WhenValueIsSmaller()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(10, target: "requiredAge", displayName: "Required age")
           .IsGreaterThanOrEqualTo(18, new ErrorOverrides { Code = "Adult" });

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "requiredAge" &&
                error.Code == "Adult"
        );
    }

    [Fact]
    public void IsInExclusiveRange_ShouldAddError_WhenValueIsAtLowerBoundary()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(10, target: "requiredAge", displayName: "Required age")
           .IsInExclusiveRange(10, 20);

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "requiredAge" &&
                error.Code == "ExclusiveRange"
        );
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_ShouldAddError_WhenStringIsWhitespace()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(" ", target: "firstName", displayName: "First name").IsNotNullOrWhiteSpace();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "firstName" &&
                error.Code == "NotNullOrWhiteSpace"
        );
    }

    [Fact]
    public void HasMinLength_ShouldAddError_WhenStringIsTooShort()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("A", target: "code", displayName: "Code").HasMinLength(2);

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "code" &&
                error.Code == "MinLength"
        );
    }

    [Fact]
    public void HasMaxLength_ShouldAddError_WhenStringIsTooLong()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check<string?>("ABCD", target: "shortCode", displayName: "Short code")
           .HasMaxLength(3, "Short code is too long");

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "shortCode" &&
                error.Code == "MaxLength" &&
                error.Message == "Short code is too long"
        );
    }

    [Fact]
    public void HasLengthIn_ShouldAddError_WhenStringIsTooShortForRange()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("AB", target: "password", displayName: "Password").HasLengthIn(3, 5);

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "password" &&
                error.Code == "LengthIn"
        );
    }

    [Fact]
    public void Matches_ShouldAddError_WhenStringDoesNotMatchPattern()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("AB12", target: "digits", displayName: "Digits").Matches("^[0-9]+$");

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "digits" &&
                error.Code == "Matches"
        );
    }

    [Fact]
    public void Matches_ShouldAddError_WhenStringDoesNotMatchRegex()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check("AB12", target: "letters", displayName: "Letters")
           .Matches(new Regex("^[A-Z]+$", RegexOptions.IgnoreCase), "Letters are invalid");

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "letters" &&
                error.Code == "Matches" &&
                error.Message == "Letters are invalid"
        );
    }

    [Fact]
    public void IsEmail_ShouldAddError_WhenStringIsNotAnEmail()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("abc@", target: "email", displayName: "Email").IsEmail();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "email" &&
                error.Code == "Email"
        );
    }

    [Fact]
    public void ContainsOnlyDigits_ShouldAddError_WhenStringContainsLetters()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("12A", target: "phone", displayName: "Phone").ContainsOnlyDigits();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "phone" &&
                error.Code == "DigitsOnly"
        );
    }

    [Fact]
    public void ContainsOnlyLettersAndDigits_ShouldAddError_WhenStringContainsSpecialCharacters()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("A-1", target: "zipCode", displayName: "Zip code").ContainsOnlyLettersAndDigits();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "zipCode" &&
                error.Code == "LettersAndDigitsOnly"
        );
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenStringLengthIsWrong()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check<string?>("AB", target: "code", displayName: "Code").HasCount(3);

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "code" &&
                error.Code == "Count"
        );
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenCollectionIsTooSmall()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check<IReadOnlyList<int>>(new CountingReadOnlyList(1, 2), target: "items", displayName: "Items")
           .HasMinCount(3);

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "items" &&
                error.Code == "MinCount"
        );
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenImmutableArrayIsTooLarge()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1, 2, 3), target: "tags", displayName: "Tags")
           .HasMaxCount(2, new ErrorOverrides { Code = "TooManyTags" });

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "tags" &&
                error.Code == "TooManyTags"
        );
    }

    [Fact]
    public void IsInEnum_ShouldAddError_WhenValueIsNotDefined()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        OrderStatus? invalidStatus = (OrderStatus) 99;

        context.Check(invalidStatus, target: "status", displayName: "Status").IsInEnum();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "status" &&
                error.Code == "Enum"
        );
    }

    [Fact]
    public void IsEnumName_ShouldAddError_WhenStringIsNotDefinedInEnum()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check<string?>("PendingApproval", target: "statusName", displayName: "Status name")
           .IsEnumName<OrderStatus>("Status name is invalid");

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "statusName" &&
                error.Code == "EnumName" &&
                error.Message == "Status name is invalid"
        );
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldAddError_WhenValueHasTooManyDecimalPlaces()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        decimal? invalidAmount = 123.4500m;

        context
           .Check(invalidAmount, target: "amount", displayName: "Amount")
           .HasPrecisionAndScale(4, 2, new ErrorOverrides { Message = "Amount format is invalid" });

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "amount" &&
                error.Code == "PrecisionScale" &&
                error.Message == "Amount format is invalid"
        );
    }

    [Fact]
    public void Predicate_ShouldAddError_WhenContextAwarePredicateFails()
    {
        var tenantKey = new ValidationContextKey<string>("tenant");
        var context = new DefaultValidationContextFactory(
            new ValidationContextOptions() with { ValueNormalizer = NoOpValueNormalizer.Instance }
        ).CreateValidationContext();
        context.SetItem(tenantKey, "checkout");
        const string tenantCode = "Alice";

        context
           .Check(tenantCode, displayName: "Tenant code")
           .Must(
                (readOnlyContext, _) =>
                    readOnlyContext.GetRequiredItem(tenantKey) == "catalog",
                new ErrorOverrides { Message = "Tenant mismatch" }
            );

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "tenantCode" &&
                error.Message == "Tenant mismatch"
        );
    }

    [Fact]
    public void Predicate_ShouldAddError_WhenSimplePredicateFails()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .ForMember("customer", isNormalized: true)
           .Check("AB", target: "code", displayName: "Code")
           .Must(
                static value => value.Length > 2,
                new ErrorOverrides { Category = ErrorCategory.UnprocessableContent }
            );

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "customer.code" &&
                error.Code == "Predicate" &&
                error.Category == ErrorCategory.UnprocessableContent
        );
    }

    [Fact]
    public void Custom_ShouldAddErrors_WhenCustomValidationIsInvoked()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .ForMember("customer", isNormalized: true)
           .Check("AB", target: "code", displayName: "Code")
           .Custom(
                static (customContext, value) =>
                {
                    if (value.Length <= 0)
                    {
                        return;
                    }

                    customContext.AddError("Code contains unsupported data", "Custom");
                    customContext.AddError(
                        "Code detail is invalid",
                        "CustomDetail",
                        target: ValidationTarget.Relative(
                            "detail",
                            isNormalized: true
                        )
                    );
                }
            );

        context.Errors.Should().HaveCount(2);
        context.Errors.Should().ContainSingle(error => error.Target == "customer.code" && error.Code == "Custom");
        context.Errors.Should()
           .ContainSingle(error => error.Target == "customer.code.detail" && error.Code == "CustomDetail");
    }

    [Fact]
    public void Predicate_ShouldNotAddError_WhenSuccessful()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var passThroughCheck = context.Check("AB", target: "otherCode", displayName: "Other code");

        passThroughCheck
           .Must(static value => value.Length == 2, BuiltInValidationErrorDefinitions.Predicate)
           .Must(
                static (readOnlyContext, value) =>
                    readOnlyContext.TargetPrefix == "otherCode" && value.Length == 2,
                new ErrorOverrides { Code = "ShouldNotFail" }
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Custom_ShouldNotBeInvoked_WhenShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuited = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var invoked = false;

        shortCircuited.Custom((_, _) => invoked = true);

        invoked.Should().BeFalse();
    }

    [Fact]
    public void Predicate_ShouldThrow_WhenPredicateIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check("AB", target: "code");

        Action act = () => check.Must((Func<string, bool>) null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void Predicate_ShouldThrow_WhenContextPredicateIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check("AB", target: "code");

        Action act = () => check.Must((Func<ReadOnlyValidationContext, string, bool>) null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void Predicate_ShouldThrow_WhenTemplateIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check("AB", target: "code");

        Action act = () => check.Must(static _ => false, (IValidationErrorMessageTemplate) null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("template");
    }

    [Fact]
    public void Custom_ShouldThrow_WhenCustomValidationIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check("AB", target: "code");

        Action act = () => check.Custom(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("customValidation");
    }

    [Fact]
    public void IsGreaterThan_ShouldThrow_WhenBoundaryIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        Action act = () => context.Check("AB", target: "rangeCode").IsGreaterThan(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("comparativeValue");
    }

    [Fact]
    public void IsIn_ShouldThrow_WhenLowerBoundaryIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        Action act = () => context.Check("AB", target: "rangeCode").IsInBetween(null!, "ZZ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("lowerBoundary");
    }

    [Fact]
    public void IsNotIn_ShouldThrow_WhenUpperBoundaryIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        Action act = () => context.Check("AB", target: "rangeCode").IsNotInBetween("AA", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("upperBoundary");
    }

    [Fact]
    public void Predicate_ShouldSupportMetadata()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var metadata = MetadataObject.Create(("kind", "predicate"));
        var check = context.Check("AB", target: "code", displayName: "Code");

        check.Must(
            static value => value.Length == 1,
            new ValidationErrorTemplates.Constant("Never used"),
            code: "PredicateWithMetadata",
            metadata: metadata
        );

        context.Errors.Should().ContainSingle(
            error =>
                error.Code == "PredicateWithMetadata" &&
                error.Metadata.HasValue &&
                error.Metadata.Value.ContainsKey("kind")
        );
    }

    [Fact]
    public void ContainsOnlyDigits_ShouldAddError_WhenStringIsEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(string.Empty, target: "digitsOnly", displayName: "Digits only")
           .ContainsOnlyDigits();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "digitsOnly" &&
                error.Code == "DigitsOnly"
        );
    }

    [Fact]
    public void ContainsOnlyLettersAndDigits_ShouldAddError_WhenStringIsEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(string.Empty, target: "lettersDigitsOnly", displayName: "Letters and digits only")
           .ContainsOnlyLettersAndDigits(new ErrorOverrides { Code = "LettersDigitsOnly" });

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "lettersDigitsOnly" &&
                error.Code == "LettersDigitsOnly"
        );
    }

    [Fact]
    public void HasCount_ShouldSucceed_WhenEnumeratorOnlyCollectionHasExpectedCount()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var enumeratorOnlyItems = new YieldOnlyEnumerable(1, 2);

        context.Check<IEnumerable>(enumeratorOnlyItems, target: "items", displayName: "Items").HasCount(2);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasMinCount_ShouldSucceed_WhenEnumeratorOnlyCollectionHasExpectedCount()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var enumeratorOnlyItems = new YieldOnlyEnumerable(1, 2);

        context.Check<IEnumerable>(enumeratorOnlyItems, target: "itemsMin", displayName: "Items min")
           .HasMinCount(2, new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasMaxCount_ShouldSucceed_WhenEnumeratorOnlyCollectionHasExpectedCount()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var enumeratorOnlyItems = new YieldOnlyEnumerable(1, 2);

        context.Check<IEnumerable>(enumeratorOnlyItems, target: "itemsMax", displayName: "Items max").HasMaxCount(2);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Predicate_ShouldNotBeInvoked_WhenShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedCheck = context.Check("AB", target: "shortCode", displayName: "Short code").ShortCircuit();
        var invoked = false;

        shortCircuitedCheck.Must(
            _ =>
            {
                invoked = true;
                return false;
            }
        );

        invoked.Should().BeFalse();
    }

    [Fact]
    public void IsNull_ShouldSucceed_WhenValueIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        string? nullableValue = null;

        context.Check(nullableValue, NoOpValueNormalizer.Instance, target: "nullableCode", displayName: "Nullable code")
           .IsNull();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNullWithOverride_ShouldSucceed_WhenValueIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        string? nullableValue = null;

        context.Check(
                nullableValue,
                NoOpValueNormalizer.Instance,
                target: "nullableCodeOverride",
                displayName: "Nullable code override"
            )
           .IsNull(new ErrorOverrides { Code = "UnusedNull" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNotNull_ShouldSucceed_WhenValueIsNotNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("AB", target: "requiredCode", displayName: "Required code").IsNotNull();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNotNullWithOverride_ShouldSucceed_WhenValueIsNotNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check("AB", target: "requiredCodeOverride", displayName: "Required code override")
           .IsNotNull(new ErrorOverrides { Code = "UnusedNotNull" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void NullChecks_ShouldNotBeInvoked_WhenShortCircuited()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var shortCircuitedCheck = context.Check("AB", target: "shortCode", displayName: "Short code").ShortCircuit();

        shortCircuitedCheck.IsNull().IsShortCircuited.Should().BeTrue();
        shortCircuitedCheck.IsNull(new ErrorOverrides { Code = "UnusedNull" }).IsShortCircuited.Should().BeTrue();
        shortCircuitedCheck.IsNotNull().IsShortCircuited.Should().BeTrue();
        shortCircuitedCheck.IsNotNull(new ErrorOverrides { Code = "UnusedNotNull" }).IsShortCircuited.Should().BeTrue();
        context.HasErrors.Should().BeFalse();
    }

    private sealed class CountingReadOnlyList : IReadOnlyList<int>
    {
        private readonly int[] _values;

        public CountingReadOnlyList(params int[] values) => _values = values;

        public int this[int index] => _values[index];

        public int Count => _values.Length;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>) _values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    private sealed class YieldOnlyEnumerable : IEnumerable
    {
        private readonly int[] _values;

        public YieldOnlyEnumerable(params int[] values) => _values = values;

        public IEnumerator GetEnumerator()
        {
            foreach (var value in _values)
            {
                yield return value;
            }
        }
    }
}
