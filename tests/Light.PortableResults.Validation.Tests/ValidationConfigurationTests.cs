using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Light.PortableResults.Validation.Messaging;
using Light.PortableResults.Validation.Normalization;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationConfigurationTests
{
    private static readonly ValidationContextKey<string> TenantKey = new ("tenant");

    [Fact]
    public void DefaultInstances_ShouldRemainSafeToReuse_WhenCustomizedCopiesAreCreated()
    {
        var customizedTemplates = ValidationErrorTemplates.Default with
        {
            NotNull = new ValidationErrorTemplates.Constant("Custom null message")
        };
        var customizedOptions = ValidationContextOptions.Default with
        {
            ValueNormalizer = NoOpValueNormalizer.Instance,
            ErrorTemplates = customizedTemplates
        };

        var defaultContext = new DefaultValidationContextFactory().CreateValidationContext();
        string? defaultValue = null;
        var defaultCheck = defaultContext.Check(defaultValue);

        var customizedContext = new DefaultValidationContextFactory(customizedOptions).CreateValidationContext();
        string? customizedValue = null;
        var customizedCheck = customizedContext.Check(customizedValue);

        defaultCheck.Value.Should().BeEmpty();
        customizedCheck.Value.Should().BeNull();
        ValidationContextOptions.Default.ValueNormalizer.Should().BeSameAs(TrimStringNormalizer.Instance);
        ValidationContextOptions.Default.ErrorTemplates.Should().BeSameAs(ValidationErrorTemplates.Default);
        ValidationErrorTemplates.Default.NotNull.Should().NotBeSameAs(customizedTemplates.NotNull);
    }

    [Fact]
    public void AutomaticNullProvider_ShouldAllowDisablingAutomaticNullErrors()
    {
        var options = ValidationContextOptions.Default with
        {
            AutomaticNullErrorProvider = NoOpAutomaticNullErrorProvider.Instance
        };
        var validator = new NullToEmptyStringValidator(new DefaultValidationContextFactory(options));

        var result = validator.Validate(null);

        result.Should().Be(Result<string?>.Ok(string.Empty));
    }

    [Fact]
    public void AutomaticNullProvider_ShouldReadSharedItemsThroughReadOnlyContext()
    {
        var options = ValidationContextOptions.Default with
        {
            AutomaticNullErrorProvider = new ContextAwareAutomaticNullErrorProvider(TenantKey)
        };
        var factory = new DefaultValidationContextFactory(options);
        var context = factory.CreateValidationContext();
        context.SetItem(TenantKey, "checkout");
        var validator = new NullToEmptyStringValidator(factory);

        var result = validator.Validate(null, context, target: "request", displayName: "Request");

        result.IsValid.Should().BeFalse();
        var expectedErrors = new Errors(
            new Error
            {
                Message = "checkout: Request is required",
                Code = "MissingValue",
                Target = string.Empty,
                Category = ErrorCategory.Validation
            }
        );
        result.Errors.Should().Equal(expectedErrors);
    }

    [Fact]
    public void MessageTemplates_ShouldSupportReferenceAndValueTypeContexts()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var template = new EchoValueTemplate();

        var stringMessage = template.ProvideMessage(
            context.Check("Alice", target: "firstName", displayName: "First name").CreateMessageContext()
        );
        var intMessage = template.ProvideMessage(
            context.Check(42, target: "age", displayName: "Age").CreateMessageContext()
        );

        stringMessage.Text.Should().Be("First name=Alice@firstName");
        intMessage.Text.Should().Be("Age=42@age");
    }

    [Fact]
    public void BuiltInParameterizedTemplates_ShouldUseConfiguredCultureFromReadOnlyContext()
    {
        var options = ValidationContextOptions.Default with
        {
            CultureInfo = CultureInfo.GetCultureInfo("de-DE")
        };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();
        var template = new ValidationErrorTemplates.DisplayNameWithParameter<decimal>(
            " must be at least ",
            " EUR"
        );

        var message = template.ProvideMessage(
            context.Check(25m, target: "amount", displayName: "Amount").CreateMessageContext(),
            1234.5m
        );

        message.Text.Should().Be("Amount must be at least 1234,5 EUR");
    }

    [Fact]
    public void MessageTemplates_ShouldSupportLocalizationAndTranslationKeys()
    {
        var options = ValidationContextOptions.Default with
        {
            CultureInfo = CultureInfo.GetCultureInfo("de-DE"),
            ErrorTemplates = ValidationErrorTemplates.Default with
            {
                NotNullOrWhiteSpace = new LocalizedRequiredTemplate()
            }
        };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();
        var check = context.Check(" ", target: "firstName", displayName: "Vorname").NormalizeTargetIfNecessary();

        check.AddError(context.ErrorTemplates.NotNullOrWhiteSpace);
        var error = context.Errors[0];

        error.Message.Should().Be("Vorname ist erforderlich");
        error.Code.Should().Be("validation.person.firstName.required");
        error.Target.Should().Be("firstName");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void AddErrorTemplate_ShouldSkipMessageGeneration_WhenCheckIsShortCircuited()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();
        var template = new CountingTemplate();
        var check = context.Check("Alice", target: "firstName", displayName: "First name").ShortCircuit();

        var updatedCheck = check.AddError(template);

        template.InvocationCount.Should().Be(0);
        updatedCheck.IsShortCircuited.Should().BeTrue();
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void GetRequiredItem_ShouldReturnSharedValue_ForMutableAndReadOnlyContexts()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.SetItem(TenantKey, "checkout");

        context.GetRequiredItem(TenantKey).Should().Be("checkout");
        context.AsReadOnly().GetRequiredItem(TenantKey).Should().Be("checkout");
    }

    [Fact]
    public void GetRequiredItem_ShouldThrowKeyNotFoundException_WhenItemIsMissing()
    {
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        Action mutableAccess = () => context.GetRequiredItem(TenantKey);
        Action readOnlyAccess = () => context.AsReadOnly().GetRequiredItem(TenantKey);

        mutableAccess.Should().Throw<KeyNotFoundException>()
           .WithMessage("*tenant*");
        readOnlyAccess.Should().Throw<KeyNotFoundException>()
           .WithMessage("*tenant*");
    }

    [Fact]
    public void ValidationContextKeys_ShouldShareItemsByNameAndType()
    {
        var stringKey1 = new ValidationContextKey<string>("tenant");
        var stringKey2 = new ValidationContextKey<string>("tenant");
        var intKey = new ValidationContextKey<int>("tenant");
        var context = new DefaultValidationContextFactory().CreateValidationContext();

        context.SetItem(stringKey1, "checkout");
        context.SetItem(intKey, 42);

        context.TryGetItem(stringKey2, out var stringValue).Should().BeTrue();
        stringValue.Should().Be("checkout");
        context.TryGetItem(intKey, out var intValue).Should().BeTrue();
        intValue.Should().Be(42);
    }

    [Fact]
    public void ValidationContextKeys_WithSameNameAndDifferentType_ShouldNotBeEqual()
    {
        var stringKey = new ValidationContextKey<string>("tenant");
        var intKey = new ValidationContextKey<int>("tenant");

        stringKey.Should().NotBe(intKey);
    }

    private sealed class NullToEmptyStringValidator : Validator<string?>
    {
        public NullToEmptyStringValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<string?> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            string? value
        ) => checkpoint.ToValidatedValue<string?>(value ?? string.Empty);
    }

    private sealed class ContextAwareAutomaticNullErrorProvider : IAutomaticNullErrorProvider
    {
        private readonly ValidationContextKey<string> _tenantKey;

        public ContextAwareAutomaticNullErrorProvider(ValidationContextKey<string> tenantKey) => _tenantKey = tenantKey;

        public bool TryCreateError<T>(in ValidationErrorMessageContext<T> context, out Error error)
        {
            context.ValidationContext.TryGetItem(_tenantKey, out var tenant).Should().BeTrue();
            error = new ValidationErrorMessage($"{tenant}: {context.DisplayName} is required")
               .ToError("MissingValue", context.Target);
            return true;
        }
    }

    private sealed class EchoValueTemplate : IValidationErrorMessageTemplate
    {
        public bool IsMessageStable => false;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context) =>
            new ($"{context.DisplayName}={context.Value}@{context.Target}");
    }

    private sealed class LocalizedRequiredTemplate : IValidationErrorMessageTemplate
    {
        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context)
        {
            var isGerman = string.Equals(
                context.ValidationContext.Options.CultureInfo.TwoLetterISOLanguageName,
                "de",
                StringComparison.Ordinal
            );
            return new ValidationErrorMessage(
                isGerman ? $"{context.DisplayName} ist erforderlich" : $"{context.DisplayName} is required",
                "validation.person.firstName.required"
            );
        }
    }

    private sealed class CountingTemplate : IValidationErrorMessageTemplate
    {
        public int InvocationCount { get; private set; }

        public bool IsMessageStable => true;

        public ValidationErrorMessage ProvideMessage<T>(in ValidationErrorMessageContext<T> context)
        {
            InvocationCount++;
            return new ValidationErrorMessage("Should not be created");
        }
    }
}
