using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.PortableResults.CloudEvents.Writing.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.CloudEvents.Writing;

/// <summary>
/// Provides methods to register services required for CloudEvents writing.
/// </summary>
public static class Module
{
    /// <summary>
    /// Gets the default serializer options used by Light.PortableResults CloudEvents writing.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Registers <see cref="PortableResultsCloudEventsWriteOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortableResultsCloudEventsWriteOptions(this IServiceCollection services)
    {
        services.AddOptions<PortableResultsCloudEventsWriteOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PortableResultsCloudEventsWriteOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Registers the CloudEvents attribute conversion service and converter registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="metadataKeyComparer">Optional metadata key comparer.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple converters register the same metadata key.
    /// </exception>
    public static IServiceCollection AddPortableResultsCloudEventsAttributeConversionService(
        this IServiceCollection services,
        IEqualityComparer<string>? metadataKeyComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, CloudEventsAttributeConverter>>(
            sp =>
            {
                var converters = sp.GetServices<CloudEventsAttributeConverter>();
                var dictionary = new Dictionary<string, CloudEventsAttributeConverter>(metadataKeyComparer);
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
        services.AddSingleton<ICloudEventsAttributeConversionService, DefaultCloudEventsAttributeConversionService>(
            sp =>
            {
                var converters = sp.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeConverter>>();
                return new DefaultCloudEventsAttributeConversionService(converters);
            }
        );

        return services;
    }

    /// <summary>
    /// Registers all CloudEvents write JSON converters on the specified <see cref="JsonSerializerOptions" />.
    /// </summary>
    /// <param name="serializerOptions">The serializer options to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializerOptions" /> is <see langword="null" />.</exception>
    public static void AddDefaultPortableResultsCloudEventsWriteJsonConverters(
        this JsonSerializerOptions serializerOptions
    )
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        serializerOptions.Converters.Add(new CloudEventsMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new CloudEventsEnvelopeForWritingJsonConverter());
        serializerOptions.Converters.Add(new CloudEventsEnvelopeForWritingJsonConverterFactory());
    }

    /// <summary>
    /// Creates a default <see cref="JsonSerializerOptions" /> instance for CloudEvents writing.
    /// </summary>
    /// <returns>A new default serializer options instance.</returns>
    public static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();
        return serializerOptions;
    }
}
