using System;
using System.Collections.Generic;
using System.Text.Json;
using Light.Results.Metadata;

namespace Light.Results.Http.Writing.Json;

public static partial class SerializerExtensions
{
    private const int MaxErrorsForStackAlloc = 10;

    private static void WriteAspNetCoreCompatibleErrors(
        Utf8JsonWriter writer,
        Errors errors
    )
    {
        if (errors.Count <= MaxErrorsForStackAlloc)
        {
            WriteAspNetCoreCompatibleErrorsOptimized(writer, errors);
        }
        else
        {
            WriteAspNetCoreCompatibleErrorsFallback(writer, errors);
        }
    }

    // ReSharper disable once CognitiveComplexity -- This is a performance-critical method.
    private static void WriteAspNetCoreCompatibleErrorsOptimized(
        Utf8JsonWriter writer,
        Errors errors
    )
    {
        // Rationale:
        // - Validation endpoints rarely produce >10 errors, so we special-case that size to stay on the stack.
        // - This avoids allocating Dictionaries/Lists from the fallback path. Only a single string[10] array remains
        //   which is cheaper than pooling and keeps the code simple.
        // Benchmarks (M3 Max, .NET 10.0.2, see ValidationErrorSerializationBenchmarks):
        // - 1 error: 208 ns / 904 B -> 153 ns / 136 B (≈26% faster, 85% fewer allocations)
        // - 5 unique targets: 782 ns / 2,160 B -> 536 ns / 136 B (≈31% faster, 94% fewer allocations)
        // - 10 unique targets: 1.54 µs / 4,368 B -> 1.11 µs / 136 B (≈28% faster, 97% fewer allocations)

        // Stack-allocated tracking for unique targets (max 10 errors = max 10 unique targets)
        Span<int> targetErrorCounts = stackalloc int[MaxErrorsForStackAlloc];
        Span<int> errorToTargetIndex = stackalloc int[MaxErrorsForStackAlloc];
        Span<int> errorIndexWithinTarget = stackalloc int[MaxErrorsForStackAlloc];

        // Store target strings - this small array is the only heap allocation
        var uniqueTargetCount = 0;
        Span<string> targetStrings = new string[MaxErrorsForStackAlloc];

        // First pass: identify unique targets and map errors to targets
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var target = GetNormalizedTargetForValidationResponse(error, i);

            // Find or add target
            var targetIndex = -1;
            for (var j = 0; j < uniqueTargetCount; j++)
            {
                if (string.Equals(targetStrings[j], target, StringComparison.Ordinal))
                {
                    targetIndex = j;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                targetIndex = uniqueTargetCount;
                targetStrings[uniqueTargetCount] = target;
                targetErrorCounts[uniqueTargetCount] = 0;
                uniqueTargetCount++;
            }

            errorIndexWithinTarget[i] = targetErrorCounts[targetIndex];
            targetErrorCounts[targetIndex]++;
            errorToTargetIndex[i] = targetIndex;
        }

        // Write "errors" object grouped by target
        writer.WritePropertyName("errors");
        writer.WriteStartObject();

        for (var targetIdx = 0; targetIdx < uniqueTargetCount; targetIdx++)
        {
            writer.WritePropertyName(targetStrings[targetIdx]);
            writer.WriteStartArray();

            // Write all messages for this target
            for (var i = 0; i < errors.Count; i++)
            {
                if (errorToTargetIndex[i] == targetIdx)
                {
                    writer.WriteStringValue(errors[i].Message);
                }
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();

        // Check if any error has details (code, category, or metadata)
        var hasAnyDetails = false;
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            if (error.Code is not null || error.Category != ErrorCategory.Unclassified || error.Metadata.HasValue)
            {
                hasAnyDetails = true;
                break;
            }
        }

        if (!hasAnyDetails)
        {
            return;
        }

        // Write "errorDetails" array
        writer.WritePropertyName("errorDetails");
        writer.WriteStartArray();

        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var hasCode = error.Code is not null;
            var hasCategory = error.Category != ErrorCategory.Unclassified;

            if (!hasCode && !hasCategory && !error.Metadata.HasValue)
            {
                continue;
            }

            writer.WriteStartObject();
            writer.WriteString("target", targetStrings[errorToTargetIndex[i]]);
            writer.WriteNumber("index", errorIndexWithinTarget[i]);

            if (hasCode)
            {
                writer.WriteString("code", error.Code);
            }

            if (hasCategory)
            {
                writer.WriteString("category", error.Category.ToString());
            }

            if (error.Metadata is { } metadata)
            {
                writer.WritePropertyName("metadata");
                HttpWriteMetadataValueJsonConverter.WriteMetadataObject(writer, metadata);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteAspNetCoreCompatibleErrorsFallback(
        Utf8JsonWriter writer,
        Errors errors
    )
    {
        var (groupedErrors, errorDetails) = ProcessErrorsForValidationResponse(errors);

        WriteGroupedErrors(writer, groupedErrors);

        if (errorDetails.Count > 0)
        {
            WriteErrorDetails(writer, errorDetails);
        }
    }

    // Processes errors into two structures for ASP.NET Core compatible serialization:
    // 1. groupedErrors: Dictionary<target, messages[]> - matches ASP.NET Core's ValidationProblemDetails.Errors
    // 2. errorDetails: additional metadata (code, category, metadata) for errors that have it
    //
    // Multiple errors can share the same target (e.g., multiple validation rules failing for one field).
    // Each error gets its own ErrorDetail entry with an index that correlates back to its message position.
    //
    // Example with two errors for "email":
    //   Error 1: target="email", message="Email is required", code="Required"
    //   Error 2: target="email", message="Email format is invalid", code="InvalidFormat"
    //
    // Produces:
    //   "errors": {
    //     "email": ["Email is required", "Email format is invalid"]
    //   },
    //   "errorDetails": [
    //     { "target": "email", "index": 0, "code": "Required" },
    //     { "target": "email", "index": 1, "code": "InvalidFormat" }
    //   ]
    //
    // The index field allows consumers to correlate each ErrorDetail back to its specific message.
    private static ErrorGrouping ProcessErrorsForValidationResponse(Errors errors)
    {
        var groupedErrors = new Dictionary<string, List<string>>();
        var errorDetails = new List<ErrorDetail>();
        var indexByTarget = new Dictionary<string, int>();

        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            var target = GetNormalizedTargetForValidationResponse(error, i);

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

        return new ErrorGrouping(groupedErrors, errorDetails);
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
            HttpWriteMetadataValueJsonConverter.WriteMetadataObject(writer, detail.Metadata.Value);
        }

        writer.WriteEndObject();
    }

    private readonly record struct ErrorGrouping(
        Dictionary<string, List<string>> GroupedErrors,
        List<ErrorDetail> ErrorDetails
    );

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
