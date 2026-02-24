using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Metadata;

namespace Light.Results.SharedJsonSerialization.Writing;

/// <summary>
/// Provides extensions to serialize Light.Results errors to JSON.
/// </summary>
public static class ErrorsExtensions
{
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
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

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
                var target = error.GetNormalizedTargetForValidationResponse(i);
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
                if (metadataTypeInfo is not JsonTypeInfo<MetadataObject> castTypeInfo)
                {
                    throw new InvalidOperationException(
                        "Could not resolve 'JsonTypeInfo<MetadataObject>'. Please ensure that your JsonSerializerOptions are configured correctly."
                    );
                }

                writer.WritePropertyName("metadata");
                JsonSerializer.Serialize(writer, error.Metadata.Value, castTypeInfo);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Ensures that the <see cref="Error.Target" /> property is present and returns a normalized value
    /// for use in HTTP validation responses.
    /// </summary>
    /// <param name="error">The error to inspect.</param>
    /// <param name="errorIndex">The position of the error within the response payload.</param>
    /// <returns>
    /// The original target when it contains a non-whitespace value, otherwise an empty string to
    /// represent the root object.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Error.Target" /> is <c>null</c>, which violates the requirements for
    /// validation responses.
    /// </exception>
    public static string GetNormalizedTargetForValidationResponse(this Error error, int errorIndex)
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
