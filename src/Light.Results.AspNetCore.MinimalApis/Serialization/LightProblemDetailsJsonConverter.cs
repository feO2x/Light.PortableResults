using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.AspNetCore.Abstractions;
using Light.Results.AspNetCore.Abstractions.Serialization;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.MinimalApis.Serialization;

/// <summary>
/// JSON converter for <see cref="LightProblemDetails" />.
/// Writes RFC 7807/9457-compliant Problem Details JSON.
/// </summary>
public sealed class LightProblemDetailsJsonConverter : JsonConverter<LightProblemDetails>
{
    public override LightProblemDetails Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotSupportedException("Deserialization of LightProblemDetails is not supported");
    }

    public override void Write(Utf8JsonWriter writer, LightProblemDetails value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("type", value.Type);
        writer.WriteString("title", value.Title);
        writer.WriteNumber("status", value.Status);
        writer.WriteString("detail", value.Detail);

        if (value.Instance is not null)
        {
            writer.WriteString("instance", value.Instance);
        }

        WriteErrors(writer, value.Errors, value.ErrorFormat);

        WriteMetadataExtensions(writer, value.Metadata);

        writer.WriteEndObject();
    }

    private static void WriteErrors(
        Utf8JsonWriter writer,
        Errors errors,
        ErrorSerializationFormat format
    )
    {
        if (format == ErrorSerializationFormat.Rich)
        {
            WriteRichErrors(writer, errors);
        }
        else
        {
            WriteAspNetCoreCompatibleErrors(writer, errors);
        }
    }

    private static void WriteRichErrors(Utf8JsonWriter writer, Errors errors)
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
                writer.WritePropertyName("metadata");
                MetadataValueJsonConverter.WriteMetadataObject(writer, error.Metadata.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteAspNetCoreCompatibleErrors(
        Utf8JsonWriter writer,
        Errors errors
    )
    {
        var groupedErrors = new Dictionary<string, List<string>>();
        var errorDetails = new List<ErrorDetail>();
        var hasDetails = false;

        var indexByTarget = new Dictionary<string, int>();

        foreach (var error in errors)
        {
            var target = error.Target ?? "";

            if (!groupedErrors.TryGetValue(target, out var messages))
            {
                messages = [];
                groupedErrors[target] = messages;
                indexByTarget[target] = 0;
            }

            var index = indexByTarget[target];
            indexByTarget[target] = index + 1;

            messages.Add(error.Message);

            var hasCode = error.Code is not null;
            var hasCategory = error.Category != ErrorCategory.Unclassified;
            var hasMetadata = error.Metadata.HasValue;

            if (!hasCode && !hasCategory && !hasMetadata)
            {
                continue;
            }

            hasDetails = true;
            errorDetails.Add(
                new ErrorDetail(
                    target,
                    index,
                    error.Code,
                    hasCategory ? error.Category : null,
                    error.Metadata
                )
            );
        }

        writer.WritePropertyName("errors");
        writer.WriteStartObject();
        foreach (var kvp in groupedErrors)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteStartArray();
            foreach (var message in kvp.Value)
            {
                writer.WriteStringValue(message);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();

        if (!hasDetails)
        {
            return;
        }

        writer.WritePropertyName("errorDetails");
        writer.WriteStartArray();
        foreach (var detail in errorDetails)
        {
            writer.WriteStartObject();

            writer.WriteString("target", detail.Target);
            writer.WriteNumber("index", detail.Index);

            if (detail.Code is not null)
            {
                writer.WriteString("code", detail.Code);
            }

            if (detail.Category.HasValue)
            {
                writer.WriteString("category", detail.Category.Value.ToString());
            }

            if (detail.Metadata.HasValue)
            {
                writer.WritePropertyName("metadata");
                MetadataValueJsonConverter.WriteMetadataObject(writer, detail.Metadata.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteMetadataExtensions(
        Utf8JsonWriter writer,
        MetadataObject? metadata
    )
    {
        if (!metadata.HasValue)
        {
            return;
        }

        foreach (var kvp in metadata.Value)
        {
            writer.WritePropertyName(kvp.Key);
            MetadataValueJsonConverter.WriteMetadataValue(writer, kvp.Value);
        }
    }

    private readonly struct ErrorDetail
    {
        public string Target { get; }
        public int Index { get; }
        public string? Code { get; }
        public ErrorCategory? Category { get; }
        public MetadataObject? Metadata { get; }

        public ErrorDetail(string target, int index, string? code, ErrorCategory? category, MetadataObject? metadata)
        {
            Target = target;
            Index = index;
            Code = code;
            Category = category;
            Metadata = metadata;
        }
    }
}
