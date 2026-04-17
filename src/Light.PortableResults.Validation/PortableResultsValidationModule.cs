using System;
using Light.PortableResults.Validation.ConfigurationIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides integration into Microsoft.Extensions.DependencyInjection for Light.PortableResults.Validation.
/// </summary>
public static class PortableResultsValidationModule
{
    /// <summary>
    /// Adds Light.PortableResults.Validation services to the specified <see cref="IServiceCollection" />.
    /// Specifically, <see cref="IValidationContextFactory" /> is registered with <see cref="DefaultValidationContextFactory" />
    /// as the implementation type and a singleton lifetime. <see cref="ValidationContextOptions" /> are added as options,
    /// they are also made available as a singleton directly so that you don't need to resolve `IOptions&lt;ValidationContextOptions>`.
    /// </summary>
    /// <param name="services">The service collection that holds all registrations.</param>
    /// <returns>The service collection for method-chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddValidationForPortableResults(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IValidationContextFactory, DefaultValidationContextFactory>();
        services.AddOptions<ValidationContextOptions>();
        services.TryAddSingleton<ValidationContextOptions>(
            sp => sp.GetRequiredService<IOptions<ValidationContextOptions>>().Value
        );
        return services;
    }

    /// <summary>
    /// Registers a <see cref="Validator{T}" /> to validate options using the <see cref="IValidateOptions{TOptions}" />
    /// pipeline from Microsoft.Extensions.Options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TValidator">The validator type that implements <see cref="Validator{TOptions}" />.</typeparam>
    /// <param name="builder">The options builder to configure.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}" /> for further chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public static OptionsBuilder<TOptions> ValidateWithPortableResults<TOptions, TValidator>(
        this OptionsBuilder<TOptions> builder
    )
        where TOptions : class
        where TValidator : Validator<TOptions>
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddValidationForPortableResults();
        builder.Services.TryAddSingleton<TValidator>();

        // This must be an AddSingleton call, not TryAddSingleton. Several registrations
        // for the same TOptions must be possible, with or without different names.
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(
            sp => new PortableResultsValidateOptions<TOptions>(
                sp.GetRequiredService<TValidator>(),
                builder.Name
            )
        );

        return builder;
    }
}
