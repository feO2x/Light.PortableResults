using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// Provides low-level JSON parsing helpers for Light.Results payloads.
/// </summary>
public static class ResultJsonReader
{
    /// <summary>
    /// Reads a <see cref="Result" /> from the current JSON token using auto-detection.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The parsed result.</returns>
    public static Result ReadResult(ref Utf8JsonReader reader)
    {
        EnsureReaderHasToken(ref reader);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Non-generic results must be JSON objects.");
        }

        var inspection = InspectObject(ref reader);
        return inspection.IsProblemDetails ? ReadNonGenericFailureResult(ref reader) : ReadSuccessResult(ref reader);
    }

    /// <summary>
    /// Reads a <see cref="Result{T}" /> from the current JSON token using auto-detection.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="serializerOptions">The serializer options providing JSON type metadata.</param>
    /// <param name="preferSuccessPayload">The preferred success payload interpretation.</param>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <returns>The parsed result.</returns>
    public static Result<T> ReadResult<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions,
        PreferSuccessPayload preferSuccessPayload = PreferSuccessPayload.Auto
    )
    {
        if (serializerOptions is null)
        {
            throw new ArgumentNullException(nameof(serializerOptions));
        }

        EnsureReaderHasToken(ref reader);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var inspection = InspectObject(ref reader);
            if (inspection.IsProblemDetails)
            {
                return ReadGenericFailureResult<T>(ref reader);
            }
        }

        return ReadSuccessResult<T>(ref reader, serializerOptions, preferSuccessPayload);
    }

    /// <summary>
    /// Reads a <see cref="Result" /> from the current JSON token using auto-detection.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <returns>The parsed result.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is not a valid result.</exception>
    public static Result ReadSuccessResult(ref Utf8JsonReader reader)
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

        return Result.Ok(metadata);
    }

    /// <summary>
    /// Reads a successful <see cref="Result{T}" /> from the provided <paramref name="reader" /> according to the
    /// preferred payload shape. Supports both bare values and wrapper objects containing optional metadata.
    /// </summary>
    /// <typeparam name="T">Type of the success payload.</typeparam>
    /// <param name="reader">The JSON reader positioned at the success payload.</param>
    /// <param name="serializerOptions">Serializer options used when materializing wrapped values.</param>
    /// <param name="preferSuccessPayload">Specifies whether to expect bare values, wrapper objects, or auto-detection.</param>
    /// <returns>A successful <see cref="Result{T}" /> built from the JSON payload.</returns>
    /// <exception cref="JsonException">
    /// Thrown when the payload shape does not match the expectations or the success value is null.
    /// </exception>
    public static Result<T> ReadSuccessResult<T>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions serializerOptions,
        PreferSuccessPayload preferSuccessPayload
    )
    {
        EnsureReaderHasToken(ref reader);

        if (preferSuccessPayload == PreferSuccessPayload.BareValue || reader.TokenType != JsonTokenType.StartObject)
        {
            var bareValue = ReadGenericValue<T>(ref reader, serializerOptions);
            if (bareValue is null)
            {
                throw new JsonException("Result value cannot be null.");
            }

            return Result<T>.Ok(bareValue);
        }

        var inspection = InspectObject(ref reader);
        var wrapperCandidate = inspection.IsWrapperCandidate;

        if (preferSuccessPayload == PreferSuccessPayload.WrappedValue)
        {
            return !wrapperCandidate ?
                throw new JsonException("Expected wrapper object containing value and optional metadata.") :
                ReadWrapperResult<T>(ref reader, serializerOptions);
        }

        if (wrapperCandidate)
        {
            return ReadWrapperResult<T>(ref reader, serializerOptions);
        }

        var value = ReadGenericValue<T>(ref reader, serializerOptions);
        return value is null ? throw new JsonException("Result value cannot be null.") : Result<T>.Ok(value);
    }

    /// <summary>
    /// Reads a failed non-generic <see cref="Result" /> from the provided <paramref name="reader" />,
    /// interpreting the payload as problem details containing errors and optional metadata.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the failure payload.</param>
    /// <returns>A failed <see cref="Result" /> populated with errors and metadata from the payload.</returns>
    /// <exception cref="JsonException">Thrown when the payload cannot be parsed as problem details.</exception>
    public static Result ReadNonGenericFailureResult(ref Utf8JsonReader reader)
    {
        var payload = ReadProblemDetails(ref reader);
        return Result.Fail(payload.Errors, payload.Metadata);
    }

    /// <summary>
    /// Reads a failed generic <see cref="Result{T}" /> from the provided <paramref name="reader" />,
    /// interpreting the payload as problem details with errors and optional metadata.
    /// </summary>
    /// <typeparam name="T">Type of the expected success payload (unused for failures but required by the result).</typeparam>
    /// <param name="reader">The JSON reader positioned at the failure payload.</param>
    /// <returns>A failed <see cref="Result{T}" /> populated with errors and metadata from the payload.</returns>
    /// <exception cref="JsonException">Thrown when the payload cannot be parsed as problem details.</exception>
    public static Result<T> ReadGenericFailureResult<T>(ref Utf8JsonReader reader)
    {
        var payload = ReadProblemDetails(ref reader);
        return Result<T>.Fail(payload.Errors, payload.Metadata);
    }

    private static Result<T> ReadWrapperResult<T>(ref Utf8JsonReader reader, JsonSerializerOptions serializerOptions)
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

        return Result<T>.Ok(value, metadata);
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
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Each error must be a JSON object.");
            }

            var error = ReadRichErrorObject(ref reader);
            errors.Add(error);
        }
    }

    private static ErrorBuilder ReadRichErrorObject(ref Utf8JsonReader reader)
    {
        string? message = null;
        string? code = null;
        string? target = null;
        var category = ErrorCategory.Unclassified;
        MetadataObject? metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in error object.");
            }

            if (reader.ValueTextEquals("message"))
            {
                message = ReadStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("code"))
            {
                code = ReadOptionalStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("target"))
            {
                target = ReadOptionalStringValue(ref reader);
            }
            else if (reader.ValueTextEquals("category"))
            {
                var categoryString = ReadOptionalStringValue(ref reader);
                if (!string.IsNullOrWhiteSpace(categoryString))
                {
                    if (!Enum.TryParse(categoryString, ignoreCase: true, out category))
                    {
                        throw new JsonException($"Unknown error category '{categoryString}'.");
                    }
                }
            }
            else if (reader.ValueTextEquals("metadata"))
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while reading error metadata.");
                }

                metadata = reader.TokenType == JsonTokenType.Null ?
                    null :
                    MetadataJsonReader.ReadMetadataObject(ref reader);
            }
            else
            {
                if (!reader.Read())
                {
                    throw new JsonException("Unexpected end of JSON while skipping error property.");
                }

                reader.Skip();
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new JsonException("Error objects must include a message.");
        }

        var messageValue = message!;
        return new ErrorBuilder(messageValue, code, target, category, metadata);
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
        var effectiveOptions = EnsureTypeInfoResolver(serializerOptions);
        var typeInfo = effectiveOptions.GetTypeInfo(typeof(T));
        if (typeInfo.Converter is JsonConverter<T> converter)
        {
            return converter.Read(ref reader, typeof(T), effectiveOptions);
        }

        return (T?) JsonSerializer.Deserialize(ref reader, typeof(T), effectiveOptions);
    }

    private static JsonSerializerOptions EnsureTypeInfoResolver(JsonSerializerOptions serializerOptions)
    {
        if (serializerOptions.TypeInfoResolver is not null)
        {
            return serializerOptions;
        }

        return new JsonSerializerOptions(serializerOptions)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
    }

    private static ObjectInspection InspectObject(ref Utf8JsonReader reader)
    {
        var inspection = new ObjectInspection();
        var lookahead = reader;

        if (lookahead.TokenType == JsonTokenType.None && !lookahead.Read())
        {
            throw new JsonException("Unexpected end of JSON.");
        }

        if (lookahead.TokenType != JsonTokenType.StartObject)
        {
            return inspection;
        }

        while (lookahead.Read())
        {
            if (lookahead.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (lookahead.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name in JSON object.");
            }

            if (lookahead.ValueTextEquals("value"))
            {
                inspection.HasValue = true;
            }
            else if (lookahead.ValueTextEquals("metadata"))
            {
                inspection.HasMetadata = true;
            }
            else if (lookahead.ValueTextEquals("errors"))
            {
                inspection.HasErrors = true;
            }
            else if (lookahead.ValueTextEquals("type"))
            {
                inspection.HasType = true;
            }
            else if (lookahead.ValueTextEquals("title"))
            {
                inspection.HasTitle = true;
            }
            else if (lookahead.ValueTextEquals("status"))
            {
                inspection.HasStatus = true;
            }
            else if (lookahead.ValueTextEquals("detail"))
            {
                inspection.HasDetail = true;
            }
            else if (lookahead.ValueTextEquals("instance"))
            {
                inspection.HasInstance = true;
            }
            else if (lookahead.ValueTextEquals("errorDetails"))
            {
                inspection.HasErrorDetails = true;
            }
            else
            {
                inspection.HasOther = true;
            }

            if (!lookahead.Read())
            {
                throw new JsonException("Unexpected end of JSON while inspecting object.");
            }

            lookahead.Skip();
        }

        return inspection;
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

    private struct ObjectInspection
    {
        public bool HasValue { get; set; }
        public bool HasMetadata { get; set; }
        public bool HasErrors { get; set; }
        public bool HasType { get; set; }
        public bool HasTitle { get; set; }
        public bool HasStatus { get; set; }
        public bool HasDetail { get; set; }
        public bool HasInstance { get; set; }
        public bool HasErrorDetails { get; set; }
        public bool HasOther { get; set; }

        public bool IsProblemDetails => HasErrors && HasType && HasTitle && HasStatus;

        public bool IsWrapperCandidate =>
            !HasErrors &&
            !HasType &&
            !HasTitle &&
            !HasStatus &&
            !HasDetail &&
            !HasInstance &&
            !HasErrorDetails &&
            !HasOther;
    }

    private enum ErrorFormat
    {
        Unknown = 0,
        AspNetCore = 1,
        Rich = 2
    }
}
