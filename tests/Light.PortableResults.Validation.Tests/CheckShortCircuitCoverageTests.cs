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
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CheckShortCircuitCoverageTests
{
    [Fact]
    public void IsNull_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>("value", NoOpValueNormalizer.Instance, target: "value").ShortCircuit();

        check.IsNull().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNull_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>("value", NoOpValueNormalizer.Instance, target: "value").ShortCircuit();

        check.IsNull(new ErrorOverrides { Code = "UnusedNull" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotNull_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "nullableValue").ShortCircuit();

        check.IsNotNull().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotNull_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "nullableValue").ShortCircuit();

        check.IsNotNull(new ErrorOverrides { Code = "UnusedNotNull" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyString_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>("value", NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyString_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>("value", NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsEmpty(new ErrorOverrides { Code = "UnusedEmpty" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyString_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(string.Empty, NoOpValueNormalizer.Instance, target: "emptyText")
           .ShortCircuit();

        check.IsNotEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyString_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(string.Empty, NoOpValueNormalizer.Instance, target: "emptyText")
           .ShortCircuit();

        check.IsNotEmpty(new ErrorOverrides { Code = "UnusedNotEmpty" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyGuid_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(Guid.NewGuid(), target: "guid").ShortCircuit();

        check.IsEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyGuid_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(Guid.NewGuid(), target: "guid").ShortCircuit();

        check.IsEmpty(new ErrorOverrides { Code = "UnusedEmptyGuid" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyGuid_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(Guid.Empty, target: "emptyGuid").ShortCircuit();

        check.IsNotEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyGuid_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(Guid.Empty, target: "emptyGuid").ShortCircuit();

        check.IsNotEmpty(new ErrorOverrides { Code = "UnusedNotEmptyGuid" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyCollection_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        IEnumerable nonEmptyItems = new[] { 1 };
        var check = context.Check(nonEmptyItems, target: "items").ShortCircuit();

        check.IsEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyCollection_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        IEnumerable nonEmptyItems = new[] { 1 };
        var check = context.Check(nonEmptyItems, target: "items").ShortCircuit();

        check.IsEmpty(new ErrorOverrides { Code = "UnusedEmptyCollection" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyCollection_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        IEnumerable emptyItems = Array.Empty<int>();
        var check = context.Check(emptyItems).ShortCircuit();

        check.IsNotEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyCollection_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        IEnumerable emptyItems = Array.Empty<int>();
        var check = context.Check(emptyItems).ShortCircuit();

        check.IsNotEmpty(new ErrorOverrides { Code = "UnusedNotEmptyCollection" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyImmutable_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1), target: "immutable").ShortCircuit();

        check.IsEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyImmutable_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1), target: "immutable").ShortCircuit();

        check.IsEmpty(new ErrorOverrides { Code = "UnusedEmptyImmutable" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyImmutable_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray<int>.Empty, target: "emptyImmutable").ShortCircuit();

        check.IsNotEmpty().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyImmutable_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray<int>.Empty, target: "emptyImmutable").ShortCircuit();

        check.IsNotEmpty(new ErrorOverrides { Code = "UnusedNotEmptyImmutable" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEqualTo_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsEqualTo("expected").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEqualTo_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsEqualTo("expected", new ErrorOverrides { Code = "UnusedEqual" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEqualTo_ShouldRespectShortCircuit_WhenComparerIsUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsEqualTo("expected", StringComparer.OrdinalIgnoreCase).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEqualTo_ShouldRespectShortCircuit_WhenComparerAndOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsEqualTo(
                "expected",
                StringComparer.OrdinalIgnoreCase,
                new ErrorOverrides { Code = "UnusedEqualComparer" }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsNotEqualTo_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsNotEqualTo("forbidden").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEqualTo_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsNotEqualTo("forbidden", new ErrorOverrides { Code = "UnusedNotEqual" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void IsNotEqualTo_ShouldRespectShortCircuit_WhenComparerIsUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsNotEqualTo("forbidden", StringComparer.OrdinalIgnoreCase).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEqualTo_ShouldRespectShortCircuit_WhenComparerAndOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "name").ShortCircuit();

        check.IsNotEqualTo(
                "forbidden",
                StringComparer.OrdinalIgnoreCase,
                new ErrorOverrides { Code = "UnusedNotEqualComparer" }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsGreaterThan_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsGreaterThan("A").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThan_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsGreaterThan("A", new ErrorOverrides { Code = "UnusedGreater" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsGreaterThanOrEqualTo("A").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThanOrEqualTo_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsGreaterThanOrEqualTo("A", new ErrorOverrides { Code = "UnusedGreaterOrEqual" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsLessThan_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsLessThan("Z").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsLessThan_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsLessThan("Z", new ErrorOverrides { Code = "UnusedLess" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsLessThanOrEqualTo_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsLessThanOrEqualTo("Z").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsLessThanOrEqualTo_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsLessThanOrEqualTo("Z", new ErrorOverrides { Code = "UnusedLessOrEqual" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsIn_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsInRange("A", "Z").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsIn_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsInRange("A", "Z", new ErrorOverrides { Code = "UnusedIn" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotIn_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsNotInRange("A", "Z").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotIn_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsNotInRange("A", "Z", new ErrorOverrides { Code = "UnusedNotIn" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsInExclusiveRange_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsInExclusiveRange("A", "Z").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsInExclusiveRange_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "code").ShortCircuit();

        check.IsInExclusiveRange("A", "Z", new ErrorOverrides { Code = "UnusedExclusive" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsNotNullOrWhiteSpace().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotNullOrWhiteSpace_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsNotNullOrWhiteSpace(new ErrorOverrides { Code = "UnusedRequired" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasMinLength_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMinLength(5).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasMinLength_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMinLength(5, new ErrorOverrides { Code = "UnusedMinLength" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasMaxLength_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "nullableText").ShortCircuit();

        check.HasMaxLength(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasMaxLength_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "nullableText").ShortCircuit();

        check.HasMaxLength(1, new ErrorOverrides { Code = "UnusedMaxLength" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasLengthIn_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasLengthIn(2, 4).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasLengthIn_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasLengthIn(2, 4, new ErrorOverrides { Code = "UnusedLengthIn" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void MatchesRegex_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();
        var regex = new Regex("^\\d+$");

        check.Matches(regex).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void MatchesRegex_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();
        var regex = new Regex("^\\d+$");

        check.Matches(regex, new ErrorOverrides { Code = "UnusedRegex" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void MatchesPattern_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.Matches("^\\d+$").IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void MatchesPattern_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.Matches("^\\d+$", new ErrorOverrides { Code = "UnusedPattern" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmail_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsEmail().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmail_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.IsEmail(new ErrorOverrides { Code = "UnusedEmail" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ContainsOnlyDigits_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.ContainsOnlyDigits().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ContainsOnlyDigits_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.ContainsOnlyDigits(new ErrorOverrides { Code = "UnusedDigits" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ContainsOnlyLettersAndDigits_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.ContainsOnlyLettersAndDigits().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ContainsOnlyLettersAndDigits_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string>(null!, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.ContainsOnlyLettersAndDigits(new ErrorOverrides { Code = "UnusedLettersDigits" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsInEnum_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check((OrderStatus) 99, target: "status").ShortCircuit();

        check.IsInEnum().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsInEnum_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check((OrderStatus) 99, target: "status").ShortCircuit();

        check.IsInEnum(new ErrorOverrides { Code = "UnusedEnum" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsInEnum_Nullable_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        OrderStatus? nullableStatus = null;
        var check = context.Check(nullableStatus, NoOpValueNormalizer.Instance).ShortCircuit();

        check.IsInEnum().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsInEnum_Nullable_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        OrderStatus? nullableStatus = null;
        var check = context.Check(nullableStatus, NoOpValueNormalizer.Instance).ShortCircuit();

        check.IsInEnum(new ErrorOverrides { Code = "UnusedNullableEnum" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void IsEnumName_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "statusName").ShortCircuit();

        check.IsEnumName<OrderStatus>().IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEnumName_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<string?>(null, NoOpValueNormalizer.Instance, target: "statusName").ShortCircuit();

        check.IsEnumName<OrderStatus>(new ErrorOverrides { Code = "UnusedEnumName" }, ignoreCase: true)
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(123.45m, target: "amount").ShortCircuit();

        check.HasPrecisionAndScale(4, 1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(123.45m, target: "amount").ShortCircuit();

        check.HasPrecisionAndScale(4, 1, new ErrorOverrides { Code = "UnusedDecimal" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void HasPrecisionAndScale_Nullable_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        decimal? nullableAmount = null;
        var check = context.Check(nullableAmount, NoOpValueNormalizer.Instance).ShortCircuit();

        check.HasPrecisionAndScale(4, 1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void HasPrecisionAndScale_Nullable_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        decimal? nullableAmount = null;
        var check = context.Check(nullableAmount, NoOpValueNormalizer.Instance).ShortCircuit();

        check.HasPrecisionAndScale(4, 1, new ErrorOverrides { Code = "UnusedNullableDecimal" })
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void StringHasCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void StringHasCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasCount(1, new ErrorOverrides { Code = "UnusedStringCount" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void StringHasMinCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMinCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void StringHasMinCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMinCount(1, new ErrorOverrides { Code = "UnusedStringMinCount" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void StringHasMaxCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMaxCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void StringHasMaxCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        string? nullableText = null;
        var check = context.Check(nullableText, NoOpValueNormalizer.Instance, target: "text").ShortCircuit();

        check.HasMaxCount(1, new ErrorOverrides { Code = "UnusedStringMaxCount" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void CollectionHasCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void CollectionHasCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasCount(1, new ErrorOverrides { Code = "UnusedCollectionCount" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void CollectionHasMinCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasMinCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void CollectionHasMinCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasMinCount(1, new ErrorOverrides { Code = "UnusedCollectionMinCount" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void CollectionHasMaxCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasMaxCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void CollectionHasMaxCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(null!, NoOpValueNormalizer.Instance, target: "items").ShortCircuit();

        check.HasMaxCount(1, new ErrorOverrides { Code = "UnusedCollectionMaxCount" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasCount(10).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasCount(10, new ErrorOverrides { Code = "UnusedImmutableCount" }).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasMinCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasMinCount(10).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasMinCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasMinCount(10, new ErrorOverrides { Code = "UnusedImmutableMinCount" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasMaxCount_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasMaxCount(1).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void ImmutableArrayHasMaxCount_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check(ImmutableArray.Create(1, 2, 3), target: "immutable").ShortCircuit();

        check.HasMaxCount(1, new ErrorOverrides { Code = "UnusedImmutableMaxCount" }).IsShortCircuited.Should()
           .BeTrue();
    }

    [Fact]
    public void MustPredicate_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var predicateInvoked = false;

        check.Must(
                _ =>
                {
                    predicateInvoked = true;
                    return false;
                }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        predicateInvoked.Should().BeFalse();
    }

    [Fact]
    public void MustPredicate_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var overridePredicateInvoked = false;

        check.Must(
                _ =>
                {
                    overridePredicateInvoked = true;
                    return false;
                },
                new ErrorOverrides { Code = "UnusedPredicate" }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        overridePredicateInvoked.Should().BeFalse();
    }

    [Fact]
    public void MustContextPredicate_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var contextPredicateInvoked = false;

        check.Must(
                (_, _) =>
                {
                    contextPredicateInvoked = true;
                    return false;
                }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        contextPredicateInvoked.Should().BeFalse();
    }

    [Fact]
    public void MustContextPredicate_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var overrideContextPredicateInvoked = false;

        check.Must(
                (_, _) =>
                {
                    overrideContextPredicateInvoked = true;
                    return false;
                },
                new ErrorOverrides { Code = "UnusedContextPredicate" }
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        overrideContextPredicateInvoked.Should().BeFalse();
    }

    [Fact]
    public void MustTemplatePredicate_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var templatePredicateInvoked = false;

        check.Must(
                _ =>
                {
                    templatePredicateInvoked = true;
                    return false;
                },
                new ValidationErrorTemplates.Constant("Unused"),
                code: "UnusedTemplate"
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        templatePredicateInvoked.Should().BeFalse();
    }

    [Fact]
    public void CustomCheck_ShouldRespectShortCircuit()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "code", displayName: "Code").ShortCircuit();
        var customInvoked = false;

        check.Custom(
                (_, _) => customInvoked = true
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        customInvoked.Should().BeFalse();
    }

    [Fact]
    public void IsEmptyString_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check<string?>("abc", target: "emptyText", displayName: "Empty text");

        check.IsEmpty(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyString_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check<string?>(string.Empty, target: "notEmptyText", displayName: "Not empty text");

        check.IsNotEmpty(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsEmptyCollection_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(new[] { 1 }, target: "items", displayName: "Items");

        check.IsEmpty(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsNotEmptyCollection_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check<IEnumerable>(
            Array.Empty<int>(),
            target: "requiredItems",
            displayName: "Required items"
        );

        check.IsNotEmpty(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void IsGreaterThan_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check(1, target: "greaterThan", displayName: "Greater than");

        check.IsGreaterThan(2, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "greaterThan" && error.Code == "GreaterThan");
    }

    [Fact]
    public void IsEqualTo_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "equalTo", displayName: "Equal to");

        check.IsEqualTo("xyz", StringComparer.Ordinal, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "equalTo" && error.Code == "EqualTo");
    }

    [Fact]
    public void IsNotEqualTo_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "notEqualTo", displayName: "Not equal to");

        check.IsNotEqualTo("abc", StringComparer.Ordinal, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "notEqualTo" && error.Code == "NotEqualTo");
    }

    [Fact]
    public void HasMinLength_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("ab", target: "minLength", displayName: "Min length");

        check.HasMinLength(3, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "minLength" && error.Code == "MinLength");
    }

    [Fact]
    public void MatchesRegex_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "regex", displayName: "Regex");
        var regex = new Regex("^\\d+$");

        check.Matches(regex, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "regex" && error.Code == "Pattern");
    }

    [Fact]
    public void MatchesPattern_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "pattern", displayName: "Pattern");

        check.Matches("^\\d+$", shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "pattern" && error.Code == "Pattern");
    }

    [Fact]
    public void IsEmail_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "email", displayName: "Email");

        check.IsEmail(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "email" && error.Code == "Email");
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check(123.45m, target: "amount", displayName: "Amount");

        check.HasPrecisionAndScale(4, 1, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "amount" && error.Code == "PrecisionScale");
    }

    [Fact]
    public void HasPrecisionAndScale_Nullable_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check((decimal?) 123.45m, target: "nullableAmount", displayName: "Nullable amount");

        check.HasPrecisionAndScale(4, 1, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "nullableAmount" && error.Code == "PrecisionScale");
    }

    [Fact]
    public void MustPredicate_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "predicate", displayName: "Predicate");

        check.Must(static _ => false, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().Contain(error => error.Target == "predicate" && error.Code == "Predicate");
    }

    [Fact]
    public void MustPredicate_ShouldShortCircuit_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "overridePredicate", displayName: "Override predicate");

        check.Must(static _ => false, new ErrorOverrides { Code = "OverridePredicate" }, shortCircuitOnError: true)
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void MustContextPredicate_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "contextPredicate", displayName: "Context predicate");

        check.Must(static (_, _) => false, shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
    }

    [Fact]
    public void MustContextPredicate_ShouldShortCircuit_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "overrideContextPredicate", displayName: "Override context predicate");

        check.Must(
                static (_, _) => false,
                new ErrorOverrides { Code = "OverrideContextPredicate" },
                shortCircuitOnError: true
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
    }

    [Fact]
    public void MustTemplatePredicate_ShouldShortCircuit_WhenRequested()
    {
        var context = CreateContext();
        var check = context.Check("AB", target: "templatePredicate", displayName: "Template predicate");

        check.Must(
                static _ => false,
                new ValidationErrorTemplates.Constant("Template predicate invalid"),
                code: "TemplatePredicate",
                shortCircuitOnError: true
            )
           .IsShortCircuited
           .Should()
           .BeTrue();
        context.Errors.Should()
           .Contain(error => error.Target == "templatePredicate" && error.Code == "TemplatePredicate");
    }

    [Fact]
    public void IsEmail_ShouldShortCircuit_WhenMessageOverrideIsUsedAndRequested()
    {
        var context = CreateContext();
        var check = context.Check("abc", target: "emailOverride", displayName: "Email override");

        check.IsEmail(new ErrorOverrides { Message = "Email override is invalid" }, shortCircuitOnError: true)
           .IsShortCircuited
           .Should()
           .BeTrue();
        context.Errors.Should().Contain(
            error => error.Target == "emailOverride" && error.Message == "Email override is invalid"
        );
    }

    [Fact]
    public void HasCount_ShouldWorkWithVariousCollections()
    {
        var context = CreateContext();

        context.Check<IEnumerable>("abc", target: "textCollection", displayName: "Text collection").HasCount(3);
        context.Check<IEnumerable>(new ArrayList { 1, 2 }, target: "arrayList", displayName: "Array list").HasCount(2);
        context.Check<IEnumerable>(
                new ObjectReadOnlyCollection("A", "B"),
                target: "objectCollection",
                displayName: "Object collection"
            )
           .HasCount(2);
        context.Check<IEnumerable>(new YieldOnlyEnumerable(1, 2), target: "yieldOnly", displayName: "Yield only")
           .HasCount(2);

        context.Check<IEnumerable<char>>("abc", target: "textSequence", displayName: "Text sequence").HasCount(3);
        context.Check<IEnumerable<int>>(
                new NumberReadOnlyCollection(1, 2),
                target: "readOnlySequence",
                displayName: "Read-only sequence"
            )
           .HasCount(2);
        context.Check<IEnumerable<int>>(new List<int> { 1, 2 }, target: "listSequence", displayName: "List sequence")
           .HasCount(2);
        context.Check<IEnumerable<int>>(
                new NonGenericCountedEnumerable(1, 2),
                target: "nonGenericSequence",
                displayName: "Non-generic sequence"
            )
           .HasCount(2);
        context.Check<IEnumerable<int>>(
                new YieldOnlyGenericEnumerable(1, 2),
                target: "yieldSequence",
                displayName: "Yield sequence"
            )
           .HasCount(2);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldWorkWithVariousDecimals()
    {
        var context = CreateContext();

        context.Check(0.000m, target: "zeroAmount", displayName: "Zero amount")
           .HasPrecisionAndScale(1, 0, ignoreTrailingZeros: true);
        context.Check(-120.3400m, target: "negativeAmount", displayName: "Negative amount")
           .HasPrecisionAndScale(5, 2, ignoreTrailingZeros: true);
        context.Check(0.00m, target: "zeroAmountNoTrim", displayName: "Zero amount no trim")
           .HasPrecisionAndScale(2, 2);
        context.Check(12.3401m, target: "nonTrimmedAmount", displayName: "Non-trimmed amount")
           .HasPrecisionAndScale(6, 4, ignoreTrailingZeros: true);
        context.Check(123m, target: "wholeAmount", displayName: "Whole amount")
           .HasPrecisionAndScale(3, 0, ignoreTrailingZeros: true);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldThrow_WhenScaleIsNegative()
    {
        var context = CreateContext();
        Action negativeScaleAct = () => context.Check(12.34m, target: "invalidAmount").HasPrecisionAndScale(4, -1);

        negativeScaleAct.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("scale");
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check(123.45m, target: "decimalOverride", displayName: "Decimal override")
           .HasPrecisionAndScale(4, 1, new ErrorOverrides { Code = "DecimalOverride" }, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "decimalOverride" && error.Code == "DecimalOverride");
    }

    [Fact]
    public void HasPrecisionAndScale_Nullable_ShouldAddError_WhenRequested()
    {
        var context = CreateContext();
        context.Check((decimal?) 123.45m, target: "nullableDecimal", displayName: "Nullable decimal")
           .HasPrecisionAndScale(4, 1, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "nullableDecimal" && error.Code == "PrecisionScale");
    }

    [Fact]
    public void IsEmpty_GenericString_ShouldAddError_WhenRequested()
    {
        var context = CreateContext();
        context.Check<IEnumerable<char>>("abc", target: "genericStringEmpty", displayName: "Generic string empty")
           .IsEmpty(shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "genericStringEmpty" && error.Code == "Empty");
    }

    [Fact]
    public void IsEmpty_ReadOnlyCollection_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check<IEnumerable<int>>(
                new NumberReadOnlyCollection(1, 2),
                target: "readOnlyEmpty",
                displayName: "Read-only empty"
            )
           .IsEmpty(new ErrorOverrides { Code = "ReadOnlyEmpty" }, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "readOnlyEmpty" && error.Code == "ReadOnlyEmpty");
    }

    [Fact]
    public void IsNotEmpty_GenericCollection_ShouldAddError_WhenRequested()
    {
        var context = CreateContext();
        context.Check<IEnumerable<int>>(
                Array.Empty<int>(),
                target: "genericCollectionNotEmpty",
                displayName: "Generic collection not empty"
            )
           .IsNotEmpty(shortCircuitOnError: true);

        context.Errors.Should()
           .Contain(error => error.Target == "genericCollectionNotEmpty" && error.Code == "NotEmpty");
    }

    [Fact]
    public void IsNotEmpty_List_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check(new List<int>(), target: "listNotEmpty", displayName: "List not empty")
           .IsNotEmpty(new ErrorOverrides { Code = "ListNotEmpty" }, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "listNotEmpty" && error.Code == "ListNotEmpty");
    }

    [Fact]
    public void IsEmpty_NonGenericCountedEnumerable_ShouldAddError_WhenRequested()
    {
        var context = CreateContext();
        context.Check(
                new NonGenericCountedEnumerable(1, 2),
                target: "nonGenericEmpty",
                displayName: "Non-generic empty"
            )
           .IsEmpty(shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "nonGenericEmpty" && error.Code == "Empty");
    }

    [Fact]
    public void IsEmpty_YieldOnlyGenericEnumerable_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check(
                new YieldOnlyGenericEnumerable(1, 2),
                target: "yieldEmpty",
                displayName: "Yield empty"
            )
           .IsEmpty(new ErrorOverrides { Code = "YieldEmpty" }, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "yieldEmpty" && error.Code == "YieldEmpty");
    }

    [Fact]
    public void IsEqualTo_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check("abc", target: "equalityOverride", displayName: "Equality override")
           .IsEqualTo(
                "xyz",
                StringComparer.Ordinal,
                new ErrorOverrides { Code = "EqualityOverride" },
                shortCircuitOnError: true
            );

        context.Errors.Should()
           .Contain(error => error.Target == "equalityOverride" && error.Code == "EqualityOverride");
    }

    [Fact]
    public void IsNotEqualTo_ShouldAddError_WhenOverridesAreUsedAndRequested()
    {
        var context = CreateContext();
        context.Check("abc", target: "notEqualityOverride", displayName: "Not equality override")
           .IsNotEqualTo(
                "abc",
                StringComparer.Ordinal,
                new ErrorOverrides { Code = "NotEqualityOverride" },
                shortCircuitOnError: true
            );

        context.Errors.Should().Contain(
            error => error.Target == "notEqualityOverride" && error.Code == "NotEqualityOverride"
        );
    }

    [Fact]
    public void MustPredicate_ShouldAddError_WhenOverridesAreUsedAndRequested_2()
    {
        var context = CreateContext();
        context.Check("abc", target: "predicateOverride", displayName: "Predicate override")
           .Must(static _ => false, new ErrorOverrides { Code = "PredicateOverride" }, shortCircuitOnError: true);

        context.Errors.Should()
           .Contain(error => error.Target == "predicateOverride" && error.Code == "PredicateOverride");
    }

    [Fact]
    public void MustPredicate_ShouldAddError_WhenTemplateIsUsedAndRequested()
    {
        var context = CreateContext();
        context.Check("abc", target: "predicateTemplate", displayName: "Predicate template")
           .Must(
                static _ => false,
                new ValidationErrorTemplates.Constant("Predicate template invalid"),
                code: "PredicateTemplate",
                shortCircuitOnError: true
            );

        context.Errors.Should()
           .Contain(error => error.Target == "predicateTemplate" && error.Code == "PredicateTemplate");
    }

    [Fact]
    public void MustContextPredicate_ShouldAddError_WhenDefinitionIsUsedAndRequested()
    {
        var context = CreateContext();
        context.Check("abc", target: "contextPredicateDefinition", displayName: "Context predicate definition")
           .Must(static (_, _) => false, BuiltInValidationErrorDefinitions.Predicate, shortCircuitOnError: true);

        context.Errors.Should()
           .Contain(error => error.Target == "contextPredicateDefinition" && error.Code == "Predicate");
    }

    [Fact]
    public void MustContextPredicate_ShouldAddError_WhenOverridesAreUsedAndRequested_2()
    {
        var context = CreateContext();
        context.Check("abc", target: "contextPredicateOverride", displayName: "Context predicate override")
           .Must(
                static (_, _) => false,
                new ErrorOverrides { Code = "ContextPredicateOverride" },
                shortCircuitOnError: true
            );

        context.Errors.Should().Contain(
            error => error.Target == "contextPredicateOverride" && error.Code == "ContextPredicateOverride"
        );
    }

    [Fact]
    public void HasMinLength_ShouldAddError_WhenRequested_2()
    {
        var context = CreateContext();
        context.Check("ab", target: "shortMinLength", displayName: "Short min length")
           .HasMinLength(3, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "shortMinLength" && error.Code == "MinLength");
    }

    [Fact]
    public void MatchesRegex_ShouldAddError_WhenOverridesAreUsedAndRequested_2()
    {
        var context = CreateContext();
        context.Check("abc", target: "regexOverride", displayName: "Regex override")
           .Matches(
                regex: new Regex("^\\d+$"),
                overrides: new ErrorOverrides { Code = "RegexOverride" },
                shortCircuitOnError: true
            );

        context.Errors.Should().Contain(error => error.Target == "regexOverride" && error.Code == "RegexOverride");
    }

    [Fact]
    public void MatchesPattern_ShouldAddError_WhenOverridesAreUsedAndRequested_2()
    {
        var context = CreateContext();
        context.Check("abc", target: "patternOverride", displayName: "Pattern override")
           .Matches("^\\d+$", new ErrorOverrides { Code = "PatternOverride" }, shortCircuitOnError: true);

        context.Errors.Should().Contain(error => error.Target == "patternOverride" && error.Code == "PatternOverride");
    }

    [Fact]
    public void IsEmail_ShouldAddError_WhenOverridesAreUsedAndRequested_2()
    {
        var context = CreateContext();
        context.Check("abc", target: "emailOverrideCode", displayName: "Email override code")
           .IsEmail(new ErrorOverrides { Code = "EmailOverrideCode" }, shortCircuitOnError: true);

        context.Errors.Should()
           .Contain(error => error.Target == "emailOverrideCode" && error.Code == "EmailOverrideCode");
    }

    [Fact]
    public void HasPrecisionAndScale_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check(12.34m, target: "decimalOverrideSuccess", displayName: "Decimal override success")
           .HasPrecisionAndScale(4, 2, new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasPrecisionAndScale_Nullable_ShouldNotAddError()
    {
        var context = CreateContext();
        context.Check((decimal?) 12.34m, target: "nullableDecimalSuccess", displayName: "Nullable decimal success")
           .HasPrecisionAndScale(4, 2);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        IEnumerable emptyItems = Array.Empty<int>();
        context.Check(emptyItems, target: "emptyItemsSuccess", displayName: "Empty items success")
           .IsEmpty(new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_NullItems_ShouldNotAddError()
    {
        var context = CreateContext();
        IEnumerable nullItems = null!;
        context.Check(
                nullItems,
                NoOpValueNormalizer.Instance,
                target: "nullItemsSuccess",
                displayName: "Null items success"
            )
           .IsEmpty();

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_NullItems_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        IEnumerable nullItems = null!;
        context.Check(
                nullItems,
                NoOpValueNormalizer.Instance,
                target: "nullItemsOverrideSuccess",
                displayName: "Null items override success"
            )
           .IsEmpty(new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNotEmpty_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        IEnumerable nonEmptyItems = new[] { 1, 2 };
        context.Check(nonEmptyItems, target: "notEmptyItemsSuccess", displayName: "Not empty items success")
           .IsNotEmpty(new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEqualTo_ShouldNotAddError_WhenComparerAndOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("ABC", target: "equalSuccess", displayName: "Equal success")
           .IsEqualTo(
                "abc",
                StringComparer.OrdinalIgnoreCase,
                new ErrorOverrides { Code = "Unused" }
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsNotEqualTo_ShouldNotAddError_WhenComparerAndOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("ABC", target: "notEqualSuccess", displayName: "Not equal success")
           .IsNotEqualTo(
                "xyz",
                StringComparer.OrdinalIgnoreCase,
                new ErrorOverrides { Code = "Unused" }
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MustPredicate_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("abc", target: "predicateOverrideSuccess", displayName: "Predicate override success")
           .Must(static value => value.Length == 3, new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MustPredicate_ShouldNotAddError_WhenTemplateIsUsed()
    {
        var context = CreateContext();
        context.Check("abc", target: "predicateTemplateSuccess", displayName: "Predicate template success")
           .Must(
                static value => value.Length == 3,
                new ValidationErrorTemplates.Constant("Unused"),
                code: "Unused"
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MustContextPredicate_ShouldNotAddError()
    {
        var context = CreateContext();
        context.Check(
                "abc",
                target: "contextPredicateDefinitionSuccess",
                displayName: "Context predicate definition success"
            )
           .Must(
                static (readOnlyContext, value) =>
                    readOnlyContext.TargetPrefix == "contextPredicateDefinitionSuccess" && value.Length == 3
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MustContextPredicate_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check(
                "abc",
                target: "contextPredicateOverrideSuccess",
                displayName: "Context predicate override success"
            )
           .Must(
                static (readOnlyContext, value) =>
                    readOnlyContext.TargetPrefix == "contextPredicateOverrideSuccess" && value.Length == 3,
                new ErrorOverrides { Code = "Unused" }
            );

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasMinLength_ShouldNotAddError()
    {
        var context = CreateContext();
        context.Check("abc", target: "minLengthSuccess", displayName: "Min length success").HasMinLength(3);

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MatchesRegex_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("123", target: "regexOverrideSuccess", displayName: "Regex override success")
           .Matches(new Regex("^\\d+$"), new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void MatchesPattern_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("123", target: "patternOverrideSuccess", displayName: "Pattern override success")
           .Matches("^\\d+$", new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEmail_ShouldNotAddError_WhenOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check("user@example.com", target: "emailOverrideSuccess", displayName: "Email override success")
           .IsEmail(new ErrorOverrides { Code = "Unused" });

        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEqualTo_ShouldThrow_WhenComparerIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "equalGuard")
           .IsEqualTo("abc", null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("equalityComparer");
    }

    [Fact]
    public void IsNotEqualTo_ShouldThrow_WhenComparerIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "notEqualGuard")
           .IsNotEqualTo("abc", null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("equalityComparer");
    }

    [Fact]
    public void MustPredicate_ShouldThrow_WhenPredicateIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "predicateGuard")
           .Must((Func<string, bool>) null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void MustContextPredicate_ShouldThrow_WhenPredicateIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "contextPredicateGuard")
           .Must((Func<ReadOnlyValidationContext, string, bool>) null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void MustTemplate_ShouldThrow_WhenPredicateIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "templatePredicatePredicateGuard")
           .Must(null!, new ValidationErrorTemplates.Constant("Unused"), code: "Unused");

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void MustTemplate_ShouldThrow_WhenTemplateIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "templatePredicateGuard")
           .Must(static _ => false, null!, code: "Unused");

        act.Should().Throw<ArgumentNullException>().WithParameterName("template");
    }

    [Fact]
    public void MatchesRegex_ShouldThrow_WhenRegexIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "regexGuard")
           .Matches((Regex) null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("regex");
    }

    [Fact]
    public void MatchesPattern_ShouldThrow_WhenPatternIsNull()
    {
        var context = CreateContext();
        Action act = () => context.Check("abc", target: "patternGuard")
           .Matches((string) null!, new ErrorOverrides { Code = "Unused" });

        act.Should().Throw<ArgumentNullException>().WithParameterName("pattern");
    }

    [Fact]
    public void IsNotEmpty_Collection_ShouldAddError_WhenNull()
    {
        var context = CreateContext();
        context.Check<IEnumerable>(
                null!,
                NoOpValueNormalizer.Instance,
                target: "nullItemsRequired",
                displayName: "Null items required"
            )
           .IsNotEmpty();

        context.Errors.Should().Contain(error => error.Target == "nullItemsRequired" && error.Code == "NotEmpty");
    }

    [Fact]
    public void IsNotEmpty_Collection_ShouldAddError_WhenNullAndOverridesAreUsed()
    {
        var context = CreateContext();
        context.Check<IEnumerable>(
                null!,
                NoOpValueNormalizer.Instance,
                target: "nullItemsRequiredOverride",
                displayName: "Null items required override"
            )
           .IsNotEmpty(new ErrorOverrides { Code = "NullItemsRequiredOverride" });

        context.Errors.Should().Contain(
            error => error.Target == "nullItemsRequiredOverride" && error.Code == "NullItemsRequiredOverride"
        );
    }

    [Fact]
    public void IsEmail_ShouldRespectRichOverrides()
    {
        var context = CreateContext();
        var metadata = MetadataObject.Create(("source", "override"));
        context.Check("abc", target: "richEmailOverride", displayName: "Rich email override")
           .IsEmail(
                new ErrorOverrides
                {
                    Message = "Rich email override is invalid",
                    Code = "RichEmailOverride",
                    Metadata = metadata,
                    Category = ErrorCategory.UnprocessableContent
                },
                shortCircuitOnError: true
            );

        context.Errors.Should().Contain(
            error => error.Target == "richEmailOverride" &&
                     error.Code == "RichEmailOverride" &&
                     error.Category == ErrorCategory.UnprocessableContent &&
                     error.Metadata == metadata
        );
    }

    [Fact]
    public void MustTemplate_ShouldUseDefaultCode_WhenNoneIsProvided()
    {
        var context = CreateContext();
        context.Check("abc", target: "templateDefaultCode", displayName: "Template default code")
           .Must(
                static _ => false,
                new ValidationErrorTemplates.Constant("Template default code is invalid"),
                shortCircuitOnError: true
            );

        context.Errors.Should().Contain(
            error => error.Target == "templateDefaultCode" && error.Code == "Predicate"
        );
    }

    private static ValidationContext CreateContext()
    {
        var options = new ValidationContextOptions() with
        {
            ValueNormalizer = NoOpValueNormalizer.Instance,
            AutomaticNullErrorProvider = NoOpAutomaticNullErrorProvider.Instance
        };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private sealed class ObjectReadOnlyCollection : IEnumerable, IReadOnlyCollection<object>
    {
        private readonly object[] _values;

        public ObjectReadOnlyCollection(params object[] values) => _values = values;

        public IEnumerator GetEnumerator() => _values.GetEnumerator();

        public int Count => _values.Length;

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => ((IEnumerable<object>) _values).GetEnumerator();
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

    private sealed class NumberReadOnlyCollection : IEnumerable<int>, IReadOnlyCollection<int>
    {
        private readonly int[] _values;

        public NumberReadOnlyCollection(params int[] values) => _values = values;

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>) _values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public int Count => _values.Length;
    }

    private sealed class NonGenericCountedEnumerable : IEnumerable<int>, ICollection
    {
        private readonly int[] _values;

        public NonGenericCountedEnumerable(params int[] values) => _values = values;

        public int Count => _values.Length;
        public bool IsSynchronized => false;
        public object SyncRoot { get; } = new ();

        public void CopyTo(Array array, int index) => ((ICollection) _values).CopyTo(array, index);

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>) _values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
    }

    private sealed class YieldOnlyGenericEnumerable : IEnumerable<int>
    {
        private readonly int[] _values;

        public YieldOnlyGenericEnumerable(params int[] values) => _values = values;

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var value in _values)
            {
                yield return value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
