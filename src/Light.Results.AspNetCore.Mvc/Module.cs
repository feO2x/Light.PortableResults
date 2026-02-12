using Light.Results.Http.Writing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Results.AspNetCore.Mvc;

/// <summary>
/// Service registration helpers for Light.Results MVC.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers all services required for Light.Results MVC.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultsForMvc(this IServiceCollection services) =>
        services
           .AddLightResultHttpWriteOptions()
           .AddLightResultsHttpHeaderConversionService()
           .ConfigureMvcJsonOptionsForLightResults();

    /// <summary>
    /// Configures JSON options for Light.Results MVC responses.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureMvcJsonOptionsForLightResults(this IServiceCollection services)
    {
        services
           .AddOptions<JsonOptions>()
           .Configure<LightResultsHttpWriteOptions>(
                (jsonOptions, lightResultOptions) =>
                {
                    jsonOptions.JsonSerializerOptions.AddDefaultLightResultsHttpWriteJsonConverters(lightResultOptions);
                }
            );
        return services;
    }
}
