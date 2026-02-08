using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// Provides helpers for building header parser registries.
/// </summary>
public static class HttpHeaderParserRegistry
{
    /// <summary>
    /// Builds a frozen registry of header parsers keyed by header name.
    /// </summary>
    /// <param name="parsers">The parsers to include in the registry.</param>
    /// <param name="headerNameComparer">Optional header name comparer.</param>
    /// <returns>The frozen registry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsers" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple parsers register the same header name.
    /// </exception>
    public static FrozenDictionary<string, HttpHeaderParser> Create(
        IEnumerable<HttpHeaderParser> parsers,
        IEqualityComparer<string>? headerNameComparer = null
    )
    {
        if (parsers is null)
        {
            throw new ArgumentNullException(nameof(parsers));
        }

        headerNameComparer ??= StringComparer.OrdinalIgnoreCase;
        var dictionary = new Dictionary<string, HttpHeaderParser>(headerNameComparer);

        foreach (var parser in parsers)
        {
            foreach (var headerName in parser.SupportedHeaderNames)
            {
                if (dictionary.TryGetValue(headerName, out var existingParser))
                {
                    throw new InvalidOperationException(
                        $"Cannot add '{parser}' to registry because header '{headerName}' is already registered by '{existingParser}'"
                    );
                }

                dictionary[headerName] = parser;
            }
        }

        return dictionary.ToFrozenDictionary(headerNameComparer);
    }
}
