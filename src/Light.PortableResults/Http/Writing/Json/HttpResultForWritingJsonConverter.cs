using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Light.PortableResults.SharedJsonSerialization.Writing;

namespace Light.PortableResults.Http.Writing.Json;

/// <summary>
/// Stateless JSON converter for <see cref="HttpResultForWriting" /> that writes either success HTTP response bodies
/// or Problem Details responses, depending on the wrapped result.
/// </summary>
public sealed class HttpResultForWritingJsonConverter : JsonConverter<HttpResultForWriting>
{
    /// <summary>
    /// Gets a shared instance of the converter.
    /// </summary>
    public static HttpResultForWritingJsonConverter Instance { get; } = new ();

    /// <inheritdoc />
    public override HttpResultForWriting Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpResultForWritingJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        HttpResultForWriting wrapper,
        JsonSerializerOptions options
    )
    {
        var result = wrapper.Data;
        var resolvedOptions = wrapper.ResolvedOptions;

        if (result.IsValid)
        {
            if (resolvedOptions.MetadataSerializationMode == MetadataSerializationMode.ErrorsOnly ||
                result.Metadata is null ||
                !result.Metadata.Value.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody))
            {
                return;
            }

            writer.WriteStartObject();
            writer.WriteMetadataPropertyAndValue(
                result.Metadata.Value,
                MetadataValueAnnotation.SerializeInHttpResponseBody
            );
            writer.WriteEndObject();
            return;
        }

        writer.SerializeProblemDetailsAndMetadata(
            result.Errors,
            result.Metadata,
            options,
            resolvedOptions
        );
    }
}

/// <summary>
/// Stateless JSON converter for <see cref="HttpResultForWriting{T}" /> that writes either success HTTP response bodies
/// or Problem Details responses, depending on the wrapped result.
/// </summary>
/// <typeparam name="T">The type of the success value in the result.</typeparam>
public sealed class HttpResultForWritingJsonConverter<T> : JsonConverter<HttpResultForWriting<T>>
{
    /// <inheritdoc />
    public override HttpResultForWriting<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    ) =>
        throw new NotSupportedException(
            $"{nameof(HttpResultForWritingJsonConverter)}<> supports serialization only. Use a deserialization converter for reading."
        );

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        HttpResultForWriting<T> wrapper,
        JsonSerializerOptions options
    )
    {
        var result = wrapper.Data;
        var resolvedOptions = wrapper.ResolvedOptions;

        if (result.IsValid)
        {
            if (resolvedOptions.MetadataSerializationMode == MetadataSerializationMode.ErrorsOnly)
            {
                writer.WriteGenericValue(result.Value, options);
                return;
            }

            SerializeValueAndMetadata(writer, result, options, resolvedOptions.MetadataSerializationMode);
            return;
        }

        writer.SerializeProblemDetailsAndMetadata(
            result.Errors,
            result.Metadata,
            options,
            resolvedOptions
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
            writer.WriteMetadataPropertyAndValue(metadata, MetadataValueAnnotation.SerializeInHttpResponseBody);
        }

        writer.WriteEndObject();
    }
}
