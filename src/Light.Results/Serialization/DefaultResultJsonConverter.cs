using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.Serialization;

/// <summary>
/// JSON converter for <see cref="Result" /> that either writes success HTTP response bodies or Problem Details
/// responses, depending on the passed result.
/// </summary>
public sealed class DefaultResultJsonConverter : JsonConverter<Result>
{
    private readonly LightResultOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResultJsonConverter" />.
    /// </summary>
    /// <param name="options">The Light.Results options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public DefaultResultJsonConverter(LightResultOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Reads the JSON representation of a <see cref="Result" />.
    /// </summary>
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ResultJsonReader.ReadResult(ref reader);

    /// <summary>
    /// Writes the JSON representation for the specified result.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required JSON type metadata is missing or when validation errors are missing targets.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, Result result, JsonSerializerOptions options) =>
        Serialize(writer, result, options);

    /// <summary>
    /// Serializes the specified result using the configured or overridden options.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <param name="overrideOptions">Optional options that override the configured defaults.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required JSON type metadata is missing or when validation errors are missing targets.
    /// </exception>
    public void Serialize(
        Utf8JsonWriter writer,
        Result result,
        JsonSerializerOptions serializerOptions,
        LightResultOptions? overrideOptions = null
    )
    {
        var lightResultOptions = overrideOptions ?? _options;
        if (result.IsValid)
        {
            // We first check if we write metadata when the result is valid
            if (lightResultOptions.MetadataSerializationMode == MetadataSerializationMode.ErrorsOnly ||
                result.Metadata is null ||
                !result.Metadata.Value.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody))
            {
                // If we end up here, we write nothing. Result does not have a value and no metadata should be written.
                return;
            }

            // If we end up here, we need to serialize metadata. We write a wrapper object which only contains
            // the metadata
            writer.WriteStartObject();
            writer.WriteMetadataPropertyAndValue(result.Metadata.Value, serializerOptions);
            writer.WriteEndObject();
            return;
        }

        // If we end up here, we need to serialize problem details because the result contains errors
        writer.SerializeProblemDetailsAndMetadata(
            result.Errors,
            result.Metadata,
            serializerOptions,
            overrideOptions ?? _options
        );
    }
}

/// <summary>
/// JSON converter for <see cref="Result{T}" /> that honors <see cref="LightResultOptions" />.
/// </summary>
public sealed class DefaultResultJsonConverter<T> : JsonConverter<Result<T>>
{
    private readonly LightResultOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResultJsonConverter{T}" />.
    /// </summary>
    /// <param name="options">The Light.Results options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public DefaultResultJsonConverter(LightResultOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Reads the JSON representation of a <see cref="Result{T}" />.
    /// </summary>
    public override Result<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions serializerOptions
    ) =>
        ResultJsonReader.ReadResult<T>(ref reader, serializerOptions);

    /// <summary>
    /// Writes the JSON representation for the specified result.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required JSON type metadata is missing or when validation errors are missing targets.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, Result<T> result, JsonSerializerOptions serializerOptions) =>
        Serialize(writer, result, serializerOptions);

    /// <summary>
    /// Serializes the specified result using the configured or overridden options.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <param name="overrideOptions">Optional options that override the configured defaults.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required JSON type metadata is missing or when validation errors are missing targets.
    /// </exception>
    public void Serialize(
        Utf8JsonWriter writer,
        Result<T> result,
        JsonSerializerOptions serializerOptions,
        LightResultOptions? overrideOptions = null
    )
    {
        var lightResultOptions = overrideOptions ?? _options;
        if (result.IsValid)
        {
            // We first check if we write metadata when the result is valid
            if (lightResultOptions.MetadataSerializationMode == MetadataSerializationMode.ErrorsOnly)
            {
                // If we end up here, we simply write the result value. No metadata is written.
                // There will be no wrapper object encompassing value and metadata
                writer.WriteGenericValue(result.Value, serializerOptions);
                return;
            }

            // If we end up here, we need to use the wrapper object encompassing value and potentially metadata
            SerializeValueAndMetadata(writer, result, serializerOptions, lightResultOptions.MetadataSerializationMode);
            return;
        }

        // If we end up here, we need to serialize problem details because the result contains errors
        writer.SerializeProblemDetailsAndMetadata(
            result.Errors,
            result.Metadata,
            serializerOptions,
            overrideOptions ?? _options
        );
    }

    private static void SerializeValueAndMetadata(
        Utf8JsonWriter writer,
        Result<T> result,
        JsonSerializerOptions serializerOptions,
        MetadataSerializationMode metadataSerializationMode
    )
    {
        writer.WriteStartObject();

        writer.WritePropertyName("value");
        writer.WriteGenericValue(result.Value, serializerOptions);
        if (metadataSerializationMode == MetadataSerializationMode.Always &&
            result.Metadata is { } metadata &&
            metadata.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody))
        {
            writer.WriteMetadataPropertyAndValue(metadata, serializerOptions);
        }

        writer.WriteEndObject();
    }
}
