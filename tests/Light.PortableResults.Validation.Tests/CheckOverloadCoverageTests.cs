using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CheckOverloadCoverageTests
{
    [Fact]
    public void IsEmpty_ShouldAddError_WhenStringIsNotEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check<string?>("abc", target: "emptyString", displayName: "Empty string").IsEmpty();

        context.Errors.Should().ContainSingle(error => error.Target == "emptyString" && error.Code == "Empty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenStringIsEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("", target: "requiredString", displayName: "Required string")
           .IsNotEmpty(new ErrorOverrides { Code = "RequiredString" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "requiredString" && error.Code == "RequiredString");
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenGuidIsNotEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(Guid.NewGuid(), target: "emptyGuid", displayName: "Empty guid")
           .IsEmpty(new ErrorOverrides { Message = "Guid must be empty" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "emptyGuid" && error.Message == "Guid must be empty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenGuidIsEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(Guid.Empty, target: "requiredGuid", displayName: "Required guid").IsNotEmpty();

        context.Errors.Should().ContainSingle(error => error.Target == "requiredGuid" && error.Code == "NotEmpty");
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenCollectionIsNotEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([1], target: "emptyCollection", displayName: "Empty collection")
           .IsEmpty(new ErrorOverrides { Code = "CollectionMustBeEmpty" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "emptyCollection" && error.Code == "CollectionMustBeEmpty"
        );
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenCollectionIsEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([], target: "requiredCollection", displayName: "Required collection")
           .IsNotEmpty();

        context.Errors.Should()
           .ContainSingle(error => error.Target == "requiredCollection" && error.Code == "NotEmpty");
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenImmutableArrayIsNotEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1), target: "emptyImmutable", displayName: "Empty immutable")
           .IsEmpty(new ErrorOverrides { Code = "ImmutableEmpty" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "emptyImmutable" && error.Code == "ImmutableEmpty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenImmutableArrayIsEmpty()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray<int>.Empty, target: "requiredImmutable", displayName: "Required immutable")
           .IsNotEmpty();

        context.Errors.Should().ContainSingle(error => error.Target == "requiredImmutable" && error.Code == "NotEmpty");
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenStringCountIsIncorrect()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("ab", target: "exactStringCount", displayName: "Exact string count")
           .HasCount(3, new ErrorOverrides { Code = "ExactStringCount" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "exactStringCount" && error.Code == "ExactStringCount");
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenStringCountIsTooLow()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check<string?>("ab", target: "minStringCount", displayName: "Min string count").HasMinCount(3);

        context.Errors.Should().ContainSingle(error => error.Target == "minStringCount" && error.Code == "MinCount");
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenStringCountIsTooHigh()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("abcd", target: "maxStringCount", displayName: "Max string count")
           .HasMaxCount(3, new ErrorOverrides { Message = "Too many string characters" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "maxStringCount" && error.Message == "Too many string characters"
        );
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenCollectionCountIsIncorrect()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([1], target: "exactCollectionCount", displayName: "Exact collection count")
           .HasCount(2);

        context.Errors.Should().ContainSingle(error => error.Target == "exactCollectionCount" && error.Code == "Count");
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenCollectionCountIsTooLow()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([1], target: "minCollectionCount", displayName: "Min collection count")
           .HasMinCount(2, new ErrorOverrides { Code = "MinCollectionCount" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "minCollectionCount" && error.Code == "MinCollectionCount"
        );
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenCollectionCountIsTooHigh()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([1, 2, 3], target: "maxCollectionCount", displayName: "Max collection count")
           .HasMaxCount(2);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "maxCollectionCount" && error.Code == "MaxCount");
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenImmutableArrayCountIsIncorrect()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1), target: "exactImmutableCount", displayName: "Exact immutable count")
           .HasCount(2, new ErrorOverrides { Code = "ExactImmutableCount" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "exactImmutableCount" && error.Code == "ExactImmutableCount"
        );
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenImmutableArrayCountIsTooLow()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1), target: "minImmutableCount", displayName: "Min immutable count")
           .HasMinCount(2);

        context.Errors.Should().ContainSingle(error => error.Target == "minImmutableCount" && error.Code == "MinCount");
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenImmutableArrayCountIsTooHigh()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1, 2, 3), target: "maxImmutableCount", displayName: "Max immutable count")
           .HasMaxCount(2, new ErrorOverrides { Code = "MaxImmutableCount" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "maxImmutableCount" && error.Code == "MaxImmutableCount");
    }

    [Fact]
    public void IsGreaterThan_ShouldAddError_WhenValueIsSmaller()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(1, target: "greaterThan", displayName: "Greater than")
           .IsGreaterThan(2, new ErrorOverrides { Code = "GreaterThanCustom" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "greaterThan" && error.Code == "GreaterThanCustom");
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_ShouldAddError_WhenValueIsSmaller()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(1, target: "greaterThanOrEqual", displayName: "Greater than or equal")
           .IsGreaterThanOrEqualTo(2);

        context.Errors.Should().ContainSingle(
            error => error.Target == "greaterThanOrEqual" && error.Code == "GreaterThanOrEqualTo"
        );
    }

    [Fact]
    public void IsLessThan_ShouldAddError_WhenValueIsGreater()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(2, target: "lessThan", displayName: "Less than")
           .IsLessThan(1, new ErrorOverrides { Message = "Value must be lower" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "lessThan" && error.Message == "Value must be lower");
    }

    [Fact]
    public void IsLessThanOrEqualTo_ShouldAddError_WhenValueIsGreater()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(2, target: "lessThanOrEqual", displayName: "Less than or equal")
           .IsLessThanOrEqualTo(1);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "lessThanOrEqual" && error.Code == "LessThanOrEqualTo");
    }

    [Fact]
    public void IsIn_ShouldAddError_WhenValueIsOutsideRange()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(1, target: "inRange", displayName: "In range")
           .IsIn(2, 3, new ErrorOverrides { Code = "InRangeCustom" });

        context.Errors.Should().ContainSingle(error => error.Target == "inRange" && error.Code == "InRangeCustom");
    }

    [Fact]
    public void IsNotIn_ShouldAddError_WhenValueIsInsideRange()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(2, target: "notInRange", displayName: "Not in range").IsNotIn(1, 3);

        context.Errors.Should().ContainSingle(error => error.Target == "notInRange" && error.Code == "NotIn");
    }

    [Fact]
    public void IsInExclusiveRange_ShouldAddError_WhenValueIsAtBoundary()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(1, target: "exclusiveRange", displayName: "Exclusive range")
           .IsInExclusiveRange(1, 3, new ErrorOverrides { Code = "ExclusiveRangeCustom" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "exclusiveRange" && error.Code == "ExclusiveRangeCustom");
    }

    [Fact]
    public void IsInEnum_ShouldAddError_WhenEnumValueIsInvalid()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check((OrderStatus) 99, target: "enumValue", displayName: "Enum value")
           .IsInEnum(new ErrorOverrides { Code = "EnumValueInvalid" });

        context.Errors.Should().ContainSingle(error => error.Target == "enumValue" && error.Code == "EnumValueInvalid");
    }

    [Fact]
    public void IsInEnum_ShouldAddError_WhenEnumValueIsInvalid_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check((OrderStatus) 99, target: "enumValueDefault", displayName: "Enum value default").IsInEnum();

        context.Errors.Should().ContainSingle(error => error.Target == "enumValueDefault" && error.Code == "Enum");
    }

    [Fact]
    public void IsInEnum_ShouldAddError_WhenNullableEnumValueIsInvalid()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        OrderStatus? nullableStatus = (OrderStatus) 99;

        context.Check(nullableStatus, target: "nullableEnumValue", displayName: "Nullable enum value").IsInEnum();

        context.Errors.Should().ContainSingle(error => error.Target == "nullableEnumValue" && error.Code == "Enum");
    }

    [Fact]
    public void IsInEnum_ShouldAddError_WhenNullableEnumValueIsInvalid_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        OrderStatus? nullableStatus = (OrderStatus) 99;

        context
           .Check(nullableStatus, target: "nullableEnumValueOverride", displayName: "Nullable enum value override")
           .IsInEnum(new ErrorOverrides { Code = "NullableEnumInvalid" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "nullableEnumValueOverride" && error.Code == "NullableEnumInvalid"
        );
    }

    [Fact]
    public void IsEnumName_ShouldAddError_WhenEnumNameIsUnknown()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("unknown", target: "enumName", displayName: "Enum name")
           .IsEnumName<OrderStatus>(ignoreCase: true);

        context.Errors.Should().ContainSingle(error => error.Target == "enumName" && error.Code == "EnumName");
    }

    [Fact]
    public void IsEnumName_ShouldAddError_WhenEnumNameIsUnknown_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("unknown", target: "enumNameOverride", displayName: "Enum name override")
           .IsEnumName<OrderStatus>(new ErrorOverrides { Code = "EnumNameInvalid" }, ignoreCase: false);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "enumNameOverride" && error.Code == "EnumNameInvalid");
    }

    [Fact]
    public void IsLessThan_ShouldAddError_WhenValueIsGreater_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(2, target: "lessThanDefault", displayName: "Less than default").IsLessThan(1);

        context.Errors.Should().ContainSingle(error => error.Target == "lessThanDefault" && error.Code == "LessThan");
    }

    [Fact]
    public void IsLessThanOrEqualTo_ShouldAddError_WhenValueIsGreater_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(2, target: "lessThanOrEqualOverride", displayName: "Less than or equal override")
           .IsLessThanOrEqualTo(1, new ErrorOverrides { Code = "LessThanOrEqualOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "lessThanOrEqualOverride" && error.Code == "LessThanOrEqualOverride"
        );
    }

    [Fact]
    public void IsIn_ShouldAddError_WhenValueIsOutsideRange_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(1, target: "inRangeDefault", displayName: "In range default").IsIn(2, 3);

        context.Errors.Should().ContainSingle(error => error.Target == "inRangeDefault" && error.Code == "IsIn");
    }

    [Fact]
    public void IsNotIn_ShouldAddError_WhenValueIsInsideRange_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(2, target: "notInRangeOverride", displayName: "Not in range override")
           .IsNotIn(1, 3, new ErrorOverrides { Code = "NotInRangeOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "notInRangeOverride" && error.Code == "NotInRangeOverride"
        );
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldAddError_WhenDecimalPrecisionIsIncorrect()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(123.45m, target: "decimalValueDefault", displayName: "Decimal value default")
           .HasPrecisionAndScale(4, 1);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "decimalValueDefault" && error.Code == "PrecisionScale");
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldAddError_WhenDecimalPrecisionIsIncorrect_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(123.45m, target: "decimalValue", displayName: "Decimal value")
           .HasPrecisionAndScale(4, 1, new ErrorOverrides { Code = "DecimalInvalid" }, ignoreTrailingZeros: false);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "decimalValue" && error.Code == "DecimalInvalid");
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldAddError_WhenNullableDecimalPrecisionIsIncorrect()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        decimal? nullableAmount = 123.45m;

        context
           .Check(nullableAmount, target: "nullableDecimalValue", displayName: "Nullable decimal value")
           .HasPrecisionAndScale(4, 1);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "nullableDecimalValue" && error.Code == "PrecisionScale");
    }

    [Fact]
    public void Must_ShouldAddError_WhenPredicateFails()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("A", target: "mustOverride", displayName: "Must override")
           .Must(static value => value.Length > 1, new ErrorOverrides { Code = "MustOverride" });

        context.Errors.Should().ContainSingle(error => error.Target == "mustOverride" && error.Code == "MustOverride");
    }

    [Fact]
    public void Must_ShouldAddError_WhenContextAwarePredicateFails()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("A", target: "mustContext", displayName: "Must context")
           .Must(static (readOnlyContext, value) => readOnlyContext.TargetPrefix == "unexpected" && value.Length > 2);

        context.Errors.Should().ContainSingle(error => error.Target == "mustContext" && error.Code == "Predicate");
    }

    [Fact]
    public void Must_ShouldAddError_WhenContextAwarePredicateFails_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("A", target: "mustContextOverride", displayName: "Must context override")
           .Must(
                static (readOnlyContext, value) => readOnlyContext.TargetPrefix == "unexpected" && value.Length > 2,
                new ErrorOverrides { Code = "MustContextOverride" }
            );

        context.Errors.Should().ContainSingle(
            error => error.Target == "mustContextOverride" && error.Code == "MustContextOverride"
        );
    }

    [Fact]
    public void Must_ShouldAddError_WhenPredicateFails_WithDefinition()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("A", target: "mustDefinition", displayName: "Must definition")
           .Must(static value => value.Length > 1, BuiltInValidationErrorDefinitions.Predicate);

        context.Errors.Should().ContainSingle(error => error.Target == "mustDefinition" && error.Code == "Predicate");
    }

    [Fact]
    public void Must_ShouldAddError_WhenPredicateFails_WithTemplateAndMetadata()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var metadata = MetadataObject.Create(("source", "predicate"));
        var template = new ValidationErrorTemplates.Constant("Predicate template failure");

        context
           .Check("A", target: "mustTemplate", displayName: "Must template")
           .Must(
                static value => value.Length > 1,
                template,
                code: "MustTemplate",
                metadata: metadata
            );

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "mustTemplate" &&
                error.Code == "MustTemplate" &&
                error.Metadata == metadata &&
                error.Message == "Predicate template failure"
        );
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenStringIsNotEmpty_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("abc", target: "emptyStringOverride", displayName: "Empty string override")
           .IsEmpty(new ErrorOverrides { Code = "EmptyStringOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "emptyStringOverride" && error.Code == "EmptyStringOverride"
        );
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenStringIsEmpty_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check<string?>("", target: "notEmptyStringDefault", displayName: "Not empty string default")
           .IsNotEmpty();

        context.Errors.Should()
           .ContainSingle(error => error.Target == "notEmptyStringDefault" && error.Code == "NotEmpty");
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenGuidIsNotEmpty_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check(Guid.NewGuid(), target: "emptyGuidDefault", displayName: "Empty guid default").IsEmpty();

        context.Errors.Should().ContainSingle(error => error.Target == "emptyGuidDefault" && error.Code == "Empty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenGuidIsEmpty_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(Guid.Empty, target: "notEmptyGuidOverride", displayName: "Not empty guid override")
           .IsNotEmpty(new ErrorOverrides { Code = "GuidRequired" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "notEmptyGuidOverride" && error.Code == "GuidRequired");
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenCollectionIsNotEmpty_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>([1], target: "emptyCollectionDefault", displayName: "Empty collection default")
           .IsEmpty();

        context.Errors.Should()
           .ContainSingle(error => error.Target == "emptyCollectionDefault" && error.Code == "Empty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenCollectionIsEmpty_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>(
                [],
                target: "notEmptyCollectionOverride",
                displayName: "Not empty collection override"
            )
           .IsNotEmpty(new ErrorOverrides { Code = "CollectionRequired" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "notEmptyCollectionOverride" && error.Code == "CollectionRequired"
        );
    }

    [Fact]
    public void IsEmpty_ShouldAddError_WhenImmutableArrayIsNotEmpty_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(ImmutableArray.Create(1), target: "emptyImmutableDefault", displayName: "Empty immutable default")
           .IsEmpty();

        context.Errors.Should()
           .ContainSingle(error => error.Target == "emptyImmutableDefault" && error.Code == "Empty");
    }

    [Fact]
    public void IsNotEmpty_ShouldAddError_WhenImmutableArrayIsEmpty_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(
                ImmutableArray<int>.Empty,
                target: "notEmptyImmutableOverride",
                displayName: "Not empty immutable override"
            )
           .IsNotEmpty(new ErrorOverrides { Code = "ImmutableRequired" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "notEmptyImmutableOverride" && error.Code == "ImmutableRequired"
        );
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenStringCountIsTooLow_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("ab", target: "minStringCountOverride", displayName: "Min string count override")
           .HasMinCount(3, new ErrorOverrides { Code = "MinStringCountOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "minStringCountOverride" && error.Code == "MinStringCountOverride"
        );
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenStringCountIsTooHigh_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("abcd", target: "maxStringCountDefault", displayName: "Max string count default")
           .HasMaxCount(3);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "maxStringCountDefault" && error.Code == "MaxCount");
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenCollectionCountIsIncorrect_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>(
                [1],
                target: "exactCollectionCountOverride",
                displayName: "Exact collection count override"
            )
           .HasCount(2, new ErrorOverrides { Code = "ExactCollectionCountOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "exactCollectionCountOverride" && error.Code == "ExactCollectionCountOverride"
        );
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenCollectionCountIsTooHigh_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<IEnumerable<int>>(
                [1, 2, 3],
                target: "maxCollectionCountOverride",
                displayName: "Max collection count override"
            )
           .HasMaxCount(2, new ErrorOverrides { Code = "MaxCollectionCountOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "maxCollectionCountOverride" && error.Code == "MaxCollectionCountOverride"
        );
    }

    [Fact]
    public void HasCount_ShouldAddError_WhenImmutableArrayCountIsIncorrect_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(
                ImmutableArray.Create(1),
                target: "exactImmutableCountDefault",
                displayName: "Exact immutable count default"
            )
           .HasCount(2);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "exactImmutableCountDefault" && error.Code == "Count");
    }

    [Fact]
    public void HasMinCount_ShouldAddError_WhenImmutableArrayCountIsTooLow_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(
                ImmutableArray.Create(1),
                target: "minImmutableCountOverride",
                displayName: "Min immutable count override"
            )
           .HasMinCount(2, new ErrorOverrides { Code = "MinImmutableCountOverride" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "minImmutableCountOverride" && error.Code == "MinImmutableCountOverride"
        );
    }

    [Fact]
    public void HasMaxCount_ShouldAddError_WhenImmutableArrayCountIsTooHigh_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(
                ImmutableArray.Create(1, 2, 3),
                target: "maxImmutableCountDefault",
                displayName: "Max immutable count default"
            )
           .HasMaxCount(2);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "maxImmutableCountDefault" && error.Code == "MaxCount");
    }

    [Fact]
    public void IsEqualTo_ShouldAddError_WhenValuesAreNotEqual_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check("abc", target: "equalToDefault", displayName: "Equal to default").IsEqualTo("xyz");

        context.Errors.Should().ContainSingle(error => error.Target == "equalToDefault" && error.Code == "EqualTo");
    }

    [Fact]
    public void IsNotEqualTo_ShouldAddError_WhenValuesAreEqual_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check("same", target: "notEqualToDefault", displayName: "Not equal default").IsNotEqualTo("same");

        context.Errors.Should()
           .ContainSingle(error => error.Target == "notEqualToDefault" && error.Code == "NotEqualTo");
    }

    [Fact]
    public void IsNotEqualTo_ShouldAddError_WhenValuesAreEqual_WithComparer()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("ABC", target: "notEqualToComparer", displayName: "Not equal comparer")
           .IsNotEqualTo("abc", StringComparer.OrdinalIgnoreCase);

        context.Errors.Should()
           .ContainSingle(error => error.Target == "notEqualToComparer" && error.Code == "NotEqualTo");
    }

    [Fact]
    public void IsNotEqualTo_ShouldAddError_WhenValuesAreEqual_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("same", target: "notEqualToOverride", displayName: "Not equal override")
           .IsNotEqualTo("same", new ErrorOverrides { Code = "NotEqualOverride" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "notEqualToOverride" && error.Code == "NotEqualOverride");
    }

    [Fact]
    public void HasMinLength_ShouldAddError_WhenStringIsTooShort_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("ab", target: "minLengthOverride", displayName: "Min length override")
           .HasMinLength(3, new ErrorOverrides { Code = "MinLengthOverride" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "minLengthOverride" && error.Code == "MinLengthOverride");
    }

    [Fact]
    public void HasMaxLength_ShouldAddError_WhenStringIsTooLong_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check<string?>("abcd", target: "maxLengthDefault", displayName: "Max length default").HasMaxLength(3);

        context.Errors.Should().ContainSingle(error => error.Target == "maxLengthDefault" && error.Code == "MaxLength");
    }

    [Fact]
    public void HasLengthIn_ShouldAddError_WhenStringLengthIsOutsideRange_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("a", target: "lengthInOverride", displayName: "Length in override")
           .HasLengthIn(2, 3, new ErrorOverrides { Code = "LengthInOverride" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "lengthInOverride" && error.Code == "LengthInOverride");
    }

    [Fact]
    public void Matches_ShouldAddError_WhenStringDoesNotMatchRegex_DefaultOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check("12A", target: "regexDefault", displayName: "Regex default").Matches(new Regex("^\\d+$"));

        context.Errors.Should().ContainSingle(error => error.Target == "regexDefault" && error.Code == "Matches");
    }

    [Fact]
    public void IsEmail_ShouldAddError_WhenStringIsNotAnEmail_WithOverrides()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("not-an-email", target: "emailOverride", displayName: "Email override")
           .IsEmail(new ErrorOverrides { Code = "EmailOverride" });

        context.Errors.Should()
           .ContainSingle(error => error.Target == "emailOverride" && error.Code == "EmailOverride");
    }

    [Fact]
    public void SuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        OrderStatus? nullableStatus = OrderStatus.Pending;
        decimal? nullableAmount = 12.30m;

        context.Check<string?>("abc", target: "exactString", displayName: "Exact string").HasCount(3);
        context
           .Check<IEnumerable<int>>([1, 2, 3], target: "exactCollection", displayName: "Exact collection")
           .HasCount(3, new ErrorOverrides { Code = "Unused" });
        context
           .Check<IEnumerable<int>>([1, 2, 3], target: "minCollection", displayName: "Min collection")
           .HasMinCount(2);
        context
           .Check<IEnumerable<int>>([1, 2, 3], target: "maxCollection", displayName: "Max collection")
           .HasMaxCount(3, new ErrorOverrides { Code = "Unused" });
        context
           .Check(ImmutableArray.Create(1, 2, 3), target: "exactImmutable", displayName: "Exact immutable")
           .HasCount(3);
        context
           .Check(ImmutableArray.Create(1, 2, 3), target: "minImmutable", displayName: "Min immutable")
           .HasMinCount(2, new ErrorOverrides { Code = "Unused" });
        context
           .Check(ImmutableArray.Create(1, 2, 3), target: "maxImmutable", displayName: "Max immutable")
           .HasMaxCount(3);

        context.Check(OrderStatus.Pending, target: "enumValue", displayName: "Enum value").IsInEnum();
        context
           .Check(nullableStatus, target: "nullableEnumValue", displayName: "Nullable enum value")
           .IsInEnum();
        context
           .Check(12.30m, target: "decimalValue", displayName: "Decimal value")
           .HasPrecisionAndScale(4, 2, ignoreTrailingZeros: true);
        context
           .Check(nullableAmount, target: "nullableDecimalValue", displayName: "Nullable decimal value")
           .HasPrecisionAndScale(4, 2, new ErrorOverrides { Code = "Unused" }, ignoreTrailingZeros: true);

        context.Check("1234", target: "pattern", displayName: "Pattern").Matches("^\\d+$");
        context
           .Check("1234", target: "patternOverride", displayName: "Pattern override")
           .Matches("^\\d+$", new ErrorOverrides { Code = "Unused" });
        context.Check("user@example.com", target: "email", displayName: "Email").IsEmail();

        context.Check(3, target: "greaterThan", displayName: "Greater than").IsGreaterThan(2);
        context.Check(3, target: "inRange", displayName: "In range").IsIn(1, 5);
        context.Check<string?>("", target: "emptyString", displayName: "Empty string").IsEmpty();
        context
           .Check<IEnumerable<int>>([], target: "emptyCollection", displayName: "Empty collection")
           .IsEmpty();
        context
           .Check<IEnumerable<int>>([1], target: "notEmptyCollection", displayName: "Not empty collection")
           .IsNotEmpty();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ShortCircuit_ShouldSucceed()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(2, target: "shortCircuit", displayName: "Short circuit")
           .ShortCircuit()
           .IsGreaterThan(3)
           .IsShortCircuited
           .Should()
           .BeTrue();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void GuardedSuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("abc", target: "equal", displayName: "Equal")
           .IsEqualTo("ABC", StringComparer.OrdinalIgnoreCase, new ErrorOverrides { Code = "Unused" });
        context
           .Check("abc", target: "notEqual", displayName: "Not equal")
           .IsNotEqualTo("xyz", StringComparer.OrdinalIgnoreCase);
        context
           .Check<string?>("abcd", target: "maxLength", displayName: "Max length")
           .HasMaxLength(4, new ErrorOverrides { Code = "Unused" });
        context
           .Check("ab", target: "minLength", displayName: "Min length")
           .HasMinLength(2, new ErrorOverrides { Code = "Unused" });
        context.Check("abc", target: "lengthIn", displayName: "Length in").HasLengthIn(2, 4);
        context.Check("123", target: "regex", displayName: "Regex").Matches(new Regex("^\\d+$"));

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEqualTo_ShouldThrow_WhenEqualityComparerIsNull()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        Action act = () => context.Check("abc").IsEqualTo("abc", (IEqualityComparer<string>) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("equalityComparer");
    }

    [Fact]
    public void IsNotEqualTo_ShouldThrow_WhenEqualityComparerIsNull()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        Action act = () => context.Check("abc").IsNotEqualTo("abc", (IEqualityComparer<string>) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("equalityComparer");
    }

    [Fact]
    public void Matches_ShouldThrow_WhenRegexIsNull()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        Action act = () => context.Check("123").Matches((Regex) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("regex");
    }

    [Fact]
    public void Matches_ShouldThrow_WhenPatternIsNull()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        Action act = () => context.Check("123").Matches((string) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("pattern");
    }

    [Fact]
    public void WrapperSuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        OrderStatus? nullableStatus = OrderStatus.Approved;

        context.Check(3, target: "greaterThan", displayName: "Greater than").IsGreaterThan(2);
        context
           .Check(3, target: "greaterThanOverride", displayName: "Greater than override")
           .IsGreaterThan(2, new ErrorOverrides { Code = "Unused" });
        context.Check(3, target: "greaterThanOrEqual", displayName: "Greater than or equal").IsGreaterThanOrEqualTo(3);
        context.Check(2, target: "lessThan", displayName: "Less than").IsLessThan(3);
        context
           .Check(2, target: "lessThanOverride", displayName: "Less than override")
           .IsLessThan(3, new ErrorOverrides { Code = "Unused" });
        context.Check(2, target: "lessThanOrEqual", displayName: "Less than or equal").IsLessThanOrEqualTo(2);
        context
           .Check(2, target: "lessThanOrEqualOverride", displayName: "Less than or equal override")
           .IsLessThanOrEqualTo(2, new ErrorOverrides { Code = "Unused" });
        context.Check(2, target: "notIn", displayName: "Not in").IsNotIn(3, 5);
        context.Check(3, target: "exclusiveRange", displayName: "Exclusive range").IsInExclusiveRange(2, 4);

        context
           .Check<string?>("abc", target: "exactCountOverride", displayName: "Exact count override")
           .HasCount(3, new ErrorOverrides { Code = "Unused" });
        context.Check<string?>("abc", target: "minCount", displayName: "Min count").HasMinCount(2);
        context
           .Check<string?>("abc", target: "minCountOverride", displayName: "Min count override")
           .HasMinCount(2, new ErrorOverrides { Code = "Unused" });
        context.Check<string?>("abc", target: "maxCount", displayName: "Max count").HasMaxCount(3);
        context
           .Check<string?>("abc", target: "maxCountOverride", displayName: "Max count override")
           .HasMaxCount(3, new ErrorOverrides { Code = "Unused" });
        context
           .Check<IEnumerable<int>>([1, 2], target: "collectionCount", displayName: "Collection count")
           .HasCount(2);
        context
           .Check<IEnumerable<int>>(
                [1, 2],
                target: "collectionMinCountOverride",
                displayName: "Collection min count override"
            )
           .HasMinCount(2, new ErrorOverrides { Code = "Unused" });
        context
           .Check<IEnumerable<int>>([1, 2], target: "collectionMaxCount", displayName: "Collection max count")
           .HasMaxCount(2);
        context
           .Check(ImmutableArray.Create(1, 2), target: "immutableMinCount", displayName: "Immutable min count")
           .HasMinCount(2);

        context
           .Check(OrderStatus.Pending, target: "enumOverride", displayName: "Enum override")
           .IsInEnum(new ErrorOverrides { Code = "Unused" });
        context
           .Check(nullableStatus, target: "nullableEnumOverride", displayName: "Nullable enum override")
           .IsInEnum(new ErrorOverrides { Code = "Unused" });
        context.Check<string?>("Approved", target: "enumName", displayName: "Enum name").IsEnumName<OrderStatus>();
        context
           .Check<string?>("approved", target: "enumNameOverride", displayName: "Enum name override")
           .IsEnumName<OrderStatus>(new ErrorOverrides { Code = "Unused" }, ignoreCase: true);

        context
           .Check("abc", target: "equalOverride", displayName: "Equal override")
           .IsEqualTo("ABC", StringComparer.OrdinalIgnoreCase, new ErrorOverrides { Code = "Unused" });
        context
           .Check("abc", target: "notEqualOverride", displayName: "Not equal override")
           .IsNotEqualTo("xyz", StringComparer.OrdinalIgnoreCase, new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ComplexShortCircuit_ShouldSucceed()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.Check("abc", target: "shortCircuitedText", displayName: "Short circuited text")
           .ShortCircuit()
           .IsNotNullOrWhiteSpace()
           .HasMinLength(10)
           .Matches(new Regex("^\\d+$"))
           .IsEmail()
           .IsShortCircuited
           .Should()
           .BeTrue();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ComparableAndRangeSuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check(5, target: "inRangeOverride", displayName: "In range override")
           .IsIn(1, 5, new ErrorOverrides { Code = "Unused" });
        context
           .Check(0, target: "notInRangeOverride", displayName: "Not in range override")
           .IsNotIn(1, 5, new ErrorOverrides { Code = "Unused" });
        context
           .Check(3, target: "exclusiveRangeOverride", displayName: "Exclusive range override")
           .IsInExclusiveRange(1, 5, new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void StringGuardsSuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check("A1", target: "requiredTextOverride", displayName: "Required text override")
           .IsNotNullOrWhiteSpace(new ErrorOverrides { Code = "Unused" });
        context
           .Check<string?>("abc", target: "maxLengthDefaultSuccess", displayName: "Max length default success")
           .HasMaxLength(3);
        context
           .Check<string?>("abc", target: "maxLengthOverrideSuccess", displayName: "Max length override success")
           .HasMaxLength(3, new ErrorOverrides { Code = "Unused" });
        context
           .Check("abc", target: "lengthInOverrideSuccess", displayName: "Length in override success")
           .HasLengthIn(2, 4, new ErrorOverrides { Code = "Unused" });
        context
           .Check("123", target: "regexOverrideSuccess", displayName: "Regex override success")
           .Matches(new Regex("^\\d+$"), new ErrorOverrides { Code = "Unused" });
        context
           .Check("123", target: "patternOverrideSuccess", displayName: "Pattern override success")
           .Matches("^\\d+$", new ErrorOverrides { Code = "Unused" });
        context
           .Check("123", target: "digitsOverrideSuccess", displayName: "Digits override success")
           .ContainsOnlyDigits(new ErrorOverrides { Code = "Unused" });
        context
           .Check("A1", target: "lettersDigitsDefaultSuccess", displayName: "Letters digits default success")
           .ContainsOnlyLettersAndDigits();
        context
           .Check("A1", target: "lettersDigitsOverrideSuccess", displayName: "Letters digits override success")
           .ContainsOnlyLettersAndDigits(new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void EnumNameSuccessPaths_ShouldNotAddErrors()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context
           .Check<string?>("Approved", target: "enumNameCaseSensitive", displayName: "Enum name case sensitive")
           .IsEnumName<OrderStatus>();
        context
           .Check<string?>("approved", target: "enumNameOverrideCaseInsensitive", displayName: "Enum name override")
           .IsEnumName<OrderStatus>(new ErrorOverrides { Code = "Unused" }, ignoreCase: true);

        context.HasErrors.Should().BeFalse();
    }

    private enum OrderStatus
    {
        Pending,
        Approved
    }
}
