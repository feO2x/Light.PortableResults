using System;
using System.Net;
using Light.PortableResults.Http;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Headers;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable ConvertToExtensionBlock

namespace Light.PortableResults.AspNetCore.Shared;

/// <summary>
/// Provides helper methods to translate <see cref="Result{T}" /> and <see cref="Result" /> instances
/// into values for ASP.NET Core <see cref="HttpResponse" />.
/// </summary>
public static class HttpExtensions
{
    /// <summary>
    /// Derives and assigns the appropriate HTTP status code for the specified result.
    /// </summary>
    /// <param name="httpResponse">The HTTP response to update.</param>
    /// <param name="result">The Light result providing validation state and errors.</param>
    /// <param name="successStatusCode">Optional status code to use on success (defaults to 200 OK).</param>
    /// <param name="firstErrorCategoryIsLeadingCategory">
    /// Determines whether the first error category should be treated as the leading category when calculating the
    /// status code.
    /// </param>
    /// <typeparam name="TResult">The concrete result struct implementing <see cref="IResultObject" />.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpResponse" /> is <c>null</c>.</exception>
    public static void SetStatusCodeFromResult<TResult>(
        this HttpResponse httpResponse,
        TResult result,
        HttpStatusCode? successStatusCode = null,
        bool firstErrorCategoryIsLeadingCategory = true
    )
        where TResult : struct, IResultObject
    {
        ArgumentNullException.ThrowIfNull(httpResponse);

        HttpStatusCode statusCode;
        if (result.IsValid)
        {
            statusCode = successStatusCode ?? HttpStatusCode.OK;
        }
        else
        {
            statusCode = result.Errors.GetLeadingCategory(firstErrorCategoryIsLeadingCategory).ToHttpStatusCode();
        }

        httpResponse.StatusCode = (int) statusCode;
    }

    /// <summary>
    /// Sets the response content type based on result validity, value, and metadata serialization rules.
    /// </summary>
    /// <param name="httpResponse">The HTTP response to update.</param>
    /// <param name="result">The Light result governing the content type choice.</param>
    /// <param name="metadataSerializationMode">Controls when metadata should be serialized.</param>
    /// <typeparam name="TResult">The concrete result struct implementing <see cref="IResultObject" />.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpResponse" /> is <c>null</c>.</exception>
    public static void SetContentTypeFromResult<TResult>(
        this HttpResponse httpResponse,
        TResult result,
        MetadataSerializationMode metadataSerializationMode
    )
        where TResult : struct, IResultObject
    {
        ArgumentNullException.ThrowIfNull(httpResponse);

        if (!result.IsValid)
        {
            // When the result is erroneous, we always write problem details
            httpResponse.ContentType = "application/problem+json";
            return;
        }

        if (!result.HasValue &&
            (
                !result.Metadata.HasValue ||
                metadataSerializationMode != MetadataSerializationMode.Always ||
                !result.Metadata.Value.HasAnyValuesWithAnnotation(MetadataValueAnnotation.SerializeInHttpResponseBody)
            ))
        {
            // Else, we do not set any content type at all
            return;
        }

        // When the result is successful and has a value, or when it is successful and has metadata
        // that needs to be serialized, we write JSON.
        httpResponse.ContentType = "application/json";
    }

    /// <summary>
    /// Writes metadata values marked for header serialization into the HTTP response headers.
    /// </summary>
    /// <param name="httpResponse">The HTTP response receiving the headers.</param>
    /// <param name="result">The Light result containing optional metadata.</param>
    /// <param name="conversionService">Service translating metadata entries into HTTP headers.</param>
    /// <typeparam name="TResult">The concrete result struct implementing <see cref="IResultObject" />.</typeparam>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpResponse" /> or
    /// <paramref name="conversionService" /> is <c>null</c>.
    /// </exception>
    public static void SetMetadataValuesAsHeadersIfNecessary<TResult>(
        this HttpResponse httpResponse,
        TResult result,
        IHttpHeaderConversionService conversionService
    )
        where TResult : struct, IResultObject
    {
        ArgumentNullException.ThrowIfNull(httpResponse);
        ArgumentNullException.ThrowIfNull(conversionService);

        if (result.Metadata is null)
        {
            return;
        }

        foreach (var (key, metadataValue) in result.Metadata.Value)
        {
            if (metadataValue.IsNull || !metadataValue.HasAnnotation(MetadataValueAnnotation.SerializeInHttpHeader))
            {
                continue;
            }

            var preparedHttpHeader = conversionService.PrepareHttpHeader(key, metadataValue);
            httpResponse.Headers.Add(preparedHttpHeader);
        }
    }

    /// <summary>
    /// Resolves Light result options from an override or the current HTTP context service provider.
    /// </summary>
    /// <param name="httpContext">The active HTTP context.</param>
    /// <param name="overrideOptions">Optional options instance to use instead of the registered one.</param>
    /// <returns>The resolved <see cref="PortableResultsHttpWriteOptions" /> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no options can be resolved.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="httpContext" /> is <c>null</c>.</exception>
    public static PortableResultsHttpWriteOptions ResolvePortableResultsHttpWriteOptions(
        this HttpContext httpContext,
        PortableResultsHttpWriteOptions? overrideOptions = null
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return overrideOptions ??
               httpContext.RequestServices.GetService<IOptions<PortableResultsHttpWriteOptions>>()?.Value ??
               throw new InvalidOperationException(
                   "No PortableResultsHttpWriteOptions are configured in the DI container"
               );
    }
}
