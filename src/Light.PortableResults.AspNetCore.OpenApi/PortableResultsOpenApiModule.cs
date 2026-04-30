using System;
using System.Linq;
using Light.PortableResults.AspNetCore.OpenApi.ErrorContracts;
using Light.PortableResults.AspNetCore.OpenApi.Generation;
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
    /// Registers the Light.PortableResults OpenAPI document transformer and optional global error metadata contracts.
    /// </summary>
    public static IServiceCollection AddPortableResultsOpenApi(
        this IServiceCollection services,
        Action<PortableErrorMetadataContractsBuilder>? configure = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<PortableResultsOpenApiDocumentTransformer>();
        RegisterErrorMetadataContractRegistry(services);
        if (configure is not null)
        {
            services.Configure<PortableErrorMetadataContractsOptions>(options => configure(options.Builder));
        }
        else
        {
            services.AddOptions<PortableErrorMetadataContractsOptions>();
        }

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

    private static void RegisterErrorMetadataContractRegistry(IServiceCollection services)
    {
        services.TryAddSingleton<IPortableErrorMetadataContractRegistry>(
            static serviceProvider =>
                new DefaultPortableErrorMetadataContractRegistry(
                    serviceProvider.GetRequiredService<IOptions<PortableErrorMetadataContractsOptions>>().Value.Builder
                )
        );
    }

    private sealed class PortableResultsOpenApiRegistrationGate;
}
