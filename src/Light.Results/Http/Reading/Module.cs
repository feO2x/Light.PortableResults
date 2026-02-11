using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Http.Reading.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.Results.Http.Reading;

/// <summary>
/// Provides methods to register services and JSON configuration for Light.Results HTTP reading.
/// </summary>
public static class Module
{
    private static readonly JsonSerializerOptions DefaultSerializerOptions = CreateDefaultSerializerOptions();

    /// <summary>
    /// Registers <see cref="LightResultsHttpReadOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLightResultsHttpReadOptions(this IServiceCollection services)
    {
        services.AddOptions<LightResultsHttpReadOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<LightResultsHttpReadOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.Results HTTP response reading.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    public static void AddDefaultLightResultsHttpReadJsonConverters(this JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        AddConverterIfMissing(
            serializerOptions,
            static () => new HttpReadMetadataObjectJsonConverter()
        );

        AddConverterIfMissing(
            serializerOptions,
            static () => new HttpReadMetadataValueJsonConverter()
        );

        AddConverterIfMissing(
            serializerOptions,
            static () => new HttpReadFailureResultPayloadJsonConverter()
        );

        AddConverterIfMissing(
            serializerOptions,
            static () => new HttpReadSuccessResultPayloadJsonConverter()
        );

        AddConverterIfMissing(
            serializerOptions,
            static () => new HttpReadSuccessResultPayloadJsonConverterFactory()
        );
    }

    /// <summary>
    /// Creates a default <see cref="JsonSerializerOptions" /> instance for HTTP result reading.
    /// </summary>
    /// <returns>A new default serializer options instance.</returns>
    public static JsonSerializerOptions CreateDefaultLightResultsHttpReadJsonSerializerOptions() =>
        CreateDefaultSerializerOptions();

    internal static JsonSerializerOptions ResolveReadSerializerOptions(JsonSerializerOptions? serializerOptions)
    {
        if (serializerOptions is null)
        {
            return DefaultSerializerOptions;
        }

        var clonedOptions = new JsonSerializerOptions(serializerOptions);
        clonedOptions.AddDefaultLightResultsHttpReadJsonConverters();
        return clonedOptions;
    }

    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.AddDefaultLightResultsHttpReadJsonConverters();
        return serializerOptions;
    }

    private static void AddConverterIfMissing<TConverter>(
        JsonSerializerOptions serializerOptions,
        Func<TConverter> createConverter
    )
        where TConverter : JsonConverter
    {
        for (var i = 0; i < serializerOptions.Converters.Count; i++)
        {
            if (serializerOptions.Converters[i] is TConverter)
            {
                return;
            }
        }

        serializerOptions.Converters.Add(createConverter());
    }
}
