using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Serialization;

namespace Light.Results.Http;

/// <summary>
/// Provides cached <see cref="JsonSerializerOptions" /> instances tuned for the different
/// <see cref="PreferSuccessPayload" /> strategies used when reading HTTP responses.
/// </summary>
public static class HttpReadJsonSerializerOptionsCache
{
    /// <summary>
    /// Gets serializer options that automatically decide how to interpret success payloads.
    /// </summary>
    public static JsonSerializerOptions Auto { get; } = Create(PreferSuccessPayload.Auto);

    /// <summary>
    /// Gets serializer options configured for bare value success payloads.
    /// </summary>
    public static JsonSerializerOptions BareValue { get; } = Create(PreferSuccessPayload.BareValue);

    /// <summary>
    /// Gets serializer options configured for wrapped value success payloads.
    /// </summary>
    public static JsonSerializerOptions WrappedValue { get; } = Create(PreferSuccessPayload.WrappedValue);

    /// <summary>
    /// Retrieves the cached serializer options that match the given <paramref name="preference" />.
    /// </summary>
    /// <param name="preference">The preferred representation for successful HTTP payloads.</param>
    /// <returns>The cached <see cref="JsonSerializerOptions" /> for the preference.</returns>
    public static JsonSerializerOptions GetByPreference(PreferSuccessPayload preference) =>
        preference switch
        {
            PreferSuccessPayload.BareValue => BareValue,
            PreferSuccessPayload.WrappedValue => WrappedValue,
            _ => Auto
        };

    /// <summary>
    /// Creates serializer options with the converters required for reading HTTP results.
    /// </summary>
    /// <param name="preference">The payload preference passed to <see cref="HttpReadResultJsonConverterFactory" />.</param>
    /// <returns>A configured <see cref="JsonSerializerOptions" /> instance.</returns>
    private static JsonSerializerOptions Create(PreferSuccessPayload preference)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        serializerOptions.Converters.Add(new MetadataObjectJsonConverter());
        serializerOptions.Converters.Add(new MetadataValueJsonConverter());
        serializerOptions.Converters.Add(new HttpReadResultJsonConverter());
        serializerOptions.Converters.Add(new HttpReadResultJsonConverterFactory(preference));

        return serializerOptions;
    }
}
