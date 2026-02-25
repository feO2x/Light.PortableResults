using System;
using System.Text.Json;
using Light.PortableResults.Http.Reading.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Light.PortableResults.Http.Reading;

/// <summary>
/// Provides methods to register services and JSON configuration for Light.PortableResults HTTP reading.
/// </summary>
public static class Module
{
    /// <summary>
    /// Gets the default serializer options used by Light.PortableResults HTTP response reading.
    /// </summary>
    public static JsonSerializerOptions DefaultSerializerOptions { get; } = CreateDefaultSerializerOptions();

    /// <summary>
    /// Registers <see cref="PortableResultsHttpReadOptions" /> in the service container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPortableResultsHttpReadOptions(this IServiceCollection services)
    {
        services.AddOptions<PortableResultsHttpReadOptions>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<PortableResultsHttpReadOptions>>().Value);
        return services;
    }

    /// <summary>
    /// Adds the default JSON converters used by Light.PortableResults HTTP response reading.
    /// </summary>
    /// <param name="serializerOptions">The JSON serializer options to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializerOptions" /> is <c>null</c>.</exception>
    public static void AddDefaultPortableResultsHttpReadJsonConverters(this JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        serializerOptions.Converters.Add(new HttpReadMetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new HttpReadMetadataValueJsonConverter());
        serializerOptions.Converters.Add(new HttpReadFailureResultPayloadJsonConverter());
        serializerOptions.Converters.Add(new HttpReadSuccessResultPayloadJsonConverter());
        serializerOptions.Converters.Add(new HttpReadSuccessResultPayloadJsonConverterFactory());
    }

    /// <summary>
    /// Creates a default <see cref="JsonSerializerOptions" /> instance for HTTP result reading.
    /// </summary>
    /// <returns>A new default serializer options instance.</returns>
    public static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.AddDefaultPortableResultsHttpReadJsonConverters();
        return serializerOptions;
    }
}
