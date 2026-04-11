using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationCachingWorkflowTests
{
    [Fact]
    public void StableTemplateBackedDefinitions_ShouldReuseCachedMessagesAcrossRuns()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new StableCountingTemplate(" is required");
        var definition = new TemplateValidationErrorDefinition(template, code: "Required");
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);
        secondContext.Check(string.Empty, target: "name", displayName: "Name").AddError(definition);

        template.InvocationCount.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        cache.StoreCalls.Should().Be(1);
        firstContext.Errors[0].Message.Should().Be("Name is required");
        secondContext.Errors[0].Message.Should().Be("Name is required");
    }

    [Fact]
    public void UnstableTemplateBackedDefinitions_ShouldBypassMessageCache()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var template = new ValueAwareTemplate();
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
    public void MessageCaching_ShouldBeCultureSensitive()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition<decimal>(
            new ValidationErrorTemplates.DisplayNameWithParameter<decimal>(" must be at least ", " EUR"),
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
    public void CacheHits_ShouldStillMaterializeCorrectTargets()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition(
            new StableCountingTemplate(" is required"),
            code: "Required"
        );
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.ForMember("address", isNormalized: true)
           .Check(string.Empty, target: "zipCode")
           .AddError(definition);
        secondContext.ForMember("address", isNormalized: true)
           .Check(string.Empty, target: "zipCode")
           .AddError(definition);

        firstContext.Errors[0].Target.Should().Be("address.zipCode");
        secondContext.Errors[0].Target.Should().Be("address.zipCode");
        firstContext.Errors[0].Message.Should().Be("address.zipCode is required");
        secondContext.Errors[0].Message.Should().Be("address.zipCode is required");
    }

    [Fact]
    public void DisplayNameChanges_ShouldProduceDistinctCachedMessages()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definition = new TemplateValidationErrorDefinition(new StableCountingTemplate(" is required"));
        var context = CreateContext(templates);

        context.Check(string.Empty, target: "name", displayName: "First name").AddError(definition);
        context.Check(string.Empty, target: "name", displayName: "Last name").AddError(definition);

        cache.StoreCalls.Should().Be(2);
        context.Errors[0].Message.Should().Be("First name is required");
        context.Errors[1].Message.Should().Be("Last name is required");
    }

    [Fact]
    public void CacheHits_ShouldResolveDefinitionAndOverrideTargets_WhenMessagesAreReused()
    {
        var cache = new SpyValidationErrorMessageCache();
        var templates = new ValidationErrorTemplates { MessageCache = cache };
        var definitionTarget = ValidationTarget.Relative("detail", isNormalized: true);
        var definition = new TemplateValidationErrorDefinition(
            new StableCountingTemplate(" is invalid"),
            target: definitionTarget
        );
        var firstContext = CreateContext(templates);
        var secondContext = CreateContext(templates);

        firstContext.ForMember("address", isNormalized: true)
           .Check("1234X", target: "zipCode", displayName: "Zip code")
           .AddError(definition);
        secondContext.ForMember("address", isNormalized: true)
           .Check("1234X", target: "zipCode", displayName: "Zip code")
           .AddError(definition, target: ValidationTarget.Relative("override", isNormalized: true));

        cache.StoreCalls.Should().Be(1);
        cache.TryGetCalls.Should().Be(2);
        firstContext.Errors[0].Target.Should().Be("address.detail");
        secondContext.Errors[0].Target.Should().Be("address.override");
        firstContext.Errors[0].Message.Should().Be("Zip code is invalid");
        secondContext.Errors[0].Message.Should().Be("Zip code is invalid");
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

    private sealed class ValueAwareTemplate : IValidationErrorMessageTemplate
    {
        public int InvocationCount { get; private set; }

        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context)
        {
            InvocationCount++;
            return new ValidationErrorMessage(context.DisplayName + " = " + context.Value);
        }
    }

    private sealed class SpyValidationErrorMessageCache : IValidationErrorMessageCache
    {
        private readonly Dictionary<ValidationErrorMessageCacheKey, CachedValidationErrorMessage> _entries = new ();

        public int TryGetCalls { get; private set; }

        public int StoreCalls { get; private set; }

        public bool TryGet(ValidationErrorMessageCacheKey key, out CachedValidationErrorMessage cachedMessage)
        {
            TryGetCalls++;
            return _entries.TryGetValue(key, out cachedMessage);
        }

        public void Store(ValidationErrorMessageCacheKey key, CachedValidationErrorMessage cachedMessage)
        {
            StoreCalls++;
            _entries[key] = cachedMessage;
        }
    }
}
