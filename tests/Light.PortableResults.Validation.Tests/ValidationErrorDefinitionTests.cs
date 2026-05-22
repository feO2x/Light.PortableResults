using System;
using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationErrorDefinitionTests
{
    [Fact]
    public void BuiltInDefinitions_ShouldExposeExpectedDefaults()
    {
        BuiltInValidationErrorDefinitions.NotNull.Code.Should().Be(ValidationErrorCodes.NotNull);
        BuiltInValidationErrorDefinitions.Null.Code.Should().Be(ValidationErrorCodes.Null);
        BuiltInValidationErrorDefinitions.Empty.Code.Should().Be(ValidationErrorCodes.Empty);
        BuiltInValidationErrorDefinitions.NotEmpty.Code.Should().Be(ValidationErrorCodes.NotEmpty);
        BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace.Code.Should().Be(ValidationErrorCodes.NotNullOrWhiteSpace);
        BuiltInValidationErrorDefinitions.Email.Code.Should().Be(ValidationErrorCodes.Email);
        BuiltInValidationErrorDefinitions.Predicate.Code.Should().Be(ValidationErrorCodes.Predicate);
        BuiltInValidationErrorDefinitions.NotNull.Metadata.Should().BeNull();
        BuiltInValidationErrorDefinitions.Email.Metadata.Should().BeNull();
    }

    [Fact]
    public void BuiltInDefinitions_ShouldEmitRenamedRuntimeCodes()
    {
        BuiltInValidationErrorDefinitions.LengthIn(2, 5).Code.Should().Be(ValidationErrorCodes.LengthInRange);
        BuiltInValidationErrorDefinitions.Matches("^[0-9]+$").Code.Should().Be(ValidationErrorCodes.Pattern);
        BuiltInValidationErrorDefinitions.IsInRange(1, 5).Code.Should().Be(ValidationErrorCodes.InRange);
        BuiltInValidationErrorDefinitions.IsNotInRange(1, 5).Code.Should().Be(ValidationErrorCodes.NotInRange);
    }

    [Fact]
    public void EqualTo_ShouldExposeExpectedMetadata()
    {
        var equalTo = BuiltInValidationErrorDefinitions.EqualTo("ABC");
        equalTo.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, "ABC"))
        );
    }

    [Fact]
    public void LengthIn_ShouldExposeExpectedMetadata()
    {
        var lengthIn = BuiltInValidationErrorDefinitions.LengthIn(2, 5);
        lengthIn.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.MinLength, 2),
                (ValidationErrorMetadataKeys.MaxLength, 5)
            )
        );
    }

    [Fact]
    public void Matches_ShouldExposeExpectedMetadata()
    {
        var matches = BuiltInValidationErrorDefinitions.Matches("^[0-9]+$", RegexOptions.IgnoreCase);
        matches.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.Pattern, "^[0-9]+$"),
                (ValidationErrorMetadataKeys.RegexOptions, (int) RegexOptions.IgnoreCase)
            )
        );
    }

    [Fact]
    public void Matches_ShouldExposePatternAndOptions()
    {
        var matches = BuiltInValidationErrorDefinitions.Matches("^[0-9]+$", RegexOptions.IgnoreCase);
        matches.Pattern.Should().Be("^[0-9]+$");
        matches.Options.Should().Be(RegexOptions.IgnoreCase);
    }

    [Fact]
    public void PrecisionScale_ShouldExposeExpectedMetadata()
    {
        var precisionScale = BuiltInValidationErrorDefinitions.PrecisionScale(4, 2, ignoreTrailingZeros: true);
        precisionScale.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.ExpectedPrecision, 4),
                (ValidationErrorMetadataKeys.ExpectedScale, 2),
                (ValidationErrorMetadataKeys.IgnoreTrailingZeros, true)
            )
        );
    }

    [Fact]
    public void EnumName_ShouldExposeIgnoreCase()
    {
        var enumName = BuiltInValidationErrorDefinitions.EnumName<TestStatus>(ignoreCase: true);
        enumName.IgnoreCase.Should().BeTrue();
    }

    [Fact]
    public void ValidationRange_ShouldBeEqual_WhenBoundariesAreEqual()
    {
        var range = new ValidationRange<int>(1, 10);
        range.Should().Be(new ValidationRange<int>(1, 10));
    }

    [Fact]
    public void ValidationRange_ShouldBeIdentical_WhenBoundariesAreIdentical()
    {
        var range = new ValidationRange<int>(1, 10);
        (range == new ValidationRange<int>(1, 10)).Should().BeTrue();
    }

    [Fact]
    public void ValidationRange_ShouldBeDifferent_WhenBoundariesAreDifferent()
    {
        var range = new ValidationRange<int>(1, 10);
        (range != new ValidationRange<int>(1, 11)).Should().BeTrue();
    }

    [Fact]
    public void PrecisionScaleDescriptor_ShouldBeEqual_WhenValuesAreEqual()
    {
        var descriptor = new PrecisionScaleDescriptor(4, 2, true);
        descriptor.Should().Be(new PrecisionScaleDescriptor(4, 2, true));
    }

    [Fact]
    public void PrecisionScaleDescriptor_ShouldBeIdentical_WhenValuesAreIdentical()
    {
        var descriptor = new PrecisionScaleDescriptor(4, 2, true);
        (descriptor == new PrecisionScaleDescriptor(4, 2, true)).Should().BeTrue();
    }

    [Fact]
    public void PrecisionScaleDescriptor_ShouldBeDifferent_WhenValuesAreDifferent()
    {
        var descriptor = new PrecisionScaleDescriptor(4, 2, true);
        (descriptor != new PrecisionScaleDescriptor(4, 3, true)).Should().BeTrue();
    }

    [Fact]
    public void GreaterThan_ShouldExposeExpectedMetadata()
    {
        var greaterThan = BuiltInValidationErrorDefinitions.GreaterThan((byte) 2);
        greaterThan.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 2)));
    }

    [Fact]
    public void GreaterThanOrEqualTo_ShouldExposeExpectedMetadata()
    {
        var greaterThanOrEqualTo = BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo((ushort) 3);
        greaterThanOrEqualTo.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 3))
        );
    }

    [Fact]
    public void LessThan_ShouldExposeExpectedMetadata()
    {
        var lessThan = BuiltInValidationErrorDefinitions.LessThan((short) 4);
        lessThan.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 4)));
    }

    [Fact]
    public void LessThanOrEqualTo_ShouldExposeExpectedMetadata()
    {
        var lessThanOrEqualTo = BuiltInValidationErrorDefinitions.LessThanOrEqualTo((uint) 5);
        lessThanOrEqualTo.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, 5U))
        );
    }

    [Fact]
    public void IsIn_ShouldExposeExpectedMetadata()
    {
        var inRange = BuiltInValidationErrorDefinitions.IsInRange(6UL, 7UL);
        inRange.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.LowerBoundary, "6"),
                (ValidationErrorMetadataKeys.UpperBoundary, "7")
            )
        );
    }

    [Fact]
    public void IsNotIn_ShouldExposeExpectedMetadata()
    {
        var notInRange = BuiltInValidationErrorDefinitions.IsNotInRange('A', 'Z');
        notInRange.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.LowerBoundary, "A"),
                (ValidationErrorMetadataKeys.UpperBoundary, "Z")
            )
        );
    }

    [Fact]
    public void IsInExclusiveRange_ShouldExposeExpectedMetadata()
    {
        var exclusiveRange = BuiltInValidationErrorDefinitions.IsInExclusiveRange(1.25f, 2.5f);
        exclusiveRange.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.LowerBoundary, 1.25f),
                (ValidationErrorMetadataKeys.UpperBoundary, 2.5f)
            )
        );
    }

    [Fact]
    public void Count_ShouldExposeExpectedMetadata()
    {
        var exactCount = BuiltInValidationErrorDefinitions.Count(3);
        exactCount.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.ExpectedCount, 3)));
    }

    [Fact]
    public void MinCount_ShouldExposeExpectedMetadata()
    {
        var minCount = BuiltInValidationErrorDefinitions.MinCount(4);
        minCount.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.MinCount, 4)));
    }

    [Fact]
    public void MaxCount_ShouldExposeExpectedMetadata()
    {
        var maxCount = BuiltInValidationErrorDefinitions.MaxCount(5);
        maxCount.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.MaxCount, 5)));
    }

    [Fact]
    public void NotEqualTo_ShouldExposeExpectedMetadata()
    {
        var notEqualTo = BuiltInValidationErrorDefinitions.NotEqualTo(OrderStatus.Approved);
        notEqualTo.Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, OrderStatus.Approved.ToString()))
        );
    }

    [Fact]
    public void MinLength_ShouldExposeExpectedMetadata()
    {
        var minLength = BuiltInValidationErrorDefinitions.MinLength(2);
        minLength.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.MinLength, 2)));
    }

    [Fact]
    public void MaxLength_ShouldExposeExpectedMetadata()
    {
        var maxLength = BuiltInValidationErrorDefinitions.MaxLength(8);
        maxLength.Metadata.Should().Be(MetadataObject.Create((ValidationErrorMetadataKeys.MaxLength, 8)));
    }

    [Fact]
    public void EnumName_ShouldExposeExpectedMetadata()
    {
        var enumName = BuiltInValidationErrorDefinitions.EnumName<TestStatus>(ignoreCase: true);
        enumName.Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.EnumType, typeof(TestStatus).FullName!),
                (ValidationErrorMetadataKeys.IgnoreCase, true)
            )
        );
    }

    [Fact]
    public void EqualTo_ShouldConvertComplexMetadataValues_MetadataObject()
    {
        var metadataObject = MetadataObject.Create(("tenant", "checkout"));
        BuiltInValidationErrorDefinitions.EqualTo(metadataObject).Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, metadataObject))
        );
    }

    [Fact]
    public void EqualTo_ShouldConvertComplexMetadataValues_MetadataArray()
    {
        var metadataArray = MetadataArray.Create("A", "B");
        BuiltInValidationErrorDefinitions.EqualTo(metadataArray).Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, metadataArray))
        );
    }

    [Fact]
    public void EqualTo_ShouldConvertComplexMetadataValues_DateTime()
    {
        var date = new DateTime(2024, 12, 24, 10, 30, 00, DateTimeKind.Utc);
        BuiltInValidationErrorDefinitions.EqualTo(date).Metadata.Should().Be(
            MetadataObject.Create(
                (ValidationErrorMetadataKeys.ComparativeValue, date.ToString(null, CultureInfo.InvariantCulture))
            )
        );
    }

    [Fact]
    public void EqualTo_ShouldConvertComplexMetadataValues_CustomValue()
    {
        var custom = new CustomValue("alpha");
        BuiltInValidationErrorDefinitions.EqualTo(custom).Metadata.Should().Be(
            MetadataObject.Create((ValidationErrorMetadataKeys.ComparativeValue, "custom:alpha"))
        );
    }

    [Fact]
    public void ComparableDefinitions_ShouldExposeStableProviders()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var readOnlyContext = context.AsReadOnly();
        var greaterThan = BuiltInValidationErrorDefinitions.GreaterThan(18);
        var greaterThanOrEqualTo = BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo(18);
        var lessThan = BuiltInValidationErrorDefinitions.LessThan(18);
        var lessThanOrEqualTo = BuiltInValidationErrorDefinitions.LessThanOrEqualTo(18);
        var inRange = BuiltInValidationErrorDefinitions.IsInRange(1, 10);
        var notInRange = BuiltInValidationErrorDefinitions.IsNotInRange(1, 10);
        var exclusiveRange = BuiltInValidationErrorDefinitions.IsInExclusiveRange(1, 10);

        greaterThan.TryGetStableMessageProvider(readOnlyContext, out var greaterThanProvider).Should().BeTrue();
        greaterThanOrEqualTo.TryGetStableMessageProvider(readOnlyContext, out var greaterThanOrEqualProvider).Should()
           .BeTrue();
        lessThan.TryGetStableMessageProvider(readOnlyContext, out var lessThanProvider).Should().BeTrue();
        lessThanOrEqualTo.TryGetStableMessageProvider(readOnlyContext, out var lessThanOrEqualProvider).Should()
           .BeTrue();
        inRange.TryGetStableMessageProvider(readOnlyContext, out var inRangeProvider).Should().BeTrue();
        notInRange.TryGetStableMessageProvider(readOnlyContext, out var notInRangeProvider).Should().BeTrue();
        exclusiveRange.TryGetStableMessageProvider(readOnlyContext, out var exclusiveRangeProvider).Should().BeTrue();

        greaterThanProvider.Should().BeSameAs(context.ErrorTemplates.GreaterThan);
        greaterThanOrEqualProvider.Should().BeSameAs(context.ErrorTemplates.GreaterThanOrEqualTo);
        lessThanProvider.Should().BeSameAs(context.ErrorTemplates.LessThan);
        lessThanOrEqualProvider.Should().BeSameAs(context.ErrorTemplates.LessThanOrEqualTo);
        inRangeProvider.Should().BeSameAs(context.ErrorTemplates.IsInRange);
        notInRangeProvider.Should().BeSameAs(context.ErrorTemplates.NotInRange);
        exclusiveRangeProvider.Should().BeSameAs(context.ErrorTemplates.ExclusiveRange);
    }

    [Fact]
    public void GreaterThan_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var greaterThan = BuiltInValidationErrorDefinitions.GreaterThan(18);
        greaterThan.ProvideMessage(messageContext).Text.Should().Be("Age must be greater than 18");
    }

    [Fact]
    public void GreaterThanOrEqualTo_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var greaterThanOrEqualTo = BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo(18);
        greaterThanOrEqualTo.ProvideMessage(messageContext).Text.Should().Be("Age must be greater than or equal to 18");
    }

    [Fact]
    public void LessThan_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var lessThan = BuiltInValidationErrorDefinitions.LessThan(18);
        lessThan.ProvideMessage(messageContext).Text.Should().Be("Age must be less than 18");
    }

    [Fact]
    public void LessThanOrEqualTo_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var lessThanOrEqualTo = BuiltInValidationErrorDefinitions.LessThanOrEqualTo(18);
        lessThanOrEqualTo.ProvideMessage(messageContext).Text.Should().Be("Age must be less than or equal to 18");
    }

    [Fact]
    public void IsIn_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var inRange = BuiltInValidationErrorDefinitions.IsInRange(1, 10);
        inRange.ProvideMessage(messageContext).Text.Should().Be("Age must be between 1 and 10");
    }

    [Fact]
    public void IsNotIn_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var notInRange = BuiltInValidationErrorDefinitions.IsNotInRange(1, 10);
        notInRange.ProvideMessage(messageContext).Text.Should().Be("Age must not be between 1 and 10");
    }

    [Fact]
    public void IsInExclusiveRange_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(10, target: "age", displayName: "Age").CreateMessageContext();
        var exclusiveRange = BuiltInValidationErrorDefinitions.IsInExclusiveRange(1, 10);
        exclusiveRange.ProvideMessage(messageContext).Text.Should().Be("Age must be between 1 and 10 (exclusive)");
    }

    [Fact]
    public void GreaterThan_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.GreaterThan(null!, 18);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void GreaterThanOrEqualTo_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo(null!, 18);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void IsIn_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.IsInRange<string>(null!, "A", "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void IsNotIn_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.IsNotInRange<string>(null!, "A", "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void IsInExclusiveRange_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.IsInExclusiveRange<string>(null!, "A", "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void GreaterThan_ShouldThrow_WhenValueIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.GreaterThan(cache, (string) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("comparativeValue");
    }

    [Fact]
    public void GreaterThanOrEqualTo_ShouldThrow_WhenValueIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.GreaterThanOrEqualTo(cache, (string) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("comparativeValue");
    }

    [Fact]
    public void LessThan_ShouldThrow_WhenValueIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.LessThan(cache, (string) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("comparativeValue");
    }

    [Fact]
    public void LessThanOrEqualTo_ShouldThrow_WhenValueIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.LessThanOrEqualTo(cache, (string) null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("comparativeValue");
    }

    [Fact]
    public void IsIn_ShouldThrow_WhenLowerBoundaryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.IsInRange(cache, null!, "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("lowerBoundary");
    }

    [Fact]
    public void IsNotIn_ShouldThrow_WhenLowerBoundaryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.IsNotInRange(cache, null!, "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("lowerBoundary");
    }

    [Fact]
    public void IsInExclusiveRange_ShouldThrow_WhenLowerBoundaryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.IsInExclusiveRange(cache, null!, "Z");
        act.Should().Throw<ArgumentNullException>().WithParameterName("lowerBoundary");
    }

    [Fact]
    public void IsNotIn_ShouldThrow_WhenUpperBoundaryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.IsNotInRange(cache, "A", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("upperBoundary");
    }

    [Fact]
    public void IsInExclusiveRange_ShouldThrow_WhenUpperBoundaryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.IsInExclusiveRange(cache, "A", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("upperBoundary");
    }

    [Fact]
    public void ParameterizedTemplateDefinitions_ShouldReportStableProviders()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext().AsReadOnly();
        var stableTemplate = new ValidationErrorTemplates.IgnoreParameter<int>(
            new ValidationErrorTemplates.Constant("Always invalid")
        );
        var stableDefinition = new TemplateValidationErrorDefinition<int>(stableTemplate, 5);
        var unstableDefinition = new TemplateValidationErrorDefinition<int>(new UnstableParameterizedTemplate(), 5);

        stableDefinition.TryGetStableMessageProvider(context, out var stableProvider).Should().BeTrue();
        unstableDefinition.TryGetStableMessageProvider(context, out var unstableProvider).Should().BeFalse();

        stableProvider.Should().BeSameAs(stableTemplate);
        unstableProvider.Should().BeNull();
    }

    [Fact]
    public void CountEqualityStringEnumAndDecimalDefinitions_ShouldExposeStableProviders()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var readOnlyContext = context.AsReadOnly();
        var count = BuiltInValidationErrorDefinitions.Count(3);
        var minCount = BuiltInValidationErrorDefinitions.MinCount(2);
        var maxCount = BuiltInValidationErrorDefinitions.MaxCount(4);
        var equalTo = BuiltInValidationErrorDefinitions.EqualTo("ABC");
        var notEqualTo = BuiltInValidationErrorDefinitions.NotEqualTo("ABC");
        var notNullOrWhiteSpace = BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace;
        var minLength = BuiltInValidationErrorDefinitions.MinLength(2);
        var maxLength = BuiltInValidationErrorDefinitions.MaxLength(4);
        var lengthIn = BuiltInValidationErrorDefinitions.LengthIn(2, 4);
        var pattern = BuiltInValidationErrorDefinitions.Matches("^[A-Z0-9]+$", RegexOptions.IgnoreCase);
        var email = BuiltInValidationErrorDefinitions.Email;
        var digitsOnly = BuiltInValidationErrorDefinitions.DigitsOnly;
        var lettersAndDigitsOnly = BuiltInValidationErrorDefinitions.LettersAndDigitsOnly;
        var enumValue = BuiltInValidationErrorDefinitions.IsInEnum<TestStatus>();
        var precisionScale = BuiltInValidationErrorDefinitions.PrecisionScale(4, 2, ignoreTrailingZeros: true);

        count.TryGetStableMessageProvider(readOnlyContext, out var countProvider).Should().BeTrue();
        minCount.TryGetStableMessageProvider(readOnlyContext, out var minCountProvider).Should().BeTrue();
        maxCount.TryGetStableMessageProvider(readOnlyContext, out var maxCountProvider).Should().BeTrue();
        equalTo.TryGetStableMessageProvider(readOnlyContext, out var equalToProvider).Should().BeTrue();
        notEqualTo.TryGetStableMessageProvider(readOnlyContext, out var notEqualToProvider).Should().BeTrue();
        notNullOrWhiteSpace.TryGetStableMessageProvider(readOnlyContext, out var requiredProvider).Should().BeTrue();
        minLength.TryGetStableMessageProvider(readOnlyContext, out var minLengthProvider).Should().BeTrue();
        maxLength.TryGetStableMessageProvider(readOnlyContext, out var maxLengthProvider).Should().BeTrue();
        lengthIn.TryGetStableMessageProvider(readOnlyContext, out var lengthInProvider).Should().BeTrue();
        pattern.TryGetStableMessageProvider(readOnlyContext, out var patternProvider).Should().BeTrue();
        email.TryGetStableMessageProvider(readOnlyContext, out var emailProvider).Should().BeTrue();
        digitsOnly.TryGetStableMessageProvider(readOnlyContext, out var digitsProvider).Should().BeTrue();
        lettersAndDigitsOnly.TryGetStableMessageProvider(readOnlyContext, out var lettersDigitsProvider).Should()
           .BeTrue();
        enumValue.TryGetStableMessageProvider(readOnlyContext, out var enumProvider).Should().BeTrue();
        precisionScale.TryGetStableMessageProvider(readOnlyContext, out var precisionScaleProvider).Should().BeTrue();

        countProvider.Should().BeSameAs(context.ErrorTemplates.Count);
        minCountProvider.Should().BeSameAs(context.ErrorTemplates.MinCount);
        maxCountProvider.Should().BeSameAs(context.ErrorTemplates.MaxCount);
        equalToProvider.Should().BeSameAs(context.ErrorTemplates.EqualTo);
        notEqualToProvider.Should().BeSameAs(context.ErrorTemplates.NotEqualTo);
        requiredProvider.Should().BeSameAs(context.ErrorTemplates.NotNullOrWhiteSpace);
        minLengthProvider.Should().BeSameAs(context.ErrorTemplates.MinLength);
        maxLengthProvider.Should().BeSameAs(context.ErrorTemplates.MaxLength);
        lengthInProvider.Should().BeSameAs(context.ErrorTemplates.LengthIn);
        patternProvider.Should().BeSameAs(context.ErrorTemplates.Pattern);
        emailProvider.Should().BeSameAs(context.ErrorTemplates.Email);
        digitsProvider.Should().BeSameAs(context.ErrorTemplates.DigitsOnly);
        lettersDigitsProvider.Should().BeSameAs(context.ErrorTemplates.LettersAndDigitsOnly);
        enumProvider.Should().BeSameAs(context.ErrorTemplates.Enum);
        precisionScaleProvider.Should().BeSameAs(context.ErrorTemplates.PrecisionScale);
    }

    [Fact]
    public void Count_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var count = BuiltInValidationErrorDefinitions.Count(3);
        count.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "3");
    }

    [Fact]
    public void MinCount_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var minCount = BuiltInValidationErrorDefinitions.MinCount(2);
        minCount.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "2");
    }

    [Fact]
    public void MaxCount_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var maxCount = BuiltInValidationErrorDefinitions.MaxCount(4);
        maxCount.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "4");
    }

    [Fact]
    public void EqualTo_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var equalTo = BuiltInValidationErrorDefinitions.EqualTo("ABC");
        equalTo.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "equal", "ABC");
    }

    [Fact]
    public void NotEqualTo_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var notEqualTo = BuiltInValidationErrorDefinitions.NotEqualTo("ABC");
        notEqualTo.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "not", "ABC");
    }

    [Fact]
    public void NotNullOrWhiteSpace_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var notNullOrWhiteSpace = BuiltInValidationErrorDefinitions.NotNullOrWhiteSpace;
        notNullOrWhiteSpace.ProvideMessage(messageContext).Text.Should().Contain("Code");
    }

    [Fact]
    public void MinLength_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var minLength = BuiltInValidationErrorDefinitions.MinLength(2);
        minLength.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "2");
    }

    [Fact]
    public void MaxLength_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var maxLength = BuiltInValidationErrorDefinitions.MaxLength(4);
        maxLength.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "4");
    }

    [Fact]
    public void LengthIn_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var lengthIn = BuiltInValidationErrorDefinitions.LengthIn(2, 4);
        lengthIn.ProvideMessage(messageContext).Text.Should().ContainAll("Code", "2", "4");
    }

    [Fact]
    public void Matches_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var pattern = BuiltInValidationErrorDefinitions.Matches("^[A-Z0-9]+$", RegexOptions.IgnoreCase);
        pattern.ProvideMessage(messageContext).Text.Should().Contain("Code");
    }

    [Fact]
    public void Email_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var email = BuiltInValidationErrorDefinitions.Email;
        email.ProvideMessage(messageContext).Text.Should().Contain("email");
    }

    [Fact]
    public void DigitsOnly_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var digitsOnly = BuiltInValidationErrorDefinitions.DigitsOnly;
        digitsOnly.ProvideMessage(messageContext).Text.Should().Contain("digits");
    }

    [Fact]
    public void LettersAndDigitsOnly_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var lettersAndDigitsOnly = BuiltInValidationErrorDefinitions.LettersAndDigitsOnly;
        lettersAndDigitsOnly.ProvideMessage(messageContext).Text.Should().Contain("letters and digits");
    }

    [Fact]
    public void IsInEnum_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var enumValue = BuiltInValidationErrorDefinitions.IsInEnum<TestStatus>();
        enumValue.ProvideMessage(messageContext).Text.Should().Contain("defined enum value");
    }

    [Fact]
    public void PrecisionScale_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check("A1", target: "code", displayName: "Code").CreateMessageContext();
        var precisionScale = BuiltInValidationErrorDefinitions.PrecisionScale(4, 2, ignoreTrailingZeros: true);
        precisionScale.ProvideMessage(messageContext).Text.Should().ContainAll("4", "2");
    }

    [Fact]
    public void Count_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.Count(null!, 1);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void MinCount_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.MinCount(null!, 1);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void MaxCount_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.MaxCount(null!, 1);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void EqualTo_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.EqualTo<string>(null!, "ABC");
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void MinLength_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.MinLength(null!, 1);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Matches_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.Matches(null!, "^[A-Z]+$");
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Matches_ShouldThrow_WhenPatternIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () => BuiltInValidationErrorDefinitions.Matches(cache, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("pattern");
    }

    [Fact]
    public void PrecisionScale_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.PrecisionScale(null!, 4, 2, true);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void ValidationErrorDefinitionCache_ShouldReuseDefinitions()
    {
        var cache = new ValidationErrorDefinitionCache();
        var countDefinition = cache.GetOrAdd(
            3,
            static value => new BuiltInValidationErrorDefinitions.CountValidationErrorDefinition(value)
        );
        var sameCountDefinition = cache.GetOrAdd(
            3,
            static value => new BuiltInValidationErrorDefinitions.CountValidationErrorDefinition(value)
        );
        countDefinition.Should().BeSameAs(sameCountDefinition);
    }

    [Fact]
    public void ValidationErrorDefinitionCache_ShouldDifferentiateBetweenDifferentTypes()
    {
        var cache = new ValidationErrorDefinitionCache();
        var countDefinition = cache.GetOrAdd(
            3,
            static value => new BuiltInValidationErrorDefinitions.CountValidationErrorDefinition(value)
        );
        var minCountDefinition = cache.GetOrAdd(
            3,
            static value => new BuiltInValidationErrorDefinitions.MinCountValidationErrorDefinition(value)
        );
        minCountDefinition.Should().NotBeSameAs(countDefinition);
    }

    [Fact]
    public void ValidationErrorDefinitionCache_GetOrAdd_ShouldThrow_WhenFactoryIsNull()
    {
        var cache = new ValidationErrorDefinitionCache();
        Action act = () =>
            cache.GetOrAdd<int, BuiltInValidationErrorDefinitions.CountValidationErrorDefinition>(1, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    public void EnumName_ShouldThrow_WhenCacheIsNull()
    {
        Action act = () => BuiltInValidationErrorDefinitions.EnumName<TestStatus>(null!, ignoreCase: true);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void ValidationErrorDefinitionCache_Default_ShouldNotBeNull()
    {
        ValidationErrorDefinitionCache.Default.Should().NotBeNull();
    }

    [Fact]
    public void GreaterThan_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.GreaterThan(18)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void IsIn_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.IsInRange(1, 10)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void Count_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.Count(3)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void MinCount_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.MinCount(2)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void MaxCount_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.MaxCount(4)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void IsInEnum_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.IsInEnum<TestStatus>()
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void EnumName_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.EnumName<TestStatus>(ignoreCase: true)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void PrecisionScale_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.PrecisionScale(4, 2, ignoreTrailingZeros: true)
           .TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    [Fact]
    public void Predicate_ShouldReportUnstableProviders_WhenTemplatesAreNotStable()
    {
        var context = CreateContextWithUnstableTemplates().AsReadOnly();
        BuiltInValidationErrorDefinitions.Predicate.TryGetStableMessageProvider(context, out var provider)
           .Should().BeFalse();
        provider.Should().BeNull();
    }

    private static ValidationContext CreateContextWithUnstableTemplates()
    {
        return CreateContext(
            ValidationErrorDefinitionCache.Default,
            ValidationErrorTemplates.Default with
            {
                GreaterThan = new UnstableComparableTemplate(),
                IsInRange = new UnstableRangeTemplate(),
                Count = new UnstableIntTemplate(),
                MinCount = new UnstableIntTemplate(),
                MaxCount = new UnstableIntTemplate(),
                Enum = new UnstableTemplate(),
                EnumName = new UnstableTemplate(),
                PrecisionScale = new UnstablePrecisionScaleTemplate(),
                Predicate = new UnstableTemplate()
            }
        );
    }

    [Fact]
    public void ValidationContexts_ShouldExposeSharedErrorDefinitionCache()
    {
        var sharedCache = new ValidationErrorDefinitionCache();
        var options = new ValidationContextOptions() with { ErrorDefinitionCache = sharedCache };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();

        context.ErrorDefinitionCache.Should().BeSameAs(sharedCache);
        context.AsReadOnly().ErrorDefinitionCache.Should().BeSameAs(sharedCache);
    }

    [Fact]
    public void EqualTo_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.EqualTo(cache, "ABC").Should().BeSameAs(
            BuiltInValidationErrorDefinitions.EqualTo(cache, "ABC")
        );
    }

    [Fact]
    public void GreaterThan_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.GreaterThan(cache, 18).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.GreaterThan(cache, 18)
        );
    }

    [Fact]
    public void IsIn_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.IsInRange(cache, 1, 10).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.IsInRange(cache, 1, 10)
        );
    }

    [Fact]
    public void IsNotIn_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.IsNotInRange(cache, 1, 10).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.IsNotInRange(cache, 1, 10)
        );
    }

    [Fact]
    public void LessThan_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.LessThan(cache, 10).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.LessThan(cache, 10)
        );
    }

    [Fact]
    public void LessThanOrEqualTo_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.LessThanOrEqualTo(cache, 10).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.LessThanOrEqualTo(cache, 10)
        );
    }

    [Fact]
    public void Matches_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.Matches(cache, "^[0-9]+$", RegexOptions.IgnoreCase).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.Matches(cache, "^[0-9]+$", RegexOptions.IgnoreCase)
        );
    }

    [Fact]
    public void Count_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.Count(cache, 3).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.Count(cache, 3)
        );
    }

    [Fact]
    public void EnumName_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.EnumName<TestStatus>(cache, ignoreCase: true).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.EnumName<TestStatus>(cache, ignoreCase: true)
        );
    }

    [Fact]
    public void PrecisionScale_ShouldBeReused_WhenEquivalent()
    {
        var cache = new ValidationErrorDefinitionCache();
        BuiltInValidationErrorDefinitions.PrecisionScale(cache, 4, 2, ignoreTrailingZeros: true).Should().BeSameAs(
            BuiltInValidationErrorDefinitions.PrecisionScale(cache, 4, 2, ignoreTrailingZeros: true)
        );
    }

    [Fact]
    public void CachedDefinitions_ShouldUseActiveTemplatesAcrossValidationRuns()
    {
        var sharedCache = new ValidationErrorDefinitionCache();
        var firstContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                EqualTo = new PrefixComparableTemplate("First run: ")
            }
        );
        var secondContext = CreateContext(
            sharedCache,
            ValidationErrorTemplates.Default with
            {
                EqualTo = new PrefixComparableTemplate("Second run: ")
            }
        );
        var definition = BuiltInValidationErrorDefinitions.EqualTo(sharedCache, 18);

        firstContext.Check(10, target: "age", displayName: "Age").AddError(definition);
        secondContext.Check(10, target: "age", displayName: "Age").AddError(definition);

        firstContext.ErrorTemplates.MessageCache.Should().NotBeSameAs(secondContext.ErrorTemplates.MessageCache);
        firstContext.Errors[0].Message.Should().Be("First run: Age = 18");
        secondContext.Errors[0].Message.Should().Be("Second run: Age = 18");
    }

    [Fact]
    public void NotNull_IsMessageStable_ShouldBeFalse()
    {
        BuiltInValidationErrorDefinitions.NotNull.IsMessageStable.Should().BeFalse();
    }

    [Fact]
    public void TemplateValidationErrorDefinition_IsMessageStable_ShouldBeFalse_WhenTemplateIsUnstable()
    {
        var unstableTemplate = new UnstableTemplate();
        var unstableDefinition = new TemplateValidationErrorDefinition(unstableTemplate);
        unstableDefinition.IsMessageStable.Should().BeFalse();
    }

    [Fact]
    public void TemplateValidationErrorDefinition_IsMessageStable_ShouldBeTrue_WhenTemplateIsStable()
    {
        var stableTemplate = new ValidationErrorTemplates.Constant("Always invalid");
        var stableDefinition = new TemplateValidationErrorDefinition(stableTemplate);
        stableDefinition.IsMessageStable.Should().BeTrue();
    }

    [Fact]
    public void ParameterizedTemplateValidationErrorDefinition_IsMessageStable_ShouldBeTrue_WhenTemplateIsStable()
    {
        var stableTemplate = new ValidationErrorTemplates.Constant("Always invalid");
        var stableParameterizedDefinition = new TemplateValidationErrorDefinition<int>(
            new ValidationErrorTemplates.IgnoreParameter<int>(stableTemplate),
            5
        );
        stableParameterizedDefinition.IsMessageStable.Should().BeTrue();
    }

    [Fact]
    public void AddErrorDefinition_ShouldUseDefinitionDefaults_WhenNoOverridesAreProvided()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var definition = new TemplateValidationErrorDefinition(
            new ValidationErrorTemplates.Constant("Name must not be empty"),
            code: "NotEmpty",
            metadata: MetadataObject.Create(("minimumLength", 1)),
            category: ErrorCategory.Validation
        );
        var check = context.Check(string.Empty, target: "name", displayName: "Name").NormalizeTargetIfNecessary();

        check.AddError(definition);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Name must not be empty",
                    Code = "NotEmpty",
                    Target = "name",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(("minimumLength", 1))
                }
            )
        );
    }

    [Fact]
    public void AddErrorDefinition_ShouldTreatDefinitionTargetsAsAbsolute()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var childContext = context.ForMember("address", isNormalized: true);
        var definition = new TemplateValidationErrorDefinition(
            new ValidationErrorTemplates.Constant("Zip code is invalid"),
            target: ValidationTarget.Absolute("address.zipCode", isNormalized: true)
        );
        var check = childContext.Check("X", target: "zipCode", displayName: "Zip code").NormalizeTargetIfNecessary();

        check.AddError(definition);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Zip code is invalid",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ModuloValidationErrorDefinition_ShouldProvideExpectedMessage()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var definition = new ModuloValidationErrorDefinition(2);
        context.Check(3, target: "quantity", displayName: "Quantity").AddError(definition);

        context.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "Quantity must be divisible by 2",
                    Code = "Modulo",
                    Target = "quantity",
                    Category = ErrorCategory.Validation,
                    Metadata = MetadataObject.Create(("divisor", 2))
                }
            )
        );
    }

    [Fact]
    public void ValidationErrorMessage_ToError_ShouldCreateExpectedError()
    {
        var validationMessage = new ValidationErrorMessage("Quantity must be divisible by 2", "validation.modulo");
        validationMessage.ToError(code: null, target: "quantity").Should().Be(
            new Error
            {
                Message = "Quantity must be divisible by 2",
                Code = "validation.modulo",
                Target = "quantity",
                Category = ErrorCategory.Validation
            }
        );
    }

    [Fact]
    public void ValidationErrorMessage_Equals_ShouldBeTrue_WhenValuesAreEqual()
    {
        var validationMessage = new ValidationErrorMessage("Quantity must be divisible by 2", "validation.modulo");
        validationMessage.Equals(new ValidationErrorMessage("Quantity must be divisible by 2", "validation.modulo"))
           .Should()
           .BeTrue();
    }

    [Fact]
    public void ValidationErrorMessage_GetHashCode_ShouldBeEqual_WhenValuesAreEqual()
    {
        var validationMessage = new ValidationErrorMessage("Quantity must be divisible by 2", "validation.modulo");
        validationMessage.GetHashCode().Should().Be(
            new ValidationErrorMessage("Quantity must be divisible by 2", "validation.modulo").GetHashCode()
        );
    }

    [Fact]
    public void ValidationErrorMessage_ShouldThrow_WhenTextIsNull()
    {
        Action act = () => _ = new ValidationErrorMessage(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("text");
    }

    [Fact]
    public void ValidationErrorMessage_ShouldThrow_WhenTextIsWhitespace()
    {
        Action act = () => _ = new ValidationErrorMessage("   ");
        act.Should().Throw<ArgumentException>().WithParameterName("text");
    }

    [Fact]
    public void ValidationErrorMessage_Equals_ShouldBeFalse_WhenComparedToString()
    {
        var message = new ValidationErrorMessage("Amount is invalid", "validation.amount.invalid");
        message.Equals(new ValidationErrorMessage("Amount is invalid")).Should().BeFalse();
    }

    [Fact]
    public void ValidationErrorMessage_ToError_ShouldExposeAllValues()
    {
        var metadata = MetadataObject.Create(("source", "validation"));
        var message = new ValidationErrorMessage("Amount is invalid", "validation.amount.invalid");
        message.ToError("AmountInvalid", "amount", ErrorCategory.UnprocessableContent, metadata).Should().Be(
            new Error
            {
                Message = "Amount is invalid",
                Code = "AmountInvalid",
                Target = "amount",
                Category = ErrorCategory.UnprocessableContent,
                Metadata = metadata
            }
        );
    }

    private static ValidationContext CreateContext(
        IValidationErrorDefinitionCache errorDefinitionCache,
        ValidationErrorTemplates errorTemplates
    )
    {
        var options = new ValidationContextOptions() with
        {
            ErrorDefinitionCache = errorDefinitionCache,
            ErrorTemplates = errorTemplates
        };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private enum TestStatus
    {
        // ReSharper disable UnusedMember.Local
        One,

        Two
        // ReSharper restore UnusedMember.Local
    }

    private sealed class CustomValue
    {
        private readonly string _value;

        public CustomValue(string value) => _value = value;

        public override string ToString() => "custom:" + _value;
    }

    private sealed class PrefixComparableTemplate : IComparableValidationErrorMessageTemplate
    {
        private readonly string _prefix;

        public PrefixComparableTemplate(string prefix) => _prefix = prefix;

        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        ) => new (_prefix + context.DisplayName + " = " + parameter);
    }

    private sealed class UnstableTemplate : IValidationErrorMessageTemplate
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new (context.DisplayName + " changed");
    }

    private sealed class UnstableParameterizedTemplate : IValidationErrorMessageTemplate<int>
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context, int parameter) =>
            new (context.DisplayName + " = " + parameter);
    }

    private sealed class UnstableComparableTemplate : IComparableValidationErrorMessageTemplate
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        ) => new (context.DisplayName + " = " + parameter);
    }

    private sealed class UnstableRangeTemplate : IRangeValidationErrorMessageTemplate
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T, TBoundary>(
            in ValidationErrorMessageContext<T> context,
            TBoundary lowerBoundary,
            TBoundary upperBoundary
        ) => new (context.DisplayName + " between " + lowerBoundary + " and " + upperBoundary);
    }

    private sealed class UnstableIntTemplate : IValidationErrorMessageTemplate<int>
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context, int parameter) =>
            new (context.DisplayName + " = " + parameter);
    }

    private sealed class UnstablePrecisionScaleTemplate : IValidationErrorMessageTemplate<PrecisionScaleDescriptor>
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context,
            PrecisionScaleDescriptor parameter
        ) => new (context.DisplayName + " = " + parameter.Precision + ":" + parameter.Scale);
    }

    private sealed class ModuloValidationErrorDefinition : ValidationErrorDefinition<int>
    {
        public ModuloValidationErrorDefinition(int divisor)
            : base(
                divisor,
                code: "Modulo",
                metadata: MetadataObject.Create(("divisor", divisor))
            ) { }

        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new ($"{context.DisplayName} must be divisible by {Parameter}");
    }
}
