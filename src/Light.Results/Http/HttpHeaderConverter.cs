using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http;

/// <summary>
/// Base type for converting metadata values into HTTP headers.
/// </summary>
public abstract class HttpHeaderConverter
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpHeaderConverter" />.
    /// </summary>
    /// <param name="supportedMetadataKeys">The metadata keys supported by this converter.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="supportedMetadataKeys" /> is default or empty.
    /// </exception>
    protected HttpHeaderConverter(ImmutableArray<string> supportedMetadataKeys)
    {
        if (supportedMetadataKeys.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                $"{nameof(supportedMetadataKeys)} must not be empty",
                nameof(supportedMetadataKeys)
            );
        }

        SupportedMetadataKeys = supportedMetadataKeys;
    }

    /// <summary>
    /// Gets the metadata keys supported by this converter.
    /// </summary>
    public ImmutableArray<string> SupportedMetadataKeys { get; }

    /// <summary>
    /// Converts the specified metadata value into an HTTP header.
    /// </summary>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The header key and value pair.</returns>
    public abstract KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value);
}
