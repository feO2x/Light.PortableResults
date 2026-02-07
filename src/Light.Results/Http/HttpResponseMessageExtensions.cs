using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Light.Results.Metadata;

namespace Light.Results.Http;

/// <summary>
/// Provides extensions for deserializing Light.Results from <see cref="HttpResponseMessage" /> instances.
/// </summary>
public static class HttpResponseMessageExtensions
{
    private const string FailurePayloadRequiredMessage = "Failure responses must include a problem details payload.";

    private const string GenericSuccessPayloadRequiredMessage =
        "Successful responses for Result<T> must include a payload.";

    private const string NonGenericFailureMustDeserializeToFailedMessage =
        "Failure responses must deserialize into failed Result payloads.";

    private const string GenericFailureMustDeserializeToFailedMessage =
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
        LightResultHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var resolvedOptions = options ?? LightResultHttpReadOptions.Default;
        var serializerOptions = ResolveSerializerOptions(resolvedOptions);

        var isProblemDetails = CheckIfResponseContainsProblemDetails(response);
        var isFailure = !response.IsSuccessStatusCode ||
                        (resolvedOptions.TreatProblemDetailsAsFailure && isProblemDetails);
        var result = await ReadBodyResultAsync(response, serializerOptions, isFailure, cancellationToken)
           .ConfigureAwait(false);
        return MergeHeaderMetadataIfNeeded(response, resolvedOptions, result);
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
        LightResultHttpReadOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var resolvedOptions = options ?? LightResultHttpReadOptions.Default;
        var serializerOptions = ResolveSerializerOptions(resolvedOptions);

