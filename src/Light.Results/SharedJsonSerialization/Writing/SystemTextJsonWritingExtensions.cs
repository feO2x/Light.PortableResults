using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Light.Results.SharedJsonSerialization.Writing;

/// <summary>
/// Provides transport-agnostic JSON serialization helpers used across Light.Results integrations.
/// </summary>
public static class SystemTextJsonWritingExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="JsonTypeInfo" /> has known polymorphism information
    /// that allows the serializer to handle type resolution without requiring runtime type checks.
    /// This method is copied from the .NET internal type Microsoft.AspNetCore.Http.JsonSerializerExtensions.
    /// </summary>
    /// <param name="jsonTypeInfo">The JSON type information to check.</param>
    /// <returns>
    /// <see langword="true" /> if the type is sealed, a value type, or has explicit polymorphism options configured;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool HasKnownPolymorphism(this JsonTypeInfo jsonTypeInfo) =>
        jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null;

    /// <summary>
    /// Determines whether the specified <see cref="JsonTypeInfo" /> should be used to serialize
    /// an object of the given runtime type.
    /// This method is copied from the .NET internal type Microsoft.AspNetCore.Http.JsonSerializerExtensions.
    /// </summary>
    /// <param name="jsonTypeInfo">The JSON type information to evaluate.</param>
    /// <param name="runtimeType">The runtime type of the object to be serialized, or <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> if the <paramref name="runtimeType" /> is <see langword="null" />,
    /// matches the <see cref="JsonTypeInfo.Type" />, or the type has known polymorphism;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool ShouldUseWith(this JsonTypeInfo jsonTypeInfo, [NotNullWhen(false)] Type? runtimeType) =>
        runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.HasKnownPolymorphism();

    /// <summary>
    /// Writes a generic value using <see cref="Utf8JsonWriter" /> and serializer metadata from <paramref name="options" />.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when serialization metadata for the runtime type is missing.</exception>
    public static void WriteGenericValue<T>(this Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var valueTypeInfo = options.GetTypeInfo(typeof(T));
        var runtimeType = value.GetType();
        if (valueTypeInfo.ShouldUseWith(runtimeType))
        {
            ((JsonConverter<T>) valueTypeInfo.Converter).Write(writer, value, options);
            return;
        }

        if (!options.TryGetTypeInfo(runtimeType, out valueTypeInfo))
        {
            throw new InvalidOperationException(
                $"No JSON serialization metadata was found for type '{runtimeType}' - please ensure that JsonOptions are configured properly"
            );
        }

        JsonSerializer.Serialize(writer, value, valueTypeInfo);
    }
}
