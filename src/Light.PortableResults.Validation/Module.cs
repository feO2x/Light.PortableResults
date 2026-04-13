using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.Validation;

/// <summary>
/// Provides integration into Microsoft.Extensions.DependencyInjection for Light.PortableResults.Validation.
/// </summary>
public static class Module
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

        services.AddSingleton<IValidationContextFactory, DefaultValidationContextFactory>();
        services.AddOptions<ValidationContextOptions>();
        services.AddSingleton<ValidationContextOptions>(
            sp => sp.GetRequiredService<IOptions<ValidationContextOptions>>().Value
        );
        return services;
    }
}
