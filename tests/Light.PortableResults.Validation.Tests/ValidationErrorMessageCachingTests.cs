using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
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
    public void StableParameterlessTemplates_ShouldUseConfiguredMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is invalid");
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(template);
        secondContext.Check("B", target: "code", displayName: "Code").AddError(template);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("Code is invalid");
        secondContext.Errors[0].Message.Should().Be("Code is invalid");
    }

    [Fact]
    public void UnstableTemplates_ShouldBypassMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new UnstableValueTemplate();
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(template);
        secondContext.Check("B", target: "code", displayName: "Code").AddError(template);

        template.InvocationCount.Should().Be(2);
        cache.TryGetCalls.Should().Be(0);
        cache.StoreCalls.Should().Be(0);
        firstContext.Errors[0].Message.Should().Be("Code = A");
        secondContext.Errors[0].Message.Should().Be("Code = B");
    }

    [Fact]
    public void ParameterizedTemplateOverload_ShouldBypassMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingParameterizedTemplate();
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check("A", target: "code", displayName: "Code").AddError(template, 3);
        secondContext.Check("A", target: "code", displayName: "Code").AddError(template, 3);

        template.InvocationCount.Should().Be(2);
        cache.TryGetCalls.Should().Be(0);
        cache.StoreCalls.Should().Be(0);
    }

    [Fact]
    public void MessageCacheKey_ShouldIncludeCulture()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition<decimal>(
            new DisplayNameWithParameterValidationErrorMessageTemplate<decimal>(
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
    public void MessageCaching_ShouldBeDisabled_WhenTemplatesDoNotExposeCache()
    {
        var template = new StableCountingTemplate(" is required");
        var templates = new ValidationErrorTemplates { MessageCache = null };
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name", displayName: "Name").AddError(template);
        secondContext.Check(string.Empty, target: "name", displayName: "Name").AddError(template);

        template.InvocationCount.Should().Be(2);
    }

    [Fact]
    public void BuiltInTemplatesAndDefinitions_ShouldReportStableMessages()
    {
        new DisplayNameValidationErrorMessageTemplate(" suffix").IsMessageStable.Should().BeTrue();
        new DisplayNameWithComparableValidationErrorMessageTemplate(" suffix").IsMessageStable.Should().BeTrue();
        new DisplayNameWithRangeValidationErrorMessageTemplate(" before ", " and ").IsMessageStable.Should().BeTrue();
        new DisplayNameWithParameterValidationErrorMessageTemplate<int>(" before ", " after ").IsMessageStable
           .Should()
           .BeTrue();
        new DisplayNameWithPrecisionScaleValidationErrorMessageTemplate().IsMessageStable.Should().BeTrue();
        new ConstantValidationErrorMessageTemplate("Constant").IsMessageStable.Should().BeTrue();
        new IgnoreParameterValidationErrorMessageTemplate<int>(
            new ConstantValidationErrorMessageTemplate("Inner")
        ).IsMessageStable.Should().BeTrue();

        var unstableTemplate = new UnstableValueTemplate();
        var stableDefinition = new TemplateValidationErrorDefinition(
            new ConstantValidationErrorMessageTemplate("Constant")
        );
        var unstableDefinition = new TemplateValidationErrorDefinition(unstableTemplate);
        var stableParameterizedDefinition = new TemplateValidationErrorDefinition<int>(
            new DisplayNameWithParameterValidationErrorMessageTemplate<int>(" before ", " after "),
            5
        );

        BuiltInValidationErrorDefinitions.NotNull.IsMessageStable.Should().BeTrue();
        BuiltInValidationErrorDefinitions.EqualTo(18).IsMessageStable.Should().BeTrue();
        stableDefinition.IsMessageStable.Should().BeTrue();
        unstableDefinition.IsMessageStable.Should().BeFalse();
        stableParameterizedDefinition.IsMessageStable.Should().BeTrue();
        new ValueAwareValidationErrorDefinition().IsMessageStable.Should().BeFalse();
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

    private sealed class ValueAwareValidationErrorDefinition : ValidationErrorDefinition
    {
        public override ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new (context.DisplayName + " = " + context.Value);
    }

    private sealed class SpyValidationErrorMessageCache : IValidationErrorMessageCache
    {
        private readonly Dictionary<ValidationErrorMessageCacheKey, ValidationErrorMessage> _messages = new ();

        public int TryGetCalls { get; private set; }
        public int StoreCalls { get; private set; }

        public bool TryGet(ValidationErrorMessageCacheKey key, out ValidationErrorMessage message)
        {
            TryGetCalls++;
            return _messages.TryGetValue(key, out message);
        }

        public void Store(ValidationErrorMessageCacheKey key, ValidationErrorMessage message)
        {
            StoreCalls++;
            _messages[key] = message;
        }
    }
}
