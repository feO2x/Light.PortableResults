using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Light.Results.Http.Reading.Headers;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading;

/// <summary>
/// Provides extensions for deserializing Light.Results from <see cref="HttpResponseMessage" /> instances.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Gets the error message for failure responses that do not contain a problem details payload.
    /// </summary>
    public const string FailurePayloadRequiredMessage = "Failure responses must include a problem details payload.";

    /// <summary>
    /// Gets the error message for successful responses that do not contain a payload.
    /// </summary>
    public const string GenericSuccessPayloadRequiredMessage =
        "Successful responses for Result<T> must include a payload.";

    /// <summary>
    /// Gets the error message indicating that a non-generic failure did not deserialize to a failed result payload.
    /// </summary>
    public const string NonGenericFailureMustDeserializeToFailedMessage =
        "Failure responses must deserialize into failed Result payloads.";

    /// <summary>
    /// Gets the error message indicating that a generic failure did not deserialize to a failed result payload.
    /// </summary>
    public const string GenericFailureMustDeserializeToFailedMessage =
        "Failure responses must deserialize into failed Result<T> payloads.";

    /// <summary>
    /// Reads a <see cref="Result" /> from the specified response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed result.</returns>
    public static async Task<Result> ReadResultAsync(
        this HttpResponseMessage response,
        LightResultsHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        options ??= LightResultsHttpReadOptions.Default;
        var isFailure = DetermineIfFailureResponse(response, options);

        var result = await ReadBodyResultAsync(response, options.SerializerOptions, isFailure, cancellationToken)
           .ConfigureAwait(false);

        return MergeHeaderMetadataIfNeeded(response, options, result);
    }

    /// <summary>
    /// Reads a <see cref="Result{T}" /> from the specified response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="options">Optional deserialization options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <returns>The parsed result.</returns>
    public static async Task<Result<T>> ReadResultAsync<T>(
        this HttpResponseMessage response,
        LightResultsHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        options ??= LightResultsHttpReadOptions.Default;
        var isFailure = DetermineIfFailureResponse(response, options);

        var result = await ReadBodyGenericResultAsync<T>(
                response,
                options.SerializerOptions,
                isFailure,
                options.PreferSuccessPayload,
                cancellationToken
            )
           .ConfigureAwait(false);

        return MergeHeaderMetadataIfNeeded(response, options, result);
    }

    private static bool DetermineIfFailureResponse(HttpResponseMessage response, LightResultsHttpReadOptions options)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType;
        var isProblemDetailsContentType = string.Equals(
            mediaType,
            "application/problem+json",
            StringComparison.OrdinalIgnoreCase
        );
        return !response.IsSuccessStatusCode || (options.TreatProblemDetailsAsFailure && isProblemDetailsContentType);
    }

    private static async Task<Result> ReadBodyResultAsync(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        CancellationToken cancellationToken
    )
    {
        var contentBytes = await ReadContentBytesOrNullAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentBytes is null || contentBytes.Length == 0)
        {
            return HandleEmptyBody(isFailure, static () => Result.Ok(), successEmptyBodyMessage: null);
        }

        if (isFailure)
        {
            var failurePayload = await DeserializeFromBytesAsync<HttpReadFailureResultPayload>(
                    contentBytes,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            EnsureFailurePayloadHasErrors(failurePayload.Errors, NonGenericFailureMustDeserializeToFailedMessage);
            return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        var successPayload = await DeserializeFromBytesAsync<HttpReadSuccessResultPayload>(
                contentBytes,
                serializerOptions,
                cancellationToken
            )
           .ConfigureAwait(false);
        return Result.Ok(successPayload.Metadata);
    }

    private static async Task<Result<T>> ReadBodyGenericResultAsync<T>(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        PreferSuccessPayload preferSuccessPayload,
        CancellationToken cancellationToken
    )
    {
        var contentBytes = await ReadContentBytesOrNullAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentBytes is null || contentBytes.Length == 0)
        {
            return HandleEmptyBody<Result<T>>(
                isFailure,
                createEmptySuccessResult: null,
                GenericSuccessPayloadRequiredMessage
            );
        }

        if (isFailure)
        {
            var failurePayload = await DeserializeFromBytesAsync<HttpReadFailureResultPayload>(
                    contentBytes,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            EnsureFailurePayloadHasErrors(failurePayload.Errors, GenericFailureMustDeserializeToFailedMessage);
            return Result<T>.Fail(failurePayload.Errors, failurePayload.Metadata);
        }

        return await DeserializeGenericSuccessPayloadAsync<T>(
                contentBytes,
                serializerOptions,
                NormalizePreference(preferSuccessPayload),
                cancellationToken
            )
           .ConfigureAwait(false);
    }

    private static PreferSuccessPayload NormalizePreference(PreferSuccessPayload preference)
    {
        return preference == PreferSuccessPayload.BareValue || preference == PreferSuccessPayload.WrappedValue ?
            preference :
            PreferSuccessPayload.Auto;
    }

    private static async Task<Result<T>> DeserializeGenericSuccessPayloadAsync<T>(
        byte[] contentBytes,
        JsonSerializerOptions serializerOptions,
        PreferSuccessPayload preferSuccessPayload,
        CancellationToken cancellationToken
    )
    {
        if (preferSuccessPayload == PreferSuccessPayload.BareValue)
        {
            var barePayload = await DeserializeFromBytesAsync<HttpReadBareSuccessResultPayload<T>>(
                    contentBytes,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            return CreateSuccessfulGenericResult(barePayload.Value, metadata: null);
        }

        if (preferSuccessPayload == PreferSuccessPayload.WrappedValue)
        {
            var wrappedPayload = await DeserializeFromBytesAsync<HttpReadWrappedSuccessResultPayload<T>>(
                    contentBytes,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            return CreateSuccessfulGenericResult(wrappedPayload.Value, wrappedPayload.Metadata);
        }

        var autoPayload = await DeserializeFromBytesAsync<HttpReadAutoSuccessResultPayload<T>>(
                contentBytes,
                serializerOptions,
                cancellationToken
            )
           .ConfigureAwait(false);
        return CreateSuccessfulGenericResult(autoPayload.Value, autoPayload.Metadata);
    }

    private static Result<T> CreateSuccessfulGenericResult<T>(T value, MetadataObject? metadata)
    {
        try
        {
            return Result<T>.Ok(value, metadata);
        }
        catch (ArgumentNullException argumentNullException)
        {
            throw new JsonException("Result value cannot be null.", argumentNullException);
        }
    }

    private static TResult HandleEmptyBody<TResult>(
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local -- this is not a precondition check
        bool isFailure,
        Func<TResult>? createEmptySuccessResult,
        string? successEmptyBodyMessage
    )
    {
        if (isFailure)
        {
            throw new InvalidOperationException(FailurePayloadRequiredMessage);
        }

        if (createEmptySuccessResult is null)
        {
            throw new InvalidOperationException(successEmptyBodyMessage ?? GenericSuccessPayloadRequiredMessage);
        }

        return createEmptySuccessResult();
    }

    private static void EnsureFailurePayloadHasErrors(Errors errors, string errorMessage)
    {
        if (errors.IsEmpty)
        {
            throw new JsonException(errorMessage);
        }
    }

    private static async Task<byte[]?> ReadContentBytesOrNullAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (TryGetContentLength(response, out var contentLength) && contentLength == 0)
        {
            return [];
        }

        if (response.Content is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    private static bool TryGetContentLength(HttpResponseMessage response, out long contentLength)
    {
        var content = response.Content;
        if (content?.Headers.ContentLength is { } knownLength)
        {
            contentLength = knownLength;
            return true;
        }

        contentLength = 0;
        return false;
    }

    private static async Task<TPayload> DeserializeFromBytesAsync<TPayload>(
        byte[] contentBytes,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new MemoryStream(contentBytes, writable: false);
        var deserialized = await JsonSerializer
           .DeserializeAsync<TPayload>(stream, serializerOptions, cancellationToken)
           .ConfigureAwait(false);

        if (deserialized is null)
        {
            throw new JsonException("Response body could not be deserialized to the expected payload type.");
        }

        return deserialized;
    }

    private static TResult MergeHeaderMetadataIfNeeded<TResult>(
        HttpResponseMessage response,
        LightResultsHttpReadOptions options,
        TResult result
    )
        where TResult : struct, ICanReplaceMetadata<TResult>
    {
        var headerMetadata = ReadHeaderMetadata(response, options);
        if (headerMetadata is null)
        {
            return result;
        }

        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(
            result.Metadata,
            headerMetadata,
            options.MergeStrategy
        );

        return mergedMetadata == result.Metadata ? result : result.ReplaceMetadata(mergedMetadata);
    }

    private static MetadataObject? ReadHeaderMetadata(HttpResponseMessage response, LightResultsHttpReadOptions options)
    {
        var headerSelectionStrategy = options.HeaderSelectionStrategy;
        if (ReferenceEquals(headerSelectionStrategy, HttpHeaderSelectionStrategies.None))
        {
            return null;
        }

        var parsingService = options.HeaderParsingService;

        var builder = MetadataObjectBuilder.Create();
        try
        {
            AppendHeaders(response.Headers, options, parsingService, headerSelectionStrategy, ref builder);
            if (response.Content is not null)
            {
                AppendHeaders(response.Content.Headers, options, parsingService, headerSelectionStrategy, ref builder);
            }

            return builder.Count == 0 ? null : builder.Build();
        }
        finally
        {
            builder.Dispose();
        }
    }

    private static void AppendHeaders(
        HttpHeaders headers,
        LightResultsHttpReadOptions options,
        IHttpHeaderParsingService parsingService,
        IHttpHeaderSelectionStrategy headerSelectionStrategy,
        ref MetadataObjectBuilder builder
    )
    {
        foreach (var header in headers)
        {
            var headerName = header.Key;
            if (!headerSelectionStrategy.ShouldInclude(headerName))
            {
                continue;
            }

            var values = header.Value as string[] ?? new List<string>(header.Value).ToArray();
            var metadataEntry = parsingService.ParseHeader(headerName, values, options.HeaderMetadataAnnotation);

            if (builder.TryGetValue(metadataEntry.Key, out _))
            {
                if (options.HeaderConflictStrategy == HeaderConflictStrategy.Throw)
                {
                    throw new InvalidOperationException(
                        $"Header '{headerName}' maps to metadata key '{metadataEntry.Key}', which is already present."
                    );
                }

                builder.AddOrReplace(metadataEntry.Key, metadataEntry.Value);
                continue;
            }

            builder.Add(metadataEntry.Key, metadataEntry.Value);
        }
    }
}
