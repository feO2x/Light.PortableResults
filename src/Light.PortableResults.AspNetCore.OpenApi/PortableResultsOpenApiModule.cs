using System;
using System.Linq;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.AspNetCore.OpenApi;

/// <summary>
/// Service registration helpers for Light.PortableResults OpenAPI support.
/// </summary>
public static class PortableResultsOpenApiModule
{
    /// <summary>
    /// Registers the Light.PortableResults OpenAPI document transformer.
    /// </summary>
    public static IServiceCollection AddPortableResultsOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<PortableResultsOpenApiDocumentTransformer>();
        services.TryAddSingleton<IPortableErrorMetadataContractRegistry>(
            static serviceProvider =>
                new PortableErrorMetadataContractRegistry(
                    serviceProvider.GetRequiredService<IOptions<PortableErrorMetadataContractsOptions>>().Value.Builder
                )
        );

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(PortableResultsOpenApiRegistrationGate)))
        {
            return services;
        }

        services.AddSingleton<PortableResultsOpenApiRegistrationGate>();
        services.ConfigureAll<OpenApiOptions>(
            static options => options.AddDocumentTransformer<PortableResultsOpenApiDocumentTransformer>()
        );
        return services;
    }

    /// <summary>
    /// Registers global error-code metadata contracts that endpoints can opt into.
    /// </summary>
    public static IServiceCollection ConfigureErrorMetadataContracts(
        this IServiceCollection services,
        Action<PortableErrorMetadataContractsBuilder> configure
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure<PortableErrorMetadataContractsOptions>(options => configure(options.Builder));
        services.TryAddSingleton<IPortableErrorMetadataContractRegistry>(
            static serviceProvider =>
                new PortableErrorMetadataContractRegistry(
                    serviceProvider.GetRequiredService<IOptions<PortableErrorMetadataContractsOptions>>().Value.Builder
                )
        );
        return services;
    }

    private sealed class PortableResultsOpenApiRegistrationGate;
}
