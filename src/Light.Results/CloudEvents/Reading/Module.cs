using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.CloudEvents.Reading.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Provides methods to register services required for CloudEvents reading.
/// </summary>
public static class Module
{
    /// <summary>
    /// Gets the default serializer options used by Light.Results CloudEvents reading.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Registers <see cref="LightResultsCloudEventsReadOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultsCloudEventsReadOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsCloudEventsReadOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsCloudEventsReadOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Registers the CloudEvents attribute parsing service and parser registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="attributeNameComparer">Optional attribute name comparer.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple parsers register the same extension attribute name.
    /// </exception>
    public static IServiceCollection AddLightResultsCloudEventsAttributeParsingService(
        this IServiceCollection services,
        IEqualityComparer<string>? attributeNameComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, CloudEventsAttributeParser>>(
            sp =>
            {
                var parsers = sp.GetServices<CloudEventsAttributeParser>();
                var dictionary = new Dictionary<string, CloudEventsAttributeParser>(attributeNameComparer);
                foreach (var parser in parsers)
                {
                    foreach (var supportedAttribute in parser.SupportedAttributeNames)
                    {
                        try
                        {
                            dictionary.Add(supportedAttribute, parser);
                        }
                        catch (ArgumentException argumentException)
                        {
                            var existingParser = dictionary[supportedAttribute];
                            throw new InvalidOperationException(
                                $"Cannot add '{parser}' to frozen dictionary because key '{supportedAttribute}' is already registered by '{existingParser}'",
                                argumentException
                            );
                        }
                    }
                }

                return attributeNameComparer is null ?
                    dictionary.ToFrozenDictionary(StringComparer.Ordinal) :
                    dictionary.ToFrozenDictionary(attributeNameComparer);
            }
        );
        services.AddSingleton<ICloudEventsAttributeParsingService, DefaultCloudEventsAttributeParsingService>(
            sp =>
            {
                var parsers = sp.GetRequiredService<FrozenDictionary<string, CloudEventsAttributeParser>>();
                return new DefaultCloudEventsAttributeParsingService(parsers);
            }
        );

        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.Results CloudEvents reading.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializerOptions" /> is <c>null</c>.</exception>
    public static void AddDefaultLightResultsCloudEventsReadJsonConverters(this JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        serializerOptions.Converters.Add(new CloudEventsEnvelopePayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventsFailurePayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventsSuccessPayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventsSuccessPayloadJsonConverterFactory());
    }

    /// <summary>
    /// Creates a default <see cref="JsonSerializerOptions" /> instance for CloudEvents reading.
    /// </summary>
    /// <returns>A new default serializer options instance.</returns>
    public static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.AddDefaultLightResultsCloudEventsReadJsonConverters();
        return serializerOptions;
    }
}
