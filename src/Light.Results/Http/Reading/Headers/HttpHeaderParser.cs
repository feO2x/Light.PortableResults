using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// Base type for parsing HTTP headers into metadata values.
/// </summary>
public abstract class HttpHeaderParser
{
    /// <summary>
    /// Initializes a new instance of <see cref="HttpHeaderParser" />.
    /// </summary>
    /// <param name="metadataKey">The metadata key to use for parsed values.</param>
    /// <param name="supportedHeaderNames">The header names supported by this parser.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="metadataKey" /> is null or whitespace, or when
    /// <paramref name="supportedHeaderNames" /> is default or empty.
    /// </exception>
    protected HttpHeaderParser(string metadataKey, ImmutableArray<string> supportedHeaderNames)
    {
        if (string.IsNullOrWhiteSpace(metadataKey))
        {
            throw new ArgumentException("Metadata key must not be null or whitespace.", nameof(metadataKey));
        }

        if (supportedHeaderNames.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                $"{nameof(supportedHeaderNames)} must not be empty",
                nameof(supportedHeaderNames)
            );
        }

        MetadataKey = metadataKey;
        SupportedHeaderNames = supportedHeaderNames;
    }

    /// <summary>
    /// Gets the metadata key assigned to parsed values.
    /// </summary>
    public string MetadataKey { get; }

    /// <summary>
    /// Gets the header names supported by this parser.
    /// </summary>
    public ImmutableArray<string> SupportedHeaderNames { get; }

    /// <summary>
    /// Parses the specified header values into a metadata value.
    /// </summary>
    /// <param name="headerName">The header name being parsed.</param>
    /// <param name="values">The header values.</param>
    /// <param name="annotation">The annotation to apply to the parsed value.</param>
    /// <returns>The parsed metadata value.</returns>
    public abstract MetadataValue ParseHeader(
        string headerName,
        IReadOnlyList<string> values,
        MetadataValueAnnotation annotation
    );
}
