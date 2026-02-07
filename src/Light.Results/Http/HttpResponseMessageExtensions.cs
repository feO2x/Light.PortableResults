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

        var contentBytes = await ReadContentBytesAsync(response, cancellationToken).ConfigureAwait(false);

        Result result;
        if (isFailure)
        {
            if (contentBytes.Length == 0)
            {
                throw new InvalidOperationException("Failure responses must include a problem details payload.");
            }

            result = JsonSerializer.Deserialize<Result>(contentBytes, serializerOptions);
            if (result.IsValid)
            {
                throw new JsonException("Failure responses must deserialize into failed Result payloads.");
            }
        }
        else
        {
            result = contentBytes.Length == 0 ?
                Result.Ok() :
                JsonSerializer.Deserialize<Result>(contentBytes, serializerOptions);
        }

        var headerMetadata = ReadHeaderMetadata(response, resolvedOptions);
        if (headerMetadata is null)
        {
            return result;
        }

        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(
            result.Metadata,
            headerMetadata,
            resolvedOptions.MergeStrategy
        );

        return mergedMetadata == result.Metadata ? result : result.ReplaceMetadata(mergedMetadata);
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

        var contentBytes = await ReadContentBytesAsync(response, cancellationToken).ConfigureAwait(false);

        Result<T> result;
        if (isFailure)
        {
            if (contentBytes.Length == 0)
            {
                throw new InvalidOperationException("Failure responses must include a problem details payload.");
            }

            result = JsonSerializer.Deserialize<Result<T>>(contentBytes, serializerOptions);
            if (result.IsValid)
            {
                throw new JsonException("Failure responses must deserialize into failed Result<T> payloads.");
            }
        }
        else
        {
            if (contentBytes.Length == 0)
            {
                throw new InvalidOperationException("Successful responses for Result<T> must include a payload.");
            }

            result = JsonSerializer.Deserialize<Result<T>>(contentBytes, serializerOptions);
        }

        var headerMetadata = ReadHeaderMetadata(response, resolvedOptions);
        if (headerMetadata is null)
        {
            return result;
        }

        var mergedMetadata = MetadataObjectExtensions.MergeIfNeeded(
            result.Metadata,
            headerMetadata,
            resolvedOptions.MergeStrategy
        );

        return mergedMetadata == result.Metadata ? result : result.ReplaceMetadata(mergedMetadata);
    }

    private static bool CheckIfResponseContainsProblemDetails(HttpResponseMessage response)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType;
        return mediaType is not null &&
               mediaType.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase);
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
