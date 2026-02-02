using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Http;
using Light.Results.Metadata;

// ReSharper disable ConvertToExtensionBlock

namespace Light.Results.AspNetCore.Shared.Serialization;

/// <summary>
/// Provides extension methods to serialize and write types of Light.Results using System.Text.Json.
/// </summary>
public static partial class SerializerExtensions
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
    /// Writes a generic value using System.Text.Json's <see cref="Utf8JsonWriter" />.
    /// </summary>
    /// <param name="writer">The writer object.</param>
    /// <param name="value">The generic value to be serialized.</param>
    /// <param name="options">The json serializer options providing JSON Type Infos.</param>
    /// <typeparam name="T">The type of the value to be serialized.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no JSON type info is found for the runtime type of T.
    /// </exception>
    public static void WriteGenericValue<T>(this Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

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

        ((JsonConverter<T>) valueTypeInfo.Converter).Write(writer, value, options);
    }

    /// <summary>
    /// Writes the metadata JSON object using System.Text.Json's <see cref="Utf8JsonWriter" /> in a JSON property named
    /// "metadata". This
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="metadata">The metadata object to be written.</param>
    /// <param name="serializerOptions">The JSON serializer options providing JSON Type Infos.</param>
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

    public static void WriteErrors(
        this Utf8JsonWriter writer,
        Errors errors,
        ErrorSerializationFormat format,
        JsonSerializerOptions serializerOptions
    )
    {
        if (format == ErrorSerializationFormat.Rich)
        {
            WriteRichErrors(writer, errors, serializerOptions);
        }
        else
        {
            WriteAspNetCoreCompatibleErrors(writer, errors);
        }
    }

    private static void WriteRichErrors(Utf8JsonWriter writer, Errors errors, JsonSerializerOptions serializerOptions)
    {
        writer.WritePropertyName("errors");
        writer.WriteStartArray();

        foreach (var error in errors)
        {
            writer.WriteStartObject();

            writer.WriteString("message", error.Message);

            if (error.Code is not null)
            {
                writer.WriteString("code", error.Code);
            }

            if (error.Target is not null)
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

    /// <summary>
    /// Serializes a <see cref="ProblemDetailsInfo" />, the provided <paramref name="errors" /> collection,
    /// and optional <paramref name="metadata" /> into the supplied <paramref name="writer" /> according to the
    /// configured <paramref name="serializerOptions" /> and <paramref name="options" />.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter" /> to write the JSON payload to.</param>
    /// <param name="errors">The errors that determine the serialized problem details and error content.</param>
    /// <param name="metadata">Optional metadata object to append to the JSON output.</param>
    /// <param name="serializerOptions">The serializer options to use for nested object serialization.</param>
    /// <param name="options">The Light.Results options controlling problem details creation and error formatting.</param>
    public static void SerializeProblemDetailsAndMetadata(
        this Utf8JsonWriter writer,
        Errors errors,
        MetadataObject? metadata,
        JsonSerializerOptions serializerOptions,
        LightResultOptions options
    )
    {
        var problemDetailsInfo =
            options.CreateProblemDetailsInfo?.Invoke(errors, metadata) ??
            ProblemDetailsInfo.CreateDefault(errors, options.FirstErrorCategoryIsLeadingCategory);

        writer.WriteStartObject();
        writer.WriteString("type", problemDetailsInfo.Type);
        writer.WriteString("title", problemDetailsInfo.Title);
        writer.WriteNumber("status", (int) problemDetailsInfo.Status);

        if (!string.IsNullOrWhiteSpace(problemDetailsInfo.Detail))
        {
            writer.WriteString("detail", problemDetailsInfo.Detail);
        }

        if (string.IsNullOrWhiteSpace(problemDetailsInfo.Detail))
        {
            writer.WriteString("instance", problemDetailsInfo.Instance);
        }

        writer.WriteErrors(errors, options.ErrorSerializationFormat, serializerOptions);

        if (metadata.HasValue)
        {
            writer.WriteMetadataPropertyAndValue(metadata.Value, serializerOptions);
        }

        writer.WriteEndObject();
    }
}
