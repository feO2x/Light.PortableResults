using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading;

/// <summary>
/// Provides extensions for deserializing Light.PortableResults from <see cref="HttpResponseMessage" /> instances.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response" /> is <see langword="null" />.</exception>
    public static async Task<Result> ReadResultAsync(
        this HttpResponseMessage response,
        PortableResultsHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        options ??= PortableResultsHttpReadOptions.Default;
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response" /> is <see langword="null" />.</exception>
    public static async Task<Result<T>> ReadResultAsync<T>(
        this HttpResponseMessage response,
        PortableResultsHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        options ??= PortableResultsHttpReadOptions.Default;
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

    private static bool DetermineIfFailureResponse(HttpResponseMessage response, PortableResultsHttpReadOptions options)
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
        var contentStream = await CreateContentReadStreamAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentStream is null)
        {
            return HandleEmptyBody(isFailure, static () => Result.Ok(), successEmptyBodyMessage: null);
        }

        using (contentStream)
        {
            if (isFailure)
            {
                var failurePayload = await JsonSerializer
                   .DeserializeAsync<HttpReadFailureResultPayload>(contentStream, serializerOptions, cancellationToken)
                   .ConfigureAwait(false);
                EnsureFailurePayloadHasErrors(failurePayload.Errors, NonGenericFailureMustDeserializeToFailedMessage);
                return Result.Fail(failurePayload.Errors, failurePayload.Metadata);
            }

            var successPayload = await JsonSerializer
               .DeserializeAsync<HttpReadSuccessResultPayload>(contentStream, serializerOptions, cancellationToken)
               .ConfigureAwait(false);
            return Result.Ok(successPayload.Metadata);
        }
    }

    private static async Task<Result<T>> ReadBodyGenericResultAsync<T>(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        PreferSuccessPayload preferSuccessPayload,
        CancellationToken cancellationToken
    )
    {
        var contentStream = await CreateContentReadStreamAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentStream is null)
        {
            return HandleEmptyBody<Result<T>>(
                isFailure,
                createEmptySuccessResult: null,
                GenericSuccessPayloadRequiredMessage
            );
        }

        using (contentStream)
        {
            if (isFailure)
            {
                var failurePayload = await JsonSerializer
                   .DeserializeAsync<HttpReadFailureResultPayload>(contentStream, serializerOptions, cancellationToken)
                   .ConfigureAwait(false);
                EnsureFailurePayloadHasErrors(failurePayload.Errors, GenericFailureMustDeserializeToFailedMessage);
                return Result<T>.Fail(failurePayload.Errors, failurePayload.Metadata);
            }

            return await DeserializeGenericSuccessPayloadAsync<T>(
                    contentStream,
                    serializerOptions,
                    NormalizePreference(preferSuccessPayload),
                    cancellationToken
                )
               .ConfigureAwait(false);
        }
    }

    private static PreferSuccessPayload NormalizePreference(PreferSuccessPayload preference)
    {
        return preference == PreferSuccessPayload.BareValue || preference == PreferSuccessPayload.WrappedValue ?
            preference :
            PreferSuccessPayload.Auto;
    }

    private static async Task<Result<T>> DeserializeGenericSuccessPayloadAsync<T>(
        Stream contentStream,
        JsonSerializerOptions serializerOptions,
        PreferSuccessPayload preferSuccessPayload,
        CancellationToken cancellationToken
    )
    {
        if (preferSuccessPayload == PreferSuccessPayload.BareValue)
        {
            var barePayload = await JsonSerializer
               .DeserializeAsync<HttpReadBareSuccessResultPayload<T>>(
                    contentStream,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            return CreateSuccessfulGenericResult(barePayload.Value, metadata: null);
        }

        if (preferSuccessPayload == PreferSuccessPayload.WrappedValue)
        {
            var wrappedPayload = await JsonSerializer
               .DeserializeAsync<HttpReadWrappedSuccessResultPayload<T>>(
                    contentStream,
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            return CreateSuccessfulGenericResult(wrappedPayload.Value, wrappedPayload.Metadata);
        }

        var autoPayload = await JsonSerializer
           .DeserializeAsync<HttpReadAutoSuccessResultPayload<T>>(contentStream, serializerOptions, cancellationToken)
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

    private static async Task<Stream?> CreateContentReadStreamAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.Content is null)
        {
            return null;
        }

        if (response.Content.Headers.ContentLength is 0L)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var stream = await response.Content.ReadAsStreamAsync();

        switch (stream.CanSeek)
        {
            case true when stream.Length == 0L:
                stream.Dispose();
                return null;
            default: return stream;
        }
    }

    private static TResult MergeHeaderMetadataIfNeeded<TResult>(
        HttpResponseMessage response,
        PortableResultsHttpReadOptions options,
        TResult result
    )
        where TResult : struct, ICanReplaceMetadata<TResult>
    {
        var headerMetadata = options.HeaderParsingService.ReadHeaderMetadata(
            response.Headers,
            response.Content?.Headers
        );
        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(
            headerMetadata,
            result.Metadata,
            options.MergeStrategy
        );
        return mergedMetadata is null ? result : result.ReplaceMetadata(mergedMetadata);
    }
}
