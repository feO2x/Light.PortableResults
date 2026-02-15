using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.CloudEvents.Writing.Json;
using Light.Results.Http.Writing.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.Results.CloudEvents.Writing;

/// <summary>
/// Provides methods to register services required for CloudEvents writing.
/// </summary>
public static class Module
{
    /// <summary>
    /// Registers <see cref="LightResultsCloudEventWriteOptions" /> in the service container.
    /// </summary>
    public static IServiceCollection AddLightResultsCloudEventWriteOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsCloudEventWriteOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsCloudEventWriteOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Registers the CloudEvent attribute conversion service and converter registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="metadataKeyComparer">Optional metadata key comparer.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple converters register the same metadata key.
    /// </exception>
    public static IServiceCollection AddLightResultsCloudEventAttributeConversionService(
        this IServiceCollection services,
        IEqualityComparer<string>? metadataKeyComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, CloudEventAttributeConverter>>(
            sp =>
            {
                var converters = sp.GetServices<CloudEventAttributeConverter>();
                var dictionary = new Dictionary<string, CloudEventAttributeConverter>(metadataKeyComparer);
                foreach (var converter in converters)
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
        services.AddSingleton<ICloudEventAttributeConversionService, DefaultCloudEventAttributeConversionService>(
            sp =>
            {
                var converters = sp.GetRequiredService<FrozenDictionary<string, CloudEventAttributeConverter>>();
                return new DefaultCloudEventAttributeConversionService(converters);
            }
        );

        return services;
    }

    /// <summary>
    /// Registers all CloudEvents write JSON converters on the specified <see cref="JsonSerializerOptions" />.
    /// </summary>
    /// <param name="serializerOptions">The serializer options to configure.</param>
    /// <param name="options">The CloudEvent write options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serializerOptions" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public static void AddDefaultLightResultsCloudEventWriteJsonConverters(
        this JsonSerializerOptions serializerOptions,
        LightResultsCloudEventWriteOptions options
    )
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        serializerOptions.Converters.Add(new HttpWriteMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new HttpWriteMetadataValueJsonConverter());
        serializerOptions.Converters.Add(new CloudEventWriteResultJsonConverter(options));
        serializerOptions.Converters.Add(new CloudEventWriteResultJsonConverterFactory(options));
    }
}
