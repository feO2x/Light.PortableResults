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
    public static IServiceCollection AddLightResultsForMinimalApis(this IServiceCollection services) =>
        services
           .AddPortableResultHttpWriteOptions()
           .AddPortableResultsHttpHeaderConversionService()
           .ConfigureMinimalApiJsonOptionsForLightResults();

    /// <summary>
    /// Configures JSON options for Light.PortableResults Minimal API responses.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMinimalApiJsonOptionsForLightResults(this IServiceCollection services)
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
