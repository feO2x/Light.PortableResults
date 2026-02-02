using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.Shared.Serialization;

public static partial class SerializerExtensions
{
    private static void WriteAspNetCoreCompatibleErrors(
        Utf8JsonWriter writer,
        Errors errors
    )
    {
        var (groupedErrors, errorDetails) = ProcessErrors(errors);

        WriteGroupedErrors(writer, groupedErrors);

        if (errorDetails.Count > 0)
        {
            WriteErrorDetails(writer, errorDetails);
        }
    }

    private static (Dictionary<string, List<string>> groupedErrors, List<ErrorDetail> errorDetails) ProcessErrors(
        Errors errors
    )
    {
        var groupedErrors = new Dictionary<string, List<string>>();
        var errorDetails = new List<ErrorDetail>();
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

            var errorDetail = CreateErrorDetailIfNeeded(error, target, index);
            if (errorDetail is not null)
            {
                errorDetails.Add(errorDetail.Value);
            }
        }

        return (groupedErrors, errorDetails);
    }

    private static ErrorDetail? CreateErrorDetailIfNeeded(Error error, string target, int index)
    {
        var hasCode = error.Code is not null;
        var hasCategory = error.Category != ErrorCategory.Unclassified;
        var hasMetadata = error.Metadata.HasValue;

        if (!hasCode && !hasCategory && !hasMetadata)
        {
            return null;
        }

        return new ErrorDetail(
            target,
            index,
            error.Code,
            hasCategory ? error.Category : null,
            error.Metadata
        );
    }

    private static void WriteGroupedErrors(Utf8JsonWriter writer, Dictionary<string, List<string>> groupedErrors)
    {
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
    }

    private static void WriteErrorDetails(Utf8JsonWriter writer, List<ErrorDetail> errorDetails)
    {
        writer.WritePropertyName("errorDetails");
        writer.WriteStartArray();

        foreach (var detail in errorDetails)
        {
            WriteSingleErrorDetail(writer, detail);
        }

        writer.WriteEndArray();
    }

    private static void WriteSingleErrorDetail(Utf8JsonWriter writer, ErrorDetail detail)
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
