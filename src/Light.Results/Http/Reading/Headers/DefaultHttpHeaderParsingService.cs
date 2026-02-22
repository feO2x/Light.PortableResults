using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// Default implementation of <see cref="IHttpHeaderParsingService" /> using a parser registry.
/// </summary>
public sealed class DefaultHttpHeaderParsingService : IHttpHeaderParsingService
{
    private readonly HeaderValueParsingMode _headerValueParsingMode;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultHttpHeaderParsingService" />.
    /// </summary>
    /// <param name="selectionStrategy">The strategy deciding which headers to include.</param>
    /// <param name="parsers">The parsers keyed by header name.</param>
    /// <param name="conflictStrategy">How conflicts are handled when multiple headers map to the same metadata key.</param>
    /// <param name="metadataAnnotation">The annotation applied to metadata values originating from headers.</param>
    /// <param name="headerValueParsingMode">The parsing mode for header values without a registered parser.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="selectionStrategy" /> is <see langword="null" />.
    /// </exception>
    public DefaultHttpHeaderParsingService(
        IHttpHeaderSelectionStrategy selectionStrategy,
        FrozenDictionary<string, HttpHeaderParser>? parsers = null,
        HeaderConflictStrategy conflictStrategy = HeaderConflictStrategy.Throw,
        MetadataValueAnnotation metadataAnnotation = MetadataValueAnnotation.SerializeInHttpHeader,
        HeaderValueParsingMode headerValueParsingMode = HeaderValueParsingMode.Primitive
    )
    {
        SelectionStrategy = selectionStrategy ?? throw new ArgumentNullException(nameof(selectionStrategy));
        Parsers = parsers ?? EmptyParsers;
        ConflictStrategy = conflictStrategy;
        MetadataAnnotation = metadataAnnotation;
        _headerValueParsingMode = headerValueParsingMode;
    }

    /// <summary>
    /// Gets a frozen dictionary containing no parsers.
    /// </summary>
    public static FrozenDictionary<string, HttpHeaderParser> EmptyParsers { get; } =
        new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase)
           .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the header selection strategy.
    /// </summary>
    public IHttpHeaderSelectionStrategy SelectionStrategy { get; }

    /// <summary>
    /// Gets the parsers keyed by header name.
    /// </summary>
    public FrozenDictionary<string, HttpHeaderParser> Parsers { get; }

    /// <summary>
    /// Gets how conflicts are handled when multiple headers map to the same metadata key.
    /// </summary>
    public HeaderConflictStrategy ConflictStrategy { get; }

    /// <summary>
    /// Gets the annotation applied to metadata values originating from headers.
    /// </summary>
    public MetadataValueAnnotation MetadataAnnotation { get; }

    /// <summary>
    /// Reads the headers from the specified response and content headers into a <see cref="MetadataObject" />.
    /// Returns <see langword="null" /> when no headers are selected or no metadata entries are produced.
    /// </summary>
    /// <param name="responseHeaders">The HTTP response headers.</param>
    /// <param name="contentHeaders">The optional HTTP content headers.</param>
    /// <returns>A <see cref="MetadataObject" /> containing the parsed header metadata, or <see langword="null" /> if no metadata was produced.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ConflictStrategy" /> is <see cref="HeaderConflictStrategy.Throw" /> and multiple headers map to the same metadata key.</exception>
    public MetadataObject? ReadHeaderMetadata(
        HttpResponseHeaders responseHeaders,
        HttpContentHeaders? contentHeaders
    )
    {
        var builder = MetadataObjectBuilder.Create();
        try
        {
            AppendHeaders(responseHeaders, ref builder);
            if (contentHeaders is not null)
            {
                AppendHeaders(contentHeaders, ref builder);
            }

            return builder.Count == 0 ? null : builder.Build();
        }
        finally
        {
            builder.Dispose();
        }
    }

    /// <summary>
    /// Parses the specified header into a metadata entry.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="values">The header values.</param>
    /// <param name="annotation">The annotation to apply to the parsed value.</param>
    /// <returns>The metadata key and value pair.</returns>
    public KeyValuePair<string, MetadataValue> ParseHeader(
        string headerName,
        IReadOnlyList<string> values,
        MetadataValueAnnotation annotation
    )
    {
        if (Parsers.TryGetValue(headerName, out var parser))
        {
            var parsedValue = parser.ParseHeader(headerName, values, annotation);
            return new KeyValuePair<string, MetadataValue>(parser.MetadataKey, parsedValue);
        }

        var defaultValue = ParseValues(values, annotation);
        return new KeyValuePair<string, MetadataValue>(headerName, defaultValue);
    }

    private void AppendHeaders(HttpHeaders headers, ref MetadataObjectBuilder builder)
    {
        foreach (var header in headers)
        {
            var headerName = header.Key;
            if (!SelectionStrategy.ShouldInclude(headerName))
            {
                continue;
            }

            var values = header.Value as IReadOnlyList<string> ?? header.Value.ToArray();
            var metadataEntry = ParseHeader(headerName, values, MetadataAnnotation);

            if (builder.TryGetValue(metadataEntry.Key, out _))
            {
                if (ConflictStrategy == HeaderConflictStrategy.Throw)
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

    private MetadataValue ParseValues(IReadOnlyList<string> values, MetadataValueAnnotation annotation)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Count == 1)
        {
            return ParseSingleValue(values[0], annotation, _headerValueParsingMode);
        }

        using var builder = MetadataArrayBuilder.Create(values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            builder.Add(ParseSingleValue(values[i], annotation, _headerValueParsingMode));
        }

        return MetadataValue.FromArray(builder.Build(), annotation);
    }

    private static MetadataValue ParseSingleValue(
        string value,
        MetadataValueAnnotation annotation,
        HeaderValueParsingMode headerValueParsingMode
    )
    {
        if (headerValueParsingMode == HeaderValueParsingMode.StringOnly)
        {
            return MetadataValue.FromString(value, annotation);
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return MetadataValue.FromBoolean(boolValue, annotation);
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var int64Value))
        {
            return MetadataValue.FromInt64(int64Value, annotation);
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue) &&
            !double.IsNaN(doubleValue) &&
            !double.IsInfinity(doubleValue))
        {
            return MetadataValue.FromDouble(doubleValue, annotation);
        }

        return MetadataValue.FromString(value, annotation);
    }
}
