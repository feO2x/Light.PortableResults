using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationErrorMessageCachingTests
{
    [Fact]
    public void StableDefinitions_ShouldUseConfiguredMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definitionTemplate = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(definitionTemplate, code: "Required");
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);
        secondContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);

        definitionTemplate.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("Name is required");
        secondContext.Errors[0].Message.Should().Be("Name is required");
    }

    [Fact]
    public void StableTemplateBackedDefinitions_ShouldUseConfiguredMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is invalid");
        var definition = new TemplateValidationErrorDefinition(template);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(definition);
        secondContext.Check("B", target: "code", displayName: "Code").AddError(definition);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("Code is invalid");
        secondContext.Errors[0].Message.Should().Be("Code is invalid");
    }

    [Fact]
    public void UnstableTemplateBackedDefinitions_ShouldBypassMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new UnstableValueTemplate();
        var definition = new TemplateValidationErrorDefinition(template);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(definition);
        secondContext.Check("B", target: "code", displayName: "Code").AddError(definition);

        template.InvocationCount.Should().Be(2);
        cache.TryGetCalls.Should().Be(0);
        cache.StoreCalls.Should().Be(0);
        firstContext.Errors[0].Message.Should().Be("Code = A");
        secondContext.Errors[0].Message.Should().Be("Code = B");
    }

    [Fact]
    public void StableParameterizedTemplateBackedDefinitions_ShouldUseConfiguredMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingParameterizedTemplate();
        var definition = new TemplateValidationErrorDefinition<int>(template, 3);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(definition);
        secondContext.Check("A", target: "code", displayName: "Code").AddError(definition);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
    }

    [Fact]
    public void MessageCacheKey_ShouldIncludeCulture()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition<decimal>(
            new ValidationErrorTemplates.DisplayNameWithParameter<decimal>(
                " must be at least ",
                " EUR"
            ),
            1234.5m
        );
        var germanContext = CreateContext(templates, CultureInfo.GetCultureInfo("de-DE"));
        var englishContext = CreateContext(templates, CultureInfo.GetCultureInfo("en-US"));

        germanContext.Check(1m, target: "amount", displayName: "Amount").AddError(definition);
        englishContext.Check(1m, target: "amount", displayName: "Amount").AddError(definition);

        germanContext.Errors[0].Message.Should().Be("Amount must be at least 1234,5 EUR");
        englishContext.Errors[0].Message.Should().Be("Amount must be at least 1234.5 EUR");
        cache.StoreCalls.Should().Be(2);
    }

    [Fact]
    public void MessageCaching_ShouldBeDisabled_WhenTemplateBackedDefinitionsDoNotExposeCache()
    {
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);
        var templates = new ValidationErrorTemplates { MessageCache = null };
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);
        secondContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);

        template.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void CacheHit_ShouldResolveTargetFromCachedDefinitionEntry()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name").AddError(definition);
        secondContext.Check(string.Empty, target: "name").AddError(definition);

        template.InvocationCount.Should().Be(1);
        firstContext.Errors[0].Target.Should().Be("name");
        secondContext.Errors[0].Target.Should().Be("name");
    }

    [Fact]
    public void CacheHit_ShouldSetResolvedAbsoluteTarget_SoThatSubsequentAssertionsWork()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition(
            new StableCountingTemplate(" is required"),
            code: "Required"
        );
        var context = CreateContext(templates);

        var check = context
           .Check(string.Empty, target: "name")
           .AddError(definition)
           .AddError(definition, code: "Required2");

        check.Target.Should().Be("name");
        context.Errors.Count.Should().Be(2);
        context.Errors[0].Target.Should().Be("name");
        context.Errors[1].Target.Should().Be("name");
    }

    [Fact]
    public void CustomDisplayName_ShouldProduceDifferentDefinitionCacheEntries()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);
        var context = CreateContext(templates);

        context.Check(string.Empty, target: "name", displayName: "First Name").AddError(definition);
        context.Check(string.Empty, target: "name", displayName: "Last Name").AddError(definition);

        template.InvocationCount.Should().Be(2);
        cache.StoreCalls.Should().Be(2);
        context.Errors[0].Message.Should().Be("First Name is required");
        context.Errors[1].Message.Should().Be("Last Name is required");
    }

    [Fact]
    public void NestedValidationScope_ShouldUsePrefixInDefinitionCacheKey()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);
        var rootContext = CreateContext(templates);
        var addressContext = rootContext.ForMember("address", isNormalized: true);
        var billingContext = rootContext.ForMember("billing", isNormalized: true);

        addressContext.Check(string.Empty, target: "city").AddError(definition);
        billingContext.Check(string.Empty, target: "city").AddError(definition);

        template.InvocationCount.Should().Be(2);
        cache.StoreCalls.Should().Be(2);
        rootContext.Errors[0].Target.Should().Be("address.city");
        rootContext.Errors[1].Target.Should().Be("billing.city");
        rootContext.Errors[0].Message.Should().Be("address.city is required");
        rootContext.Errors[1].Message.Should().Be("billing.city is required");
    }

    [Fact]
    public void NestedSamePrefix_ShouldHitDefinitionCacheOnSecondCall()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);
        var firstChild = firstContext.ForMember("address", isNormalized: true);
        var secondChild = secondContext.ForMember("address", isNormalized: true);

        firstChild.Check(string.Empty, target: "city").AddError(definition);
        secondChild.Check(string.Empty, target: "city").AddError(definition);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
    }

    [Fact]
    public void DefinitionTarget_ShouldNotAffectCacheKeyButShouldResolveCorrectly()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition(
            new StableCountingTemplate(" is required"),
            code: "Required",
            target: ValidationTarget.Relative("customTarget", isNormalized: true)
        );
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name").AddError(definition);
        secondContext.Check(string.Empty, target: "name").AddError(definition);

        firstContext.Errors[0].Target.Should().Be("customTarget");
        secondContext.Errors[0].Target.Should().Be("customTarget");
    }

    [Fact]
    public void OverrideTarget_ShouldBeResolvedOnCacheHit()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition(
            new StableCountingTemplate(" is required"),
            code: "Required"
        );
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);
        var overrideTarget = ValidationTarget.Relative("overriddenTarget", isNormalized: true);

        firstContext.Check(string.Empty, target: "name").AddError(definition, target: overrideTarget);
        secondContext.Check(string.Empty, target: "name").AddError(definition, target: overrideTarget);

        firstContext.Errors[0].Target.Should().Be("overriddenTarget");
        secondContext.Errors[0].Target.Should().Be("overriddenTarget");
    }

    [Fact]
    public void NullableDisplayName_ShouldDeriveFromResolvedTargetForTemplateBackedDefinitions()
    {
        var context = CreateContext(new ValidationErrorTemplates());
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);

        context.Check(string.Empty, target: "firstName").AddError(definition);

        context.Errors[0].Message.Should().Be("firstName is required");
        context.Errors[0].Target.Should().Be("firstName");
    }

    [Fact]
    public void NullableDisplayName_InNestedScope_ShouldDeriveFromComposedTargetForTemplateBackedDefinitions()
    {
        var context = CreateContext(new ValidationErrorTemplates());
        var childContext = context.ForMember("address", isNormalized: true);
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);

        childContext.Check(string.Empty, target: "zipCode").AddError(definition);

        context.Errors[0].Message.Should().Be("address.zipCode is required");
        context.Errors[0].Target.Should().Be("address.zipCode");
    }

    [Fact]
    public void ExplicitDisplayName_ShouldOverrideNullDefaultInTemplateBackedDefinitions()
    {
        var context = CreateContext(new ValidationErrorTemplates());
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template);

        context.Check(string.Empty, target: "firstName", displayName: "First Name").AddError(definition);

        context.Errors[0].Message.Should().Be("First Name is required");
        context.Errors[0].Target.Should().Be("firstName");
    }

    [Fact]
    public void CacheHit_ForTemplateBackedDefinition_ShouldBuildErrorDirectly()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is invalid");
        var definition = new TemplateValidationErrorDefinition(template);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code").AddError(definition, code: "Invalid");
        secondContext.Check("B", target: "code").AddError(definition, code: "Invalid");

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("code is invalid");
        secondContext.Errors[0].Message.Should().Be("code is invalid");
        firstContext.Errors[0].Code.Should().Be("Invalid");
        secondContext.Errors[0].Code.Should().Be("Invalid");
    }

    [Fact]
    public void BuiltInComparableDefinitions_ShouldUseComparableTemplateStabilityForCaching()
    {
        var cache = new SpyValidationErrorMessageCache();
        var template = new StableCountingComparableTemplate(" = ");
        var templates = new ValidationErrorTemplates
        {
            MessageCache = cache,
            EqualTo = template
        };
        var definition = BuiltInValidationErrorDefinitions.EqualTo(18);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(10, target: "age", displayName: "Age").AddError(definition);
        secondContext.Check(10, target: "age", displayName: "Age").AddError(definition);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
    }

    [Fact]
    public void BuiltInDefinitions_ShouldKeepMessageCaching_WhenOnlyNonMessageOverridesAreSupplied()
    {
        var cache = new SpyValidationErrorMessageCache();
        var template = new StableCountingComparableTemplate(" = ");
        var templates = new ValidationErrorTemplates
        {
            MessageCache = cache,
            EqualTo = template
        };
        var overrides = new ErrorOverrides
        {
            Code = "AdultRequired",
            Category = ErrorCategory.UnprocessableContent
        };
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(10, target: "age", displayName: "Age").IsEqualTo(18, overrides);
        secondContext.Check(10, target: "age", displayName: "Age").IsEqualTo(18, overrides);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("Age = 18");
        secondContext.Errors[0].Message.Should().Be("Age = 18");
        firstContext.Errors[0].Code.Should().Be("AdultRequired");
        secondContext.Errors[0].Code.Should().Be("AdultRequired");
        firstContext.Errors[0].Category.Should().Be(ErrorCategory.UnprocessableContent);
        secondContext.Errors[0].Category.Should().Be(ErrorCategory.UnprocessableContent);
    }

    [Fact]
    public void BuiltInDefinitions_ShouldBypassMessageCaching_WhenMessageOverridesAreSupplied()
    {
        var cache = new SpyValidationErrorMessageCache();
        var template = new StableCountingComparableTemplate(" = ");
        var templates = new ValidationErrorTemplates
        {
            MessageCache = cache,
            EqualTo = template
        };
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(10, target: "age", displayName: "Age").IsEqualTo(
            18,
            new ErrorOverrides { Message = "Age must be adult" }
        );
        secondContext.Check(10, target: "age", displayName: "Age").IsEqualTo(
            18,
            new ErrorOverrides { Message = "Age must be eighteen or older" }
        );

        template.InvocationCount.Should().Be(0);
        cache.TryGetCalls.Should().Be(0);
        cache.StoreCalls.Should().Be(0);
        firstContext.Errors[0].Message.Should().Be("Age must be adult");
        secondContext.Errors[0].Message.Should().Be("Age must be eighteen or older");
        firstContext.Errors[0].Code.Should().Be("EqualTo");
        secondContext.Errors[0].Code.Should().Be("EqualTo");
    }

    [Fact]
    public void BuiltInRangeDefinitions_ShouldBypassCaching_WhenActiveRangeTemplateIsUnstable()
    {
        var cache = new SpyValidationErrorMessageCache();
        var template = new UnstableRangeTemplate();
        var templates = new ValidationErrorTemplates
        {
            MessageCache = cache,
            IsIn = template
        };
        var definition = BuiltInValidationErrorDefinitions.IsIn(1, 10);
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(0, target: "age", displayName: "Age").AddError(definition);
        secondContext.Check(11, target: "age", displayName: "Age").AddError(definition);

        template.InvocationCount.Should().Be(2);
        cache.TryGetCalls.Should().Be(0);
        cache.StoreCalls.Should().Be(0);
        firstContext.Errors[0].Message.Should().Be("Age = 0, range = 1..10");
        secondContext.Errors[0].Message.Should().Be("Age = 11, range = 1..10");
    }

    [Fact]
    public void TemplateImplementationsAndTemplateBackedDefinitions_ShouldReportStableMessages()
    {
        new ValidationErrorTemplates.DisplayName(" suffix").IsMessageStable.Should().BeTrue();
        new ValidationErrorTemplates.DisplayNameWithComparable(" suffix").IsMessageStable.Should().BeTrue();
        new ValidationErrorTemplates.DisplayNameWithRange(" before ", " and ").IsMessageStable.Should().BeTrue();
        new ValidationErrorTemplates.DisplayNameWithParameter<int>(" before ", " after ").IsMessageStable
           .Should()
           .BeTrue();
        new ValidationErrorTemplates.DisplayNameWithPrecisionScale().IsMessageStable.Should().BeTrue();
        new ValidationErrorTemplates.Constant("Constant").IsMessageStable.Should().BeTrue();
        new ValidationErrorTemplates.IgnoreParameter<int>(
            new ValidationErrorTemplates.Constant("Inner")
        ).IsMessageStable.Should().BeTrue();

        var unstableTemplate = new UnstableValueTemplate();
        var stableDefinition = new TemplateValidationErrorDefinition(
            new ValidationErrorTemplates.Constant("Constant")
        );
        var unstableDefinition = new TemplateValidationErrorDefinition(unstableTemplate);
        var stableParameterizedDefinition = new TemplateValidationErrorDefinition<int>(
            new ValidationErrorTemplates.DisplayNameWithParameter<int>(" before ", " after "),
            5
        );

        stableDefinition.IsMessageStable.Should().BeTrue();
        unstableDefinition.IsMessageStable.Should().BeFalse();
        stableParameterizedDefinition.IsMessageStable.Should().BeTrue();
        new ValueAwareValidationErrorDefinition().IsMessageStable.Should().BeFalse();
        BuiltInValidationErrorDefinitions.NotNull.IsMessageStable.Should().BeFalse();
        BuiltInValidationErrorDefinitions.EqualTo(18).IsMessageStable.Should().BeFalse();
    }

    private static ValidationContext CreateContext(
        ValidationErrorTemplates templates,
        CultureInfo? cultureInfo = null
    )
    {
        var options = ValidationContextOptions.Default with
        {
            ErrorTemplates = templates,
            CultureInfo = cultureInfo ?? CultureInfo.InvariantCulture
        };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private sealed class StableCountingTemplate : IValidationErrorMessageTemplate
    {
        private readonly string _suffix;

        public StableCountingTemplate(string suffix) => _suffix = suffix;

        public int InvocationCount { get; private set; }

        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context)
        {
            InvocationCount++;
            return new ValidationErrorMessage(context.DisplayName + _suffix);
        }
    }

    private sealed class UnstableValueTemplate : IValidationErrorMessageTemplate
    {
        public int InvocationCount { get; private set; }

        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context)
        {
            InvocationCount++;
            return new ValidationErrorMessage(context.DisplayName + " = " + context.Value);
        }
    }

    private sealed class StableCountingParameterizedTemplate : IValidationErrorMessageTemplate<int>
    {
        public int InvocationCount { get; private set; }

        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T>(
            in ValidationErrorMessageContext<T> context,
            int parameter
        )
        {
            InvocationCount++;
            return new ValidationErrorMessage(context.DisplayName + " >= " + parameter);
        }
    }

    private sealed class StableCountingComparableTemplate : IComparableValidationErrorMessageTemplate
    {
        private readonly string _separator;

        public StableCountingComparableTemplate(string separator) => _separator = separator;

        public int InvocationCount { get; private set; }

        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T, TParameter>(
            in ValidationErrorMessageContext<T> context,
            TParameter parameter
        )
        {
            InvocationCount++;
            return new ValidationErrorMessage(context.DisplayName + _separator + parameter);
        }
    }

    private sealed class UnstableRangeTemplate : IRangeValidationErrorMessageTemplate
    {
        public int InvocationCount { get; private set; }

        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T, TBoundary>(
            in ValidationErrorMessageContext<T> context,
            TBoundary lowerBoundary,
            TBoundary upperBoundary
        )
        {
            InvocationCount++;
            return new ValidationErrorMessage(
                context.DisplayName +
                " = " +
                context.Value +
                ", range = " +
                lowerBoundary +
                ".." +
                upperBoundary
            );
        }
    }

    private sealed class ValueAwareValidationErrorDefinition : ValidationErrorDefinition
    {
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new (context.DisplayName + " = " + context.Value);
    }

    private sealed class SpyValidationErrorMessageCache : IValidationErrorMessageCache
    {
        private readonly Dictionary<ValidationErrorMessageCacheKey, CachedValidationErrorMessage> _messages = new ();

        public int TryGetCalls { get; private set; }
        public int StoreCalls { get; private set; }

        public bool TryGet(ValidationErrorMessageCacheKey key, out CachedValidationErrorMessage entry)
        {
            TryGetCalls++;
            return _messages.TryGetValue(key, out entry);
        }

        public void Store(ValidationErrorMessageCacheKey key, CachedValidationErrorMessage entry)
        {
            StoreCalls++;
            _messages[key] = entry;
        }
    }
}
