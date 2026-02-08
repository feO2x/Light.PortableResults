using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.Http.Reading.Json;

/// <summary>
/// JSON converter for reading <see cref="Result" /> payloads in HTTP client scenarios.
/// </summary>
public sealed class HttpReadResultJsonConverter : JsonConverter<Result>
{
    /// <summary>
    /// Reads the JSON representation of a <see cref="Result" />.
    /// </summary>
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ResultJsonReader.ReadResult(ref reader);

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadResultJsonConverter)} supports deserialization only. Use a serialization converter for writing."
        );
}

/// <summary>
/// JSON converter for reading <see cref="Result{T}" /> payloads in HTTP client scenarios.
/// </summary>
public sealed class HttpReadResultJsonConverter<T> : JsonConverter<Result<T>>
{
    private readonly PreferSuccessPayload _preferSuccessPayload;

    /// <summary>
    /// Initializes a new instance of <see cref="HttpReadResultJsonConverter{T}" />.
    /// </summary>
    public HttpReadResultJsonConverter(PreferSuccessPayload preferSuccessPayload = PreferSuccessPayload.Auto)
    {
        _preferSuccessPayload = preferSuccessPayload;
    }

    /// <summary>
    /// Reads the JSON representation of a <see cref="Result{T}" />.
    /// </summary>
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ResultJsonReader.ReadResult<T>(ref reader, options, _preferSuccessPayload);

    /// <summary>
    /// Writing is not supported by this converter.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(HttpReadResultJsonConverter<T>)} supports deserialization only. Use a serialization converter for writing."
        );
}
