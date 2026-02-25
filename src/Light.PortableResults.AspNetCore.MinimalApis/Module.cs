using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Light.PortableResults.AspNetCore.MinimalApis;

/// <summary>
/// Service registration helpers for Light.PortableResults Minimal APIs.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers all services required for Light.PortableResults Minimal APIs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortableResultsForMinimalApis(this IServiceCollection services) =>
        services
           .AddPortableResultHttpWriteOptions()
           .AddPortableResultsHttpHeaderConversionService()
           .ConfigureMinimalApiJsonOptionsForPortableResults();

    /// <summary>
    /// Configures JSON options for Light.PortableResults Minimal API responses.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMinimalApiJsonOptionsForPortableResults(this IServiceCollection services)
    {
        services
           .AddOptions<JsonOptions>()
           .Configure(
                jsonOptions =>
                {
                    jsonOptions.SerializerOptions.AddDefaultPortableResultsHttpWriteJsonConverters();
                }
            );
        return services;
    }
}
