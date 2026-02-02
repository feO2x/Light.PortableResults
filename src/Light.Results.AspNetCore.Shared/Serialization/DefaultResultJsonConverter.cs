using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.AspNetCore.Shared.Serialization;

public sealed class DefaultResultJsonConverter : JsonConverter<Result>
{
    private readonly LightResultOptions _options;

    public DefaultResultJsonConverter(LightResultOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Result result, JsonSerializerOptions options) =>
        Serialize(writer, result, options);

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
                result.Metadata is null)
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

public sealed class DefaultResultJsonConverter<T> : JsonConverter<Result<T>>
{
    private readonly LightResultOptions _options;

    public DefaultResultJsonConverter(LightResultOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    public override Result<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions serializerOptions
    ) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Result<T> result, JsonSerializerOptions serializerOptions) =>
        Serialize(writer, result, serializerOptions);

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
        if (metadataSerializationMode == MetadataSerializationMode.Always && result.Metadata.HasValue)
        {
            writer.WriteMetadataPropertyAndValue(result.Metadata.Value, serializerOptions);
        }

        writer.WriteEndObject();
    }
}
