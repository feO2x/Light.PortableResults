using System;
using FluentAssertions;
using Light.PortableResults.Validation.ConfigurationIntegration;
using Xunit;

namespace Light.PortableResults.Validation.Tests.ConfigurationIntegration;

public sealed class PortableResultsValidateOptionsTests
{
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenValidatorIsNull()
    {
        Action act = () => _ = new PortableResultsValidateOptions<TestOptions>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validator");
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenValidationPasses()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);
        var options = new TestOptions { ConnectionString = "Server=localhost", TimeoutSeconds = 30 };

        var result = adapter.Validate(name: null, options);

        result.Succeeded.Should().BeTrue();
        result.Failed.Should().BeFalse();
        result.Skipped.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldIncludeErrorMessagesInFailResult()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new AlwaysFailValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);
        var options = new TestOptions();

        var result = adapter.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle().Which.Should().Be("Always fails");
    }

    [Fact]
    public void Validate_ShouldIncludeAllErrorMessages_WhenMultipleValidationErrorsExist()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);
        var options = new TestOptions { ConnectionString = "", TimeoutSeconds = 0 };

        var result = adapter.Validate(name: null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);

        Action act = () => adapter.Validate(name: null, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Validate_ShouldReturnSkip_WhenAdapterHasNameAndIncomingNameDoesNotMatch()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator, name: "SpecificName");
        var options = new TestOptions { ConnectionString = "test", TimeoutSeconds = 1 };

        var result = adapter.Validate(name: "DifferentName", options);

        result.Skipped.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenAdapterHasNameAndIncomingNameMatches()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator, name: "SpecificName");
        var options = new TestOptions { ConnectionString = "Server=localhost", TimeoutSeconds = 30 };

        var result = adapter.Validate(name: "SpecificName", options);

        result.Succeeded.Should().BeTrue();
        result.Skipped.Should().BeFalse();
    }

    [Theory]
    [InlineData("AnyName")]
    [InlineData(null)]
    public void Validate_ShouldReturnSuccess_WhenAdapterHasNoName(string? incomingName)
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new ValidatingTestOptionsValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator, name: null);
        var options = new TestOptions { ConnectionString = "Server=localhost", TimeoutSeconds = 30 };

        var result = adapter.Validate(name: incomingName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldForwardOptionsName_ToValidationContext()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new OptionsNameCapturingValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);
        var options = new TestOptions { ConnectionString = "test", TimeoutSeconds = 1 };

        adapter.Validate(name: "MyOptionsName", options);

        validator.CapturedOptionsName.Should().Be("MyOptionsName");
    }

    [Fact]
    public void Validate_ShouldForwardNullOptionsName_ToValidationContext()
    {
        var factory = DefaultValidationContextFactory.Create();
        var validator = new OptionsNameCapturingValidator(factory);
        var adapter = new PortableResultsValidateOptions<TestOptions>(validator);
        var options = new TestOptions { ConnectionString = "test", TimeoutSeconds = 1 };

        adapter.Validate(name: null, options);

        validator.CapturedOptionsName.Should().BeNull();
    }

    [Fact]
    public void OptionsNameKey_ShouldBeAccessibleAsPublicStaticField()
    {
        var key = ConfigurationConstants.OptionsNameKey;

        key.Should().NotBeNull();
        key.Name.Should().Be("OptionsName");
    }

    private sealed class TestOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
    }

    private sealed class ValidatingTestOptionsValidator : Validator<TestOptions>
    {
        public ValidatingTestOptionsValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<TestOptions> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            TestOptions value
        )
        {
            context.Check(value.ConnectionString).IsNotNullOrWhiteSpace();
            context.Check(value.TimeoutSeconds).IsGreaterThan(0);
            return checkpoint.HasNewErrors ?
                ValidatedValue<TestOptions>.NoValue :
                ValidatedValue<TestOptions>.Success(value);
        }
    }

    private sealed class AlwaysFailValidator : Validator<TestOptions>
    {
        public AlwaysFailValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<TestOptions> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            TestOptions value
        )
        {
            context.AddError("Always fails");
            return checkpoint.ToValidatedValue(value);
        }
    }

    private sealed class OptionsNameCapturingValidator : Validator<TestOptions>
    {
        public OptionsNameCapturingValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        public string? CapturedOptionsName { get; private set; }

        protected override ValidatedValue<TestOptions> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            TestOptions value
        )
        {
            if (context.TryGetItem(ConfigurationConstants.OptionsNameKey, out var name))
            {
                CapturedOptionsName = name;
            }

            return ValidatedValue<TestOptions>.Success(value);
        }
    }
}
