using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Http.Writing.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.Http.Writing;

/// <summary>
/// Provides methods to register services required for Light.PortableResults HTTP writing.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers <see cref="PortableResultsHttpWriteOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultHttpWriteOptions(this IServiceCollection services)
    {
        services.AddOptions<PortableResultsHttpWriteOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PortableResultsHttpWriteOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.PortableResults.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serializerOptions" /> is <see langword="null" />.
    /// </exception>
    public static void AddDefaultLightResultsHttpWriteJsonConverters(
        this JsonSerializerOptions serializerOptions
    )
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        serializerOptions.Converters.Add(new HttpWriteMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new HttpWriteMetadataValueJsonConverter());
        serializerOptions.Converters.Add(new HttpResultForWritingJsonConverter());
        serializerOptions.Converters.Add(new HttpResultForWritingJsonConverterFactory());
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
