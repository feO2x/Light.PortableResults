using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Http.Writing.Headers;
using Light.Results.Http.Writing.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.Results.Http.Writing;

/// <summary>
/// Provides methods to register services required for Light.Results HTTP writing.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers <see cref="LightResultsHttpWriteOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultHttpWriteOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsHttpWriteOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsHttpWriteOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.Results.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <param name="options">The Light.Results options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serializerOptions" /> or <paramref name="options" /> are <see langword="null" />.
    /// </exception>
    public static void AddDefaultLightResultsHttpWriteJsonConverters(
        this JsonSerializerOptions serializerOptions,
        LightResultsHttpWriteOptions options
    )
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

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
                        try
                        {
                            dictionary.Add(supportedKey, converter);
                        }
                        catch (ArgumentException argumentException)
                        {
                            var existingConverter = dictionary[supportedKey];
                            throw new InvalidOperationException(
                                $"Cannot add '{converter}' to frozen dictionary because key '{supportedKey}' is already registered by '{existingConverter}'",
                                argumentException
                            );
                        }
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
