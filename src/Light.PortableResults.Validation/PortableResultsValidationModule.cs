using System;
using System.Diagnostics.CodeAnalysis;
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
    /// Registers the <see cref="IValidationContextFactory" /> and <see cref="ValidationContextOptions" /> as singletons
    /// to the DI container when they have not been registered already.
    /// </summary>
    /// <param name="services">The service collection that holds all registrations.</param>
    /// <param name="createOptions">
    /// The optional delegate that creates the default <see cref="ValidationContextOptions" /> instance. The created
    /// options are registered as a singleton (as the <see cref="IValidationContextFactory" /> itself is registered
    /// as a singleton).
    /// </param>
    /// <returns>The service collection for method-chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services" /> is null.</exception>
    public static IServiceCollection AddValidationForPortableResults(
        this IServiceCollection services,
        Func<IServiceProvider, ValidationContextOptions>? createOptions = null
    )
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IValidationContextFactory, DefaultValidationContextFactory>();
        if (createOptions is null)
        {
            services.TryAddSingleton<ValidationContextOptions>();
        }
        else
        {
            services.TryAddSingleton(createOptions);
        }

        return services;
    }

    /// <summary>
    /// Registers a <see cref="Validator{T}" /> to validate options using the <see cref="IValidateOptions{TOptions}" />
    /// pipeline from Microsoft.Extensions.Options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    /// <typeparam name="TValidator">
    /// The validator type that implements <see cref="Validator{TOptions}" />. It is instantiated by the DI container,
    /// so its public constructors must be preserved when trimming.
    /// </typeparam>
    /// <param name="builder">The options builder to configure.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}" /> for further chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public static OptionsBuilder<TOptions> ValidateWithPortableResults<
        TOptions,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(
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