        var isProblemDetails = CheckIfResponseContainsProblemDetails(response);
        var isFailure = !response.IsSuccessStatusCode ||
                        (resolvedOptions.TreatProblemDetailsAsFailure && isProblemDetails);
        var result = await ReadBodyResultAsync<T>(response, serializerOptions, isFailure, cancellationToken)
           .ConfigureAwait(false);
        return MergeHeaderMetadataIfNeeded(response, resolvedOptions, result);
    }

    private static bool CheckIfResponseContainsProblemDetails(HttpResponseMessage response)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType;
        return mediaType is not null &&
               mediaType.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Result> ReadBodyResultAsync(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        CancellationToken cancellationToken
    ) =>
        await ReadBodyResultAsync(
                response,
                serializerOptions,
                isFailure,
                createEmptySuccessResult: static () => Result.Ok(),
                successEmptyBodyMessage: null,
                resultIsValid: static result => result.IsValid,
                invalidFailureResultMessage: NonGenericFailureMustDeserializeToFailedMessage,
                cancellationToken
            )
           .ConfigureAwait(false);

    private static async Task<Result<T>> ReadBodyResultAsync<T>(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        CancellationToken cancellationToken
    ) =>
        await ReadBodyResultAsync<Result<T>>(
                response,
                serializerOptions,
                isFailure,
                createEmptySuccessResult: null,
                successEmptyBodyMessage: GenericSuccessPayloadRequiredMessage,
                resultIsValid: static result => result.IsValid,
                invalidFailureResultMessage: GenericFailureMustDeserializeToFailedMessage,
                cancellationToken
            )
           .ConfigureAwait(false);

    private static async Task<TResult> ReadBodyResultAsync<TResult>(
        HttpResponseMessage response,
        JsonSerializerOptions serializerOptions,
        bool isFailure,
        Func<TResult>? createEmptySuccessResult,
        string? successEmptyBodyMessage,
        Func<TResult, bool> resultIsValid,
        string invalidFailureResultMessage,
        CancellationToken cancellationToken
    )
    {
        if (TryGetContentLength(response, out var contentLength))
        {
            if (contentLength == 0)
            {
                return HandleEmptyBody(isFailure, createEmptySuccessResult, successEmptyBodyMessage);
            }

            var deserializedFromStream = await DeserializeResultFromStreamAsync<TResult>(
                    response.Content!, // Call to TryGetContentLength proves Content is not null
                    serializerOptions,
                    cancellationToken
                )
               .ConfigureAwait(false);
            EnsureFailureResultIsInvalid(isFailure, resultIsValid(deserializedFromStream), invalidFailureResultMessage);
            return deserializedFromStream;
        }

        var contentBytes = await ReadContentBytesAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentBytes.Length == 0)
        {
            return HandleEmptyBody(isFailure, createEmptySuccessResult, successEmptyBodyMessage);
        }

        var deserializedFromBytes = DeserializeResultFromBytes<TResult>(contentBytes, serializerOptions);
        EnsureFailureResultIsInvalid(isFailure, resultIsValid(deserializedFromBytes), invalidFailureResultMessage);
        return deserializedFromBytes;
    }

    private static TResult HandleEmptyBody<TResult>(
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

    private static void EnsureFailureResultIsInvalid(
        bool isFailure,
        bool isResultValid,
        string invalidFailureResultMessage
    )
    {
        if (isFailure && isResultValid)
        {
            throw new JsonException(invalidFailureResultMessage);
        }
    }

    private static TResult DeserializeResultFromBytes<TResult>(
        byte[] contentBytes,
        JsonSerializerOptions serializerOptions
    )
    {
        var deserialized = JsonSerializer.Deserialize<TResult>(contentBytes, serializerOptions);
        if (deserialized is null)
        {
            throw new JsonException("Response body could not be deserialized to the expected result type.");
        }

        return deserialized;
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

    private static async Task<TResult> DeserializeResultFromStreamAsync<TResult>(
        HttpContent content,
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
        var deserialized = await JsonSerializer.DeserializeAsync<TResult>(stream, serializerOptions, cancellationToken)
           .ConfigureAwait(false);
        if (deserialized is null)
        {
            throw new JsonException("Response body could not be deserialized to the expected result type.");
        }

        return deserialized;
    }

    private static async Task<byte[]> ReadContentBytesAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.Content is null)
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    private static JsonSerializerOptions ResolveSerializerOptions(LightResultHttpReadOptions options) =>
        options.SerializerOptions ?? HttpReadJsonSerializerOptionsCache.GetByPreference(options.PreferSuccessPayload);

    private static TResult MergeHeaderMetadataIfNeeded<TResult>(
        HttpResponseMessage response,
        LightResultHttpReadOptions options,
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

    private static MetadataObject? ReadHeaderMetadata(HttpResponseMessage response, LightResultHttpReadOptions options)
    {
        if (options.HeaderSelectionMode == HeaderSelectionMode.None)
        {
            return null;
        }

        var allowList = options.HeaderAllowList is { Count: > 0 } ?
            new HashSet<string>(options.HeaderAllowList, StringComparer.OrdinalIgnoreCase) :
            null;
        var denyList = options.HeaderDenyList is { Count: > 0 } ?
            new HashSet<string>(options.HeaderDenyList, StringComparer.OrdinalIgnoreCase) :
            null;

        var parsingService = options.HeaderParsingService;

        var builder = MetadataObjectBuilder.Create();
        try
        {
            AppendHeaders(response.Headers, options, parsingService, ref builder, allowList, denyList);
            if (response.Content is not null)
            {
                AppendHeaders(response.Content.Headers, options, parsingService, ref builder, allowList, denyList);
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
        LightResultHttpReadOptions options,
        IHttpHeaderParsingService parsingService,
        ref MetadataObjectBuilder builder,
        HashSet<string>? allowList,
        HashSet<string>? denyList
    )
    {
        foreach (var header in headers)
        {
            var headerName = header.Key;
            if (!ShouldIncludeHeader(headerName, options, allowList, denyList))
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

    private static bool ShouldIncludeHeader(
        string headerName,
        LightResultHttpReadOptions options,
        HashSet<string>? allowList,
        HashSet<string>? denyList
    )
    {
        return options.HeaderSelectionMode switch
        {
            HeaderSelectionMode.None => false,
            HeaderSelectionMode.All => true,
            HeaderSelectionMode.AllowList => allowList is not null && allowList.Contains(headerName),
            HeaderSelectionMode.DenyList => denyList is null || !denyList.Contains(headerName),
            _ => false
        };
    }
}
