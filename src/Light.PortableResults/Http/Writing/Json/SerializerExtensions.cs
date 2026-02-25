using System;
using System.Net;
using System.Text.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.Http.Writing.Json;

/// <summary>
/// Provides extension methods to serialize and write types of Light.PortableResults using System.Text.Json.
/// </summary>
public static partial class SerializerExtensions
{
    /// <summary>
    /// Writes the serialized error payload for the specified <see cref="Errors" /> collection.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="errors">The errors to serialize.</param>
    /// <param name="format">The validation problem serialization format.</param>
    /// <param name="statusCode">The HTTP status code associated with the response.</param>
    /// <param name="serializerOptions">The serializer options to use for nested serialization.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when validation errors are missing targets for HTTP 400/422 responses.
    /// </exception>
    public static void WriteErrors(
        this Utf8JsonWriter writer,
        Errors errors,
        ValidationProblemSerializationFormat format,
        HttpStatusCode statusCode,
        JsonSerializerOptions serializerOptions
    )
    {
        // ASP.NET Core compatible format only applies to validation responses (400 and 422)
        var isValidationResponse = statusCode == HttpStatusCode.BadRequest || (int) statusCode == 422;
        if (format == ValidationProblemSerializationFormat.AspNetCoreCompatible && isValidationResponse)
        {
            WriteAspNetCoreCompatibleErrors(writer, errors);
        }
        else
        {
            writer.WriteRichErrors(errors, isValidationResponse, serializerOptions);
        }
    }

    /// <summary>
    /// Serializes a <see cref="ProblemDetailsInfo" />, the provided <paramref name="errors" /> collection,
    /// and optional <paramref name="metadata" /> into the supplied <paramref name="writer" /> according to the
    /// configured <paramref name="serializerOptions" /> and <paramref name="resolvedOptions" />.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter" /> to write the JSON payload to.</param>
    /// <param name="errors">The errors that determine the serialized problem details and error content.</param>
    /// <param name="metadata">Optional metadata object to append to the JSON output.</param>
    /// <param name="serializerOptions">The serializer options to use for nested object serialization.</param>
    /// <param name="resolvedOptions">The frozen options controlling problem details creation and error formatting.</param>
    public static void SerializeProblemDetailsAndMetadata(
        this Utf8JsonWriter writer,
        Errors errors,
        MetadataObject? metadata,
        JsonSerializerOptions serializerOptions,
        ResolvedHttpWriteOptions resolvedOptions
    )
    {
        var problemDetailsInfo =
            resolvedOptions.CreateProblemDetailsInfo?.Invoke(errors, metadata) ??
            ProblemDetailsInfo.CreateDefault(errors, resolvedOptions.FirstErrorCategoryIsLeadingCategory);

        writer.WriteStartObject();
        writer.WriteString("type", problemDetailsInfo.Type);
        writer.WriteString("title", problemDetailsInfo.Title);
        writer.WriteNumber("status", (int) problemDetailsInfo.Status);

        if (!string.IsNullOrWhiteSpace(problemDetailsInfo.Detail))
        {
            writer.WriteString("detail", problemDetailsInfo.Detail);
        }

        if (!string.IsNullOrWhiteSpace(problemDetailsInfo.Instance))
        {
            writer.WriteString("instance", problemDetailsInfo.Instance);
        }

        writer.WriteErrors(
            errors,
            resolvedOptions.ValidationProblemSerializationFormat,
            problemDetailsInfo.Status,
            serializerOptions
        );

        if (metadata.HasValue)
        {
            writer.WriteMetadataPropertyAndValue(metadata.Value, MetadataValueAnnotation.SerializeInHttpResponseBody);
        }

        writer.WriteEndObject();
    }
}
