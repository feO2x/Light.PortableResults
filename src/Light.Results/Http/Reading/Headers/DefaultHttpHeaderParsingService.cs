using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// Default implementation of <see cref="IHttpHeaderParsingService" /> using a parser registry.
/// </summary>
public sealed class DefaultHttpHeaderParsingService : IHttpHeaderParsingService
{
    private static readonly FrozenDictionary<string, HttpHeaderParser> EmptyParsers =
        new Dictionary<string, HttpHeaderParser>(StringComparer.OrdinalIgnoreCase)
           .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly HeaderValueParsingMode _headerValueParsingMode;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultHttpHeaderParsingService" />.
    /// </summary>
    /// <param name="parsers">The parsers keyed by header name.</param>
    /// <param name="headerValueParsingMode">The parsing mode for header values without a registered parser.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsers" /> is <see langword="null" />.</exception>
    public DefaultHttpHeaderParsingService(
        FrozenDictionary<string, HttpHeaderParser> parsers,
        HeaderValueParsingMode headerValueParsingMode = HeaderValueParsingMode.Primitive
    )
    {
        Parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        _headerValueParsingMode = headerValueParsingMode;
    }

    /// <summary>
    /// Gets an empty parsing service instance.
    /// </summary>
    public static DefaultHttpHeaderParsingService Empty { get; } = new (EmptyParsers);

    /// <summary>
    /// Gets the parsers keyed by header name.
    /// </summary>
    public FrozenDictionary<string, HttpHeaderParser> Parsers { get; }

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
