using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidateWithPortableResultsTests
{
    [Fact]
    public void ValidateWithPortableResults_ShouldThrowArgumentNullException_WhenBuilderIsNull()
    {
        OptionsBuilder<TestOptions> builder = null!;

        Action act = () => builder.ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        act.Should().Throw<ArgumentNullException>().WithParameterName("builder");
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldRegisterValidatorAsSingleton()
    {
        CustomTestOptionsValidator.InstanceCount = 0;
        var services = new ServiceCollection();

        services
           .AddOptions<TestOptions>()
           .ValidateWithPortableResults<TestOptions, CustomTestOptionsValidator>();

        var provider = services.BuildServiceProvider();

        // Resolve twice to verify singleton
        _ = provider.GetService<CustomTestOptionsValidator>();
        _ = provider.GetService<CustomTestOptionsValidator>();

        CustomTestOptionsValidator.InstanceCount.Should().Be(1);
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();
        var builder = services.AddOptions<TestOptions>();

        var returnedBuilder = builder.ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        returnedBuilder.Should().BeSameAs(builder);
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldBeIdempotent_WhenCalledMultipleTimesForSameOptions()
    {
        CustomTestOptionsValidator.InstanceCount = 0;
        var services = new ServiceCollection();

        services
           .AddOptions<TestOptions>()
           .ValidateWithPortableResults<TestOptions, CustomTestOptionsValidator>();

        services
           .AddOptions<TestOptions>()
           .ValidateWithPortableResults<TestOptions, CustomTestOptionsValidator>();

        var provider = services.BuildServiceProvider();
        var validateOptions = provider.GetServices<IValidateOptions<TestOptions>>().ToList();

        validateOptions.Should().HaveCount(1);
        CustomTestOptionsValidator.InstanceCount.Should().Be(1);
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldNotOverridePreRegisteredValidator()
    {
        var services = new ServiceCollection();
        var customInstance = new TestOptionsValidator(DefaultValidationContextFactory.Create());

        services.AddSingleton(customInstance);
        services
           .AddOptions<TestOptions>()
           .ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        var provider = services.BuildServiceProvider();
        var registeredValidator = provider.GetRequiredService<TestOptionsValidator>();

        registeredValidator.Should().BeSameAs(customInstance);
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldValidateOptions_WhenResolvedFromContainer()
    {
        var services = new ServiceCollection();

        services
           .AddOptions<TestOptions>()
           .Configure(
                o =>
                {
                    o.ConnectionString = "";
                    o.TimeoutSeconds = 0;
                }
            )
           .ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TestOptions>>();

        // Accessing Value triggers validation
        Action act = () => _ = options.Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldSucceed_WhenOptionsAreValid()
    {
        var services = new ServiceCollection();

        services
           .AddOptions<TestOptions>()
           .Configure(
                o =>
                {
                    o.ConnectionString = "Server=localhost";
                    o.TimeoutSeconds = 30;
                }
            )
           .ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<TestOptions>>();

        var value = options.Value;

        value.ConnectionString.Should().Be("Server=localhost");
        value.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void ValidateWithPortableResults_ShouldCaptureOptionsBuilderName_ForFiltering()
    {
        var services = new ServiceCollection();

        services
           .AddOptions<TestOptions>("NamedInstance")
           .Configure(
                o =>
                {
                    o.ConnectionString = "";
                    o.TimeoutSeconds = 0;
                }
            )
           .ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        // Also register unnamed options with valid values
        services
           .AddOptions<TestOptions>()
           .Configure(
                o =>
                {
                    o.ConnectionString = "Server=localhost";
                    o.TimeoutSeconds = 30;
                }
            )
           .ValidateWithPortableResults<TestOptions, TestOptionsValidator>();

        var provider = services.BuildServiceProvider();

        // Unnamed options should be valid
        var unnamedOptions = provider.GetRequiredService<IOptions<TestOptions>>();
        var unnamedValue = unnamedOptions.Value;
        unnamedValue.ConnectionString.Should().Be("Server=localhost");

        // Named options should fail validation (invalid values)
        var namedSnapshot = provider.GetRequiredService<IOptionsSnapshot<TestOptions>>();
        Action act = () => _ = namedSnapshot.Get("NamedInstance");

        act.Should().Throw<OptionsValidationException>();
    }

    private sealed class TestOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
    }

    private sealed class TestOptionsValidator : Validator<TestOptions>
    {
        public TestOptionsValidator(IValidationContextFactory validationContextFactory)
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

    private sealed class CustomTestOptionsValidator : Validator<TestOptions>
    {
        public CustomTestOptionsValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory)
        {
            InstanceCount++;
        }

        public static int InstanceCount { get; set; }

        protected override ValidatedValue<TestOptions> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            TestOptions value
        )
        {
            return ValidatedValue<TestOptions>.Success(value);
        }
    }
}
