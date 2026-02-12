using Light.Results.Http.Writing;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Results.AspNetCore.MinimalApis;

/// <summary>
/// Service registration helpers for Light.Results Minimal APIs.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers all services required for Light.Results Minimal APIs.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultsForMinimalApis(this IServiceCollection services) =>
        services
           .AddLightResultHttpWriteOptions()
           .AddLightResultsHttpHeaderConversionService()
           .ConfigureMinimalApiJsonOptionsForLightResults();

    /// <summary>
    /// Configures JSON options for Light.Results Minimal API responses.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMinimalApiJsonOptionsForLightResults(this IServiceCollection services)
    {
        services
           .AddOptions<JsonOptions>()
           .Configure<LightResultsHttpWriteOptions>(
                (jsonOptions, lightResultOptions) =>
                {
                    jsonOptions.SerializerOptions.AddDefaultLightResultsHttpWriteJsonConverters(lightResultOptions);
                }
            );
        return services;
    }
}
