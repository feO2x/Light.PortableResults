using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Http.Writing;
using Light.Results.Http.Writing.Headers;
using Light.Results.Http.Writing.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
           .AddLightResultOptions()
           .AddLightResultsHttpHeaderConversionService()
           .ConfigureMinimalApiJsonOptionsForLightResults();

    /// <summary>
    /// Registers <see cref="LightResultsHttpWriteOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsHttpWriteOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsHttpWriteOptions>>().Value);
        return services;
    }

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
                    jsonOptions.SerializerOptions.AddDefaultLightResultsJsonConverters(lightResultOptions);
                }
            );
        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.Results.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <param name="options">The Light.Results options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public static void AddDefaultLightResultsJsonConverters(
        this JsonSerializerOptions serializerOptions,
        LightResultsHttpWriteOptions options
    )
    {
        serializerOptions.Converters.Add(new HttpWriteMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new HttpWriteMetadataValueJsonConverter());
        serializerOptions.Converters.Add(new HttpWriteResultJsonConverter(options));
        serializerOptions.Converters.Add(new HttpWriteResultJsonConverterFactory(options));
    }

    /// <summary>
    /// Registers the HTTP header conversion service and converter registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="metadataKeyComparer">Optional metadata key comparer.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple converters register the same metadata key.
    /// </exception>
    public static IServiceCollection AddLightResultsHttpHeaderConversionService(
        this IServiceCollection services,
        IEqualityComparer<string>? metadataKeyComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, HttpHeaderConverter>>(
            sp =>
            {
                var httpHeaderConverters = sp.GetServices<HttpHeaderConverter>();
                var dictionary = new Dictionary<string, HttpHeaderConverter>(metadataKeyComparer);
                foreach (var converter in httpHeaderConverters)
                {
                    foreach (var supportedKey in converter.SupportedMetadataKeys)
                    {
                        if (dictionary.TryAdd(supportedKey, converter))
                        {
                            continue;
                        }

                        var existingConverter = dictionary[supportedKey];
                        throw new InvalidOperationException(
                            $"Cannot add '{converter}' to frozen dictionary because key '{supportedKey}' is already registered by '{existingConverter}'"
                        );
                    }
                }

                return metadataKeyComparer is null ?
                    dictionary.ToFrozenDictionary() :
                    dictionary.ToFrozenDictionary(metadataKeyComparer);
            }
        );
        services.AddSingleton<IHttpHeaderConversionService, DefaultHttpHeaderConversionService>();

        return services;
    }
}
