using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Metadata;

namespace Light.Results.SharedJsonSerialization;

/// <summary>
/// Provides transport-agnostic JSON serialization helpers used across Light.Results integrations.
/// </summary>
public static class SharedSerializerExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="JsonTypeInfo" /> has known polymorphism information.
    /// </summary>
    public static bool HasKnownPolymorphism(this JsonTypeInfo jsonTypeInfo) =>
        jsonTypeInfo.Type.IsSealed || jsonTypeInfo.Type.IsValueType || jsonTypeInfo.PolymorphismOptions is not null;

    /// <summary>
    /// Determines whether the specified <see cref="JsonTypeInfo" /> should be used for the given runtime type.
    /// </summary>
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
            try
            {
                ((JsonConverter<T>) valueTypeInfo.Converter).Write(writer, value, options);
                return;
            }
            catch (NotSupportedException) when (valueTypeInfo.Type == runtimeType)
            {
                JsonSerializer.Serialize(writer, value, runtimeType, options);
                return;
            }
        }

        if (!options.TryGetTypeInfo(runtimeType, out valueTypeInfo))
        {
            throw new InvalidOperationException(
                $"No JSON serialization metadata was found for type '{runtimeType}' - please ensure that JsonOptions are configured properly"
            );
        }

        if (valueTypeInfo.Converter is JsonConverter<T> converter)
        {
            try
            {
                converter.Write(writer, value, options);
                return;
            }
            catch (NotSupportedException) when (valueTypeInfo.Type == runtimeType)
            {
                JsonSerializer.Serialize(writer, value, runtimeType, options);
                return;
            }
        }

        JsonSerializer.Serialize(writer, value, valueTypeInfo);
    }

    /// <summary>
    /// Writes the metadata JSON object in a property named <c>metadata</c>.
    /// </summary>
    public static void WriteMetadataPropertyAndValue(
        this Utf8JsonWriter writer,
        MetadataObject metadata,
        JsonSerializerOptions serializerOptions
    )
    {
        var metadataTypeInfo =
            serializerOptions.GetTypeInfo(typeof(MetadataObject)) ??
            throw new InvalidOperationException(
                $"No JSON serialization metadata was found for type '{typeof(MetadataObject)}' - please ensure that JsonOptions are configured properly"
            );
        writer.WritePropertyName("metadata");
        ((JsonConverter<MetadataObject>) metadataTypeInfo.Converter).Write(writer, metadata, serializerOptions);
    }

    /// <summary>
    /// Writes rich errors in the Light.Results-native format.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="errors">The errors to serialize.</param>
    /// <param name="isValidationResponse">
    /// When <see langword="true" />, targets are required and normalized to empty strings when whitespace.
    /// </param>
    /// <param name="serializerOptions">Serializer options for nested metadata serialization.</param>
    /// <exception cref="InvalidOperationException">Thrown when a validation response error has no target.</exception>
    public static void WriteRichErrors(
        this Utf8JsonWriter writer,
        Errors errors,
        bool isValidationResponse,
        JsonSerializerOptions serializerOptions
    )
    {
        writer.WritePropertyName("errors");
        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            writer.WriteStartObject();

            writer.WriteString("message", error.Message);

            if (error.Code is not null)
            {
                writer.WriteString("code", error.Code);
            }

            if (isValidationResponse)
            {
                var target = GetNormalizedTargetForValidationResponse(error, i);
                writer.WriteString("target", target);
            }
            else if (error.Target is not null)
            {
                writer.WriteString("target", error.Target);
            }

            if (error.Category != ErrorCategory.Unclassified)
            {
                writer.WriteString("category", error.Category.ToString());
            }

            if (error.Metadata.HasValue)
            {
                var metadataTypeInfo = serializerOptions.GetTypeInfo(typeof(MetadataObject));
                writer.WritePropertyName("metadata");
                ((JsonConverter<MetadataObject>) metadataTypeInfo.Converter).Write(
                    writer,
                    error.Metadata.Value,
                    serializerOptions
                );
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static string GetNormalizedTargetForValidationResponse(Error error, int errorIndex)
    {
        if (error.Target is null)
        {
            throw new InvalidOperationException(
                $"Error at index {errorIndex} does not have a Target set. For HTTP 400 Bad Request and HTTP 422 Unprocessable Content responses, all errors must have the Target property set. Use an empty string to indicate the root object."
            );
        }

        return string.IsNullOrWhiteSpace(error.Target) ? "" : error.Target;
    }
}
