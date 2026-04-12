using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ModuleTests
{
    [Fact]
    public void AddValidationForLightPortableResults_ShouldThrowArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddValidationForLightPortableResults();

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldReturnSameServiceCollection_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var returnedServices = services.AddValidationForLightPortableResults();

        returnedServices.Should().BeSameAs(services);
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldRegisterIValidationContextFactory_AsSingletonDefaultValidationContextFactory()
    {
        using var provider = CreateDefaultProvider();

        var factory1 = provider.GetRequiredService<IValidationContextFactory>();
        var factory2 = provider.GetRequiredService<IValidationContextFactory>();

        factory1.Should().BeOfType<DefaultValidationContextFactory>();
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldRegisterValidationContextOptions_AsSingleton()
    {
        using var provider = CreateDefaultProvider();

        var options1 = provider.GetRequiredService<ValidationContextOptions>();
        var options2 = provider.GetRequiredService<ValidationContextOptions>();

        options1.Should().BeSameAs(options2);
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldMakeDirectValidationContextOptions_ConsistentWithIOptions()
    {
        using var provider = CreateDefaultProvider();

        var directOptions = provider.GetRequiredService<ValidationContextOptions>();
        var optionsFromIOptions = provider.GetRequiredService<IOptions<ValidationContextOptions>>().Value;

        directOptions.Should().BeSameAs(optionsFromIOptions);
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldRegisterDefaultValidationContextOptions()
    {
        using var provider = CreateDefaultProvider();

        var options = provider.GetRequiredService<ValidationContextOptions>();

        options.Should().Be(new ValidationContextOptions());
    }

    [Fact]
    public void AddValidationForLightPortableResults_ShouldAllowCreatingFunctionalValidationContexts()
    {
        using var provider = CreateDefaultProvider();
        var factory = provider.GetRequiredService<IValidationContextFactory>();

        var context = factory.CreateValidationContext();
        context.Check(string.Empty, target: "name", displayName: "Name").IsNotNullOrWhiteSpace();

        context.HasErrors.Should().BeTrue();
        context.Errors.Should().ContainSingle(e => e.Target == "name");
    }

    private static ServiceProvider CreateDefaultProvider() =>
        new ServiceCollection()
            .AddValidationForLightPortableResults()
            .BuildServiceProvider();
}
