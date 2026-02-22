using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Light.Results.Metadata;
using Light.Results.SharedJsonSerialization.Reading;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for HTTP read payloads.
/// </summary>
public static class ResultJsonReader
{
    /// <summary>
    /// Reads a non-generic successful payload from the current JSON token.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The parsed success payload.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is not a valid non-generic success payload.</exception>
    public static HttpReadSuccessResultPayload ReadSuccessPayload(ref Utf8JsonReader reader)
    {
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Non-generic results must be JSON objects containing metadata only.");
        }

        MetadataObject? metadata = null;
        var hasMetadata = false;
        var hasOther = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in result object.");
            }

            if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader);
                hasMetadata = true;
            }
            else
            {
                hasOther = true;
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping property.");
                }

                reader.Skip();
            }
        }

        if (!hasMetadata || hasOther)
        {
            throw new JsonException("Non-generic results require an object with only the metadata property.");
        }

        return new HttpReadSuccessResultPayload(metadata);
    }

    /// <summary>
    /// Reads a failed payload from the current JSON token, interpreting the body as problem details.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the failure payload.</param>
    /// <returns>The parsed failure payload.</returns>
    /// <exception cref="JsonException">Thrown when the payload cannot be parsed as problem details.</exception>
    public static HttpReadFailureResultPayload ReadFailurePayload(ref Utf8JsonReader reader)
    {
        var payload = ReadProblemDetails(ref reader);
        return new HttpReadFailureResultPayload(payload.Errors, payload.Metadata);
    }

    /// <summary>
    /// Reads a successful payload interpreted as a bare value.
    /// </summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="reader">The JSON reader positioned at the success payload.</param>
    /// <param name="serializerOptions">Serializer options used when deserializing the value.</param>
    /// <returns>The parsed bare success payload.</returns>
    public static HttpReadBareSuccessResultPayload<T> ReadBareSuccessPayload<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions
    )
    {
        EnsureSerializerOptions(serializerOptions);
        EnsureReaderHasToken(ref reader);
        return ReadBareSuccessPayloadCore<T>(ref reader, serializerOptions);
    }

    /// <summary>
    /// Reads a successful payload interpreted as a wrapper object with a required <c>value</c> property and optional
    /// <c>metadata</c> property.
    /// </summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="reader">The JSON reader positioned at the success payload.</param>
    /// <param name="serializerOptions">Serializer options used when deserializing the wrapped value.</param>
    /// <returns>The parsed wrapped success payload.</returns>
    public static HttpReadWrappedSuccessResultPayload<T> ReadWrappedSuccessPayload<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions
    )
    {
        EnsureSerializerOptions(serializerOptions);
        EnsureReaderHasToken(ref reader);
        return ReadWrappedSuccessPayloadCore<T>(ref reader, serializerOptions);
    }

    /// <summary>
    /// Reads a successful payload using wrapper auto-detection.
    /// </summary>
    /// <typeparam name="T">The payload value type.</typeparam>
    /// <param name="reader">The JSON reader positioned at the success payload.</param>
    /// <param name="serializerOptions">Serializer options used when deserializing values.</param>
    /// <returns>The parsed auto success payload.</returns>
    public static HttpReadAutoSuccessResultPayload<T> ReadAutoSuccessPayload<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions
    )
    {
        EnsureSerializerOptions(serializerOptions);
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            var barePayload = ReadBareSuccessPayloadCore<T>(ref reader, serializerOptions);
            return new HttpReadAutoSuccessResultPayload<T>(barePayload.Value, metadata: null);
        }

        if (IsWrappedSuccessPayloadCandidate(ref reader))
        {
            var wrappedPayload = ReadWrappedSuccessPayloadCore<T>(ref reader, serializerOptions);
            return new HttpReadAutoSuccessResultPayload<T>(wrappedPayload.Value, wrappedPayload.Metadata);
        }

        var fallbackBarePayload = ReadBareSuccessPayloadCore<T>(ref reader, serializerOptions);
        return new HttpReadAutoSuccessResultPayload<T>(fallbackBarePayload.Value, metadata: null);
    }

    private static HttpReadBareSuccessResultPayload<T> ReadBareSuccessPayloadCore<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions
    )
    {
        var value = ReadGenericValue<T>(ref reader, serializerOptions);
        if (value is null)
        {
            throw new JsonException("Result value cannot be null.");
        }

        return new HttpReadBareSuccessResultPayload<T>(value);
    }

    private static HttpReadWrappedSuccessResultPayload<T> ReadWrappedSuccessPayloadCore<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of wrapper object.");
        }

        T? value = default;
        var hasValue = false;
        MetadataObject? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in wrapper object.");
            }

            if (reader.ValueTextEquals("value"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading value.");
                }

                value = ReadGenericValue<T>(ref reader, serializerOptions);
                hasValue = true;
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader);
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping property.");
                }

                reader.Skip();
                throw new JsonException("Unexpected property in wrapper object.");
            }
        }

        if (!hasValue)
        {
            throw new JsonException("Wrapper object is missing the value property.");
        }

        if (value is null)
        {
            throw new JsonException("Result value cannot be null.");
        }

        return new HttpReadWrappedSuccessResultPayload<T>(value, metadata);
    }

    private static ProblemDetailsPayload ReadProblemDetails(ref Utf8JsonReader reader)
    {
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Problem details payload must be a JSON object.");
        }

        string? type = null;
        string? title = null;
        string? detail = null;
        int? status = null;
        MetadataObject? metadata = null;

        List<ErrorBuilder>? errors = null;
        Dictionary<string, List<int>>? indexByTarget = null;
        List<ErrorDetailInfo>? pendingDetails = null;
        var errorFormat = ErrorFormat.Unknown;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in problem details object.");
            }

            if (reader.ValueTextEquals("type"))
            {
                type = ReadStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("title"))
            {
                title = ReadStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("status"))
            {
                status = ReadStatusValue(ref reader);
            }
            else if (reader.ValueTextEquals("detail"))
            {
                detail = ReadOptionalStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("instance"))
            {
                _ = ReadOptionalStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader);
            }
            else if (reader.ValueTextEquals("errors"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading errors.");
                }

                errors ??= new List<ErrorBuilder>();
                indexByTarget ??= new Dictionary<string, List<int>>(StringComparer.Ordinal);

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    errorFormat = ErrorFormat.Rich;
                    ReadRichErrors(ref reader, errors);
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    errorFormat = ErrorFormat.AspNetCore;
                    ReadAspNetErrors(ref reader, errors, indexByTarget);
                }
                else
                {
                    throw new JsonException("Errors must be a JSON array or object.");
                }

                if (pendingDetails is not null && errorFormat == ErrorFormat.AspNetCore)
                {
                    ApplyErrorDetails(errors, indexByTarget, pendingDetails);
                    pendingDetails = null;
                }
            }
            else if (reader.ValueTextEquals("errorDetails"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading error details.");
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("errorDetails must be an array.");
                }

                pendingDetails ??= new List<ErrorDetailInfo>();
                ReadErrorDetails(ref reader, pendingDetails);

                if (errors is not null && errorFormat == ErrorFormat.AspNetCore)
                {
                    ApplyErrorDetails(errors, indexByTarget!, pendingDetails);
                    pendingDetails = null;
                }
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping property.");
                }

                reader.Skip();
            }
        }

        if (status is null || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(title))
        {
            throw new JsonException("Problem details payload must include type, title, and status.");
        }

        var errorList = errors is { Count: > 0 } ? errors : null;
        if (errorList is null)
        {
            var fallbackError = CreateFallbackError(title!, detail, status.Value);
            return new ProblemDetailsPayload(new Errors(fallbackError), metadata);
        }

        if (pendingDetails is { Count: > 0 } && errorFormat == ErrorFormat.AspNetCore && indexByTarget is not null)
        {
            ApplyErrorDetails(errorList, indexByTarget, pendingDetails);
        }

        var errorArray = new Error[errorList.Count];
        for (var i = 0; i < errorList.Count; i++)
        {
            errorArray[i] = errorList[i].ToError();
        }

        return new ProblemDetailsPayload(new Errors(errorArray), metadata);
    }

    private static void ReadRichErrors(ref Utf8JsonReader reader, List<ErrorBuilder> errors)
    {
        var parsedErrors = SharedResultJsonReader.ReadRichErrors(ref reader);
        for (var i = 0; i < parsedErrors.Count; i++)
        {
            var error = parsedErrors[i];
            errors.Add(new ErrorBuilder(error.Message, error.Code, error.Target, error.Category, error.Metadata));
        }
    }

    private static void ReadAspNetErrors(
        ref Utf8JsonReader reader,
        List<ErrorBuilder> errors,
        Dictionary<string, List<int>> indexByTarget
    )
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in errors object.");
            }

            var target = reader.GetString() ?? string.Empty;
            if (!reader.Read())
            {
                throw new JsonException("Unexpected end of JSON while reading error list.");
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Errors values must be arrays.");
            }

            var indexes = new List<int>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException("Error messages must be strings.");
                }

                var message = reader.GetString();
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new JsonException("Error messages must be non-empty.");
                }

                var messageValue = message!;
                var errorIndex = errors.Count;
                errors.Add(new ErrorBuilder(messageValue, null, target, ErrorCategory.Validation, null));
                indexes.Add(errorIndex);
            }

            indexByTarget[target] = indexes;
        }
    }

    private static void ReadErrorDetails(ref Utf8JsonReader reader, List<ErrorDetailInfo> details)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Each error detail must be an object.");
            }

            details.Add(ReadErrorDetailObject(ref reader));
        }
    }

    private static ErrorDetailInfo ReadErrorDetailObject(ref Utf8JsonReader reader)
    {
        string? target = null;
        int? index = null;
        string? code = null;
        ErrorCategory? category = null;
        MetadataObject? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in error detail object.");
            }

            if (reader.ValueTextEquals("target"))
            {
                target = ReadOptionalStringValue(ref reader) ?? string.Empty;
            }
            else if (reader.ValueTextEquals("index"))
            {
                index = ReadIndexValue(ref reader);
            }
            else if (reader.ValueTextEquals("code"))
            {
                code = ReadOptionalStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("category"))
            {
                var categoryString = ReadOptionalStringValue(ref reader);
                if (!string.IsNullOrWhiteSpace(categoryString))
                {
                    if (!Enum.TryParse(categoryString, ignoreCase: true, out ErrorCategory parsed))
                    {
                        throw new JsonException($"Unknown error category '{categoryString}'.");
                    }

                    category = parsed;
                }
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading error detail metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader);
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping error detail property.");
                }

                reader.Skip();
            }
        }

        if (target is null)
        {
            throw new JsonException("Error detail objects must include a target.");
        }

        if (index is null)
        {
            throw new JsonException("Error detail objects must include an index.");
        }

        return new ErrorDetailInfo(target, index.Value, code, category, metadata);
    }

    private static void ApplyErrorDetails(
        List<ErrorBuilder> errors,
        Dictionary<string, List<int>> indexByTarget,
        List<ErrorDetailInfo> details
    )
    {
        for (var i = 0; i < details.Count; i++)
        {
            var detail = details[i];
            if (!indexByTarget.TryGetValue(detail.Target, out var indexes))
            {
                throw new JsonException($"No errors found for target '{detail.Target}'.");
            }

            if (detail.Index < 0 || detail.Index >= indexes.Count)
            {
                throw new JsonException(
                    $"Error detail index {detail.Index.ToString(CultureInfo.InvariantCulture)} is out of range for target '{detail.Target}'."
                );
            }

            var errorIndex = indexes[detail.Index];
            var builder = errors[errorIndex];
            builder.Code = detail.Code ?? builder.Code;
            builder.Category = detail.Category ?? builder.Category;
            builder.Metadata = detail.Metadata ?? builder.Metadata;
            errors[errorIndex] = builder;
        }
    }

    private static string ReadStringValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading string value.");
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected string value.");
        }

        return reader.GetString() ?? string.Empty;
    }

    private static string? ReadOptionalStringValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading string value.");
        }

        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            _ => throw new JsonException("Expected string or null value.")
        };
    }

    private static int ReadStatusValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading status.");
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var status))
        {
            throw new JsonException("Problem details status must be an integer.");
        }

        return status;
    }

    private static int ReadIndexValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON while reading index.");
        }

        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var index))
        {
            throw new JsonException("Error detail index must be an integer.");
        }

        return index;
    }

    private static Error CreateFallbackError(string title, string? detail, int status)
    {
        var message = !string.IsNullOrWhiteSpace(detail) ? detail! : title;
        var category = Enum.IsDefined(typeof(ErrorCategory), status) ?
            (ErrorCategory) status :
            ErrorCategory.Unclassified;

        return new Error
        {
            Message = message,
            Category = category
        };
    }

    private static T? ReadGenericValue<T>(ref Utf8JsonReader reader, JsonSerializerOptions serializerOptions)
    {
        return JsonSerializer.Deserialize<T>(ref reader, serializerOptions);
    }

    private static bool IsWrappedSuccessPayloadCandidate(ref Utf8JsonReader reader)
    {
        var lookahead = reader;

        if (lookahead.TokenType == JsonTokenType.None && !lookahead.Read())
        {
            throw new JsonException("Unexpected end of JSON.");
        }

        if (lookahead.TokenType != JsonTokenType.StartObject)
        {
            return false;
        }

        while (lookahead.Read())
        {
            if (lookahead.TokenType == JsonTokenType.EndObject)
            {
                return true;
            }

            if (lookahead.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in JSON object.");
            }

            if (!lookahead.ValueTextEquals("value") && !lookahead.ValueTextEquals("metadata"))
            {
                return false;
            }

            if (!lookahead.Read())
            {
                throw new JsonException("Unexpected end of JSON while inspecting object.");
            }

            lookahead.Skip();
        }

        throw new JsonException("Unexpected end of JSON while inspecting object.");
    }

    private static void EnsureReaderHasToken(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.None)
        {
            return;
        }

        if (!reader.Read())
        {
            throw new JsonException("Unexpected end of JSON.");
        }
    }

    private static void EnsureSerializerOptions(JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }
    }

    private readonly struct ProblemDetailsPayload
    {
        public ProblemDetailsPayload(Errors errors, MetadataObject? metadata)
        {
            Errors = errors;
            Metadata = metadata;
        }

        public Errors Errors { get; }
        public MetadataObject? Metadata { get; }
    }

    private struct ErrorBuilder
    {
        public ErrorBuilder(
            string message,
            string? code,
            string? target,
            ErrorCategory category,
            MetadataObject? metadata
        )
        {
            Message = message;
            Code = code;
            Target = target;
            Category = category;
            Metadata = metadata;
        }

        public string Message { get; }
        public string? Code { get; set; }
        public string? Target { get; }
        public ErrorCategory Category { get; set; }
        public MetadataObject? Metadata { get; set; }

        public Error ToError() => new ()
        {
            Message = Message,
            Code = Code,
            Target = Target,
            Category = Category,
            Metadata = Metadata
        };
    }

    private readonly struct ErrorDetailInfo
    {
        public ErrorDetailInfo(
            string target,
            int index,
            string? code,
            ErrorCategory? category,
            MetadataObject? metadata
        )
        {
            Target = target;
            Index = index;
            Code = code;
            Category = category;
            Metadata = metadata;
        }

        public string Target { get; }
        public int Index { get; }
        public string? Code { get; }
        public ErrorCategory? Category { get; }
        public MetadataObject? Metadata { get; }
    }

    private enum ErrorFormat
    {
        Unknown = 0,
        AspNetCore = 1,
        Rich = 2
    }
}
