using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Light.PortableResults.Validation.Definitions;
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
        var customizedOptions = new ValidationContextOptions() with
        {
            ValueNormalizer = NoOpValueNormalizer.Instance,
            ErrorTemplates = customizedTemplates
        };

        var defaultContext = DefaultValidationContextFactory.Create().CreateValidationContext();
        string? defaultValue = null;
        var defaultCheck = defaultContext.Check(defaultValue);

        var customizedContext = new DefaultValidationContextFactory(customizedOptions).CreateValidationContext();
        string? customizedValue = null;
        var customizedCheck = customizedContext.Check(customizedValue);

        defaultCheck.Value.Should().BeEmpty();
        customizedCheck.Value.Should().BeNull();
        new ValidationContextOptions().ValueNormalizer.Should().BeSameAs(TrimStringNormalizer.Instance);
        new ValidationContextOptions().ErrorTemplates.Should().BeSameAs(ValidationErrorTemplates.Default);
        ValidationErrorTemplates.Default.NotNull.Should().NotBeSameAs(customizedTemplates.NotNull);
    }

    [Fact]
    public void AutomaticNullProvider_ShouldAllowDisablingAutomaticNullErrors()
    {
        var options = new ValidationContextOptions() with
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
        var options = new ValidationContextOptions() with
        {
            AutomaticNullErrorProvider = new ContextAwareAutomaticNullErrorProvider(TenantKey)
        };
        var factory = new DefaultValidationContextFactory(options);
        var context = factory.CreateValidationContext();
        context.SetItem(TenantKey, "checkout");
        var validator = new NullToEmptyStringValidator(factory);

        var result = validator.Validate(null, context, target: "request", displayName: "Request");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(
                new Error
                {
                    Message = "checkout: Request is required",
                    Code = "MissingValue",
                    Target = string.Empty,
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void MessageTemplates_ShouldSupportReferenceAndValueTypeContexts()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
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
        var options = new ValidationContextOptions() with
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
    public void SpecificBuiltInTemplateHelpers_ShouldBehaveAsExpected()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext = context.Check(12.34m, target: "amount", displayName: "Amount").CreateMessageContext();
        var constant = new ValidationErrorTemplates.Constant("Always invalid", "validation.constant");
        var precisionScale = new ValidationErrorTemplates.DisplayNameWithPrecisionScale();
        var ignored = new ValidationErrorTemplates.IgnoreParameter<int>(constant);

        constant.ProvideMessage(messageContext).Should()
           .Be(new ValidationErrorMessage("Always invalid", "validation.constant"));
        precisionScale.ProvideMessage(messageContext, new PrecisionScaleDescriptor(4, 2, true)).Text.Should().Be(
            "Amount must not be more than 4 digits in total, with allowance for 2 decimals when trailing decimal zeros are ignored"
        );
        ignored.ProvideMessage(messageContext, 5).Should()
           .Be(new ValidationErrorMessage("Always invalid", "validation.constant"));
        constant.IsMessageStable.Should().BeTrue();
        precisionScale.IsMessageStable.Should().BeTrue();
        ignored.IsMessageStable.Should().BeTrue();
    }

    [Fact]
    public void TemplateBackedDefinitions_ShouldSupportLocalizationAndTranslationKeys()
    {
        var options = new ValidationContextOptions() with
        {
            CultureInfo = CultureInfo.GetCultureInfo("de-DE"),
            ErrorTemplates = ValidationErrorTemplates.Default with
            {
                NotNullOrWhiteSpace = new LocalizedRequiredTemplate()
            }
        };
        var context = new DefaultValidationContextFactory(options).CreateValidationContext();
        var check = context.Check(" ", target: "firstName", displayName: "Vorname").NormalizeTargetIfNecessary();
        var definition = new TemplateValidationErrorDefinition(context.ErrorTemplates.NotNullOrWhiteSpace);

        check.AddError(definition);
        var error = context.Errors[0];

        error.Message.Should().Be("Vorname ist erforderlich");
        error.Code.Should().Be("validation.person.firstName.required");
        error.Target.Should().Be("firstName");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void AddErrorDefinition_ShouldSkipMessageGeneration_WhenCheckIsShortCircuited()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var template = new CountingTemplate();
        var definition = new TemplateValidationErrorDefinition(template);
        var check = context.Check("Alice", target: "firstName", displayName: "First name").ShortCircuit();

        var updatedCheck = check.AddError(definition);

        template.InvocationCount.Should().Be(0);
        updatedCheck.IsShortCircuited.Should().BeTrue();
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void GetRequiredItem_ShouldReturnSharedValue_ForMutableAndReadOnlyContexts()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.SetItem(TenantKey, "checkout");

        context.GetRequiredItem(TenantKey).Should().Be("checkout");
        context.AsReadOnly().GetRequiredItem(TenantKey).Should().Be("checkout");
    }

    [Fact]
    public void GetRequiredItem_ShouldThrowKeyNotFoundException_WhenItemIsMissing()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

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
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();

        context.SetItem(stringKey1, "checkout");
        context.SetItem(intKey, 42);

        context.TryGetItem(stringKey2, out var stringValue).Should().BeTrue();
        stringValue.Should().Be("checkout");
        context.TryGetItem(intKey, out var intValue).Should().BeTrue();
        intValue.Should().Be(42);
        stringKey1.ToString().Should().Contain("tenant");
    }

    [Fact]
    public void ValidationContextKeys_WithSameNameAndDifferentType_ShouldNotBeEqual()
    {
        var stringKey = new ValidationContextKey<string>("tenant");
        var intKey = new ValidationContextKey<int>("tenant");

        stringKey.Should().NotBe(intKey);
        Action nullNameAct = () => _ = new ValidationContextKey<string>(null!);
        Action whitespaceNameAct = () => _ = new ValidationContextKey<string>(" ");

        nullNameAct.Should().Throw<ArgumentNullException>();
        whitespaceNameAct.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValueNormalizersAndNoOpNullProvider_ShouldExposeSingletonBehaviors()
    {
        ValueNormalizers.Trim.Should().BeSameAs(TrimStringNormalizer.Instance);
        ValueNormalizers.NoOp.Should().BeSameAs(NoOpValueNormalizer.Instance);

        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var messageContext =
            context.Check<string?>("Alice", target: "name", displayName: "Name").CreateMessageContext();

        NoOpAutomaticNullErrorProvider.Instance.TryCreateError(messageContext, out var error).Should().BeFalse();
        error.IsDefaultInstance.Should().BeTrue();
    }

    [Fact]
    public void ValidationErrorMessageContext_ShouldValidateConstructorArguments_AndExposeValues()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var readOnlyContext = context.AsReadOnly();

        var messageContext = new ValidationErrorMessageContext<string>(
            readOnlyContext,
            "Name",
            "name",
            "Alice"
        );

        messageContext.ValidationContext.Should().Be(readOnlyContext);
        messageContext.DisplayName.Should().Be("Name");
        messageContext.Target.Should().Be("name");
        messageContext.Value.Should().Be("Alice");

        Action defaultContextAct =
            () => _ = new ValidationErrorMessageContext<string>(default, "Name", "name", "Alice");
        Action nullDisplayNameAct = () =>
            _ = new ValidationErrorMessageContext<string>(readOnlyContext, null!, "name", "Alice");
        Action nullTargetAct = () =>
            _ = new ValidationErrorMessageContext<string>(readOnlyContext, "Name", null!, "Alice");

        defaultContextAct.Should().Throw<ArgumentException>().WithParameterName("validationContext");
        nullDisplayNameAct.Should().Throw<ArgumentNullException>().WithParameterName("displayName");
        nullTargetAct.Should().Throw<ArgumentNullException>().WithParameterName("target");
    }

    [Fact]
    public void DefaultReadOnlyContextAndDefaultCheckpoint_ShouldThrow_WhenAccessed()
    {
        ReadOnlyValidationContext readOnlyContext = default;
        ValidationCheckpoint checkpoint = default;

        Action readOnlyAct = () => _ = readOnlyContext.TargetPrefix;
        Action checkpointAct = () => _ = checkpoint.HasNewErrors;

        readOnlyAct.Should().Throw<InvalidOperationException>().WithMessage("*must not be the default instance*");
        checkpointAct.Should().Throw<InvalidOperationException>().WithMessage("*must not be the default instance*");
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
