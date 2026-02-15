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
    /// Gets the default serializer options used by Light.Results CloudEvent reading.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Registers <see cref="LightResultsCloudEventReadOptions" /> in the service container.
    /// </summary>
    public static IServiceCollection AddLightResultsCloudEventReadOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsCloudEventReadOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsCloudEventReadOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Registers the CloudEvent attribute parsing service and parser registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="attributeNameComparer">Optional attribute name comparer.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple parsers register the same extension attribute name.
    /// </exception>
    public static IServiceCollection AddLightResultsCloudEventAttributeParsingService(
        this IServiceCollection services,
        IEqualityComparer<string>? attributeNameComparer = null
    )
    {
        services.TryAddSingleton<FrozenDictionary<string, CloudEventAttributeParser>>(
            sp =>
            {
                var parsers = sp.GetServices<CloudEventAttributeParser>();
                var dictionary = new Dictionary<string, CloudEventAttributeParser>(attributeNameComparer);
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
        services.AddSingleton<ICloudEventAttributeParsingService, DefaultCloudEventAttributeParsingService>(
            sp =>
            {
                var parsers = sp.GetRequiredService<FrozenDictionary<string, CloudEventAttributeParser>>();
                return new DefaultCloudEventAttributeParsingService(parsers);
            }
        );

        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.Results CloudEvent reading.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializerOptions" /> is <c>null</c>.</exception>
    public static void AddDefaultLightResultsCloudEventReadJsonConverters(this JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        serializerOptions.Converters.Add(new CloudEventEnvelopePayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventFailurePayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventSuccessPayloadJsonConverter());
        serializerOptions.Converters.Add(new CloudEventSuccessPayloadJsonConverterFactory());
    }

    /// <summary>
    /// Creates a default <see cref="JsonSerializerOptions" /> instance for CloudEvent reading.
    /// </summary>
    /// <returns>A new default serializer options instance.</returns>
    public static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.AddDefaultLightResultsCloudEventReadJsonConverters();
        return serializerOptions;
    }
}
