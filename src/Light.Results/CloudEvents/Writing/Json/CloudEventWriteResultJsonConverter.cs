using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Light.Results.CloudEvents.Writing.Json;

/// <summary>
/// JSON converter for <see cref="Result" /> that serializes results as CloudEvents JSON envelopes.
/// </summary>
public sealed class CloudEventWriteResultJsonConverter : JsonConverter<Result>
{
    private readonly LightResultsCloudEventWriteOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventWriteResultJsonConverter" />.
    /// </summary>
    /// <param name="options">The CloudEvent write options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public CloudEventWriteResultJsonConverter(LightResultsCloudEventWriteOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Throws a <see cref="NotSupportedException" /> because this converter only supports writing.
    /// </summary>
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventWriteResultJsonConverter)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified result as a CloudEvents envelope.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, Result result, JsonSerializerOptions options) =>
        result.WriteCloudEvent(
            writer,
            successType: _options.SuccessType,
            failureType: _options.FailureType,
            id: _options.IdResolver?.Invoke() ?? Guid.NewGuid().ToString(),
            source: _options.Source,
            subject: _options.Subject,
            dataschema: _options.DataSchema,
            time: _options.Time,
            options: _options
        );
}

/// <summary>
/// JSON converter for <see cref="Result{T}" /> that serializes results as CloudEvents JSON envelopes.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public sealed class CloudEventWriteResultJsonConverter<T> : JsonConverter<Result<T>>
{
    private readonly LightResultsCloudEventWriteOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="CloudEventWriteResultJsonConverter{T}" />.
    /// </summary>
    /// <param name="options">The CloudEvent write options.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public CloudEventWriteResultJsonConverter(LightResultsCloudEventWriteOptions options) =>
        _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Throws a <see cref="NotSupportedException" /> because this converter only supports writing.
    /// </summary>
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(
            $"{nameof(CloudEventWriteResultJsonConverter<T>)} supports serialization only. Use a deserialization converter for reading."
        );

    /// <summary>
    /// Writes the JSON representation for the specified result as a CloudEvents envelope.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="result">The result to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, Result<T> result, JsonSerializerOptions options) =>
        result.WriteCloudEvent(
            writer,
            successType: _options.SuccessType,
            failureType: _options.FailureType,
            id: _options.IdResolver?.Invoke() ?? Guid.NewGuid().ToString(),
            source: _options.Source,
            subject: _options.Subject,
            dataschema: _options.DataSchema,
            time: _options.Time,
            options: _options
        );
}
