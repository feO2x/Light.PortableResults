using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Light.Results.CloudEvents.Reading;

/// <summary>
/// Provides helpers for building CloudEvents extension attribute parser registries.
/// </summary>
public static class CloudEventsAttributeParserRegistry
{
    /// <summary>
    /// Builds a frozen registry of parsers keyed by attribute name.
    /// </summary>
    /// <param name="parsers">The parsers to include in the registry.</param>
    /// <param name="attributeNameComparer">Optional attribute name comparer.</param>
    /// <returns>The frozen registry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parsers" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when multiple parsers register the same attribute name.
    /// </exception>
    public static FrozenDictionary<string, CloudEventsAttributeParser> Create(
        IEnumerable<CloudEventsAttributeParser> parsers,
        IEqualityComparer<string>? attributeNameComparer = null
    )
    {
        if (parsers is null)
        {
            throw new ArgumentNullException(nameof(parsers));
        }

        attributeNameComparer ??= StringComparer.Ordinal;
        var dictionary = new Dictionary<string, CloudEventsAttributeParser>(attributeNameComparer);

        foreach (var parser in parsers)
        {
            foreach (var attributeName in parser.SupportedAttributeNames)
            {
                if (dictionary.TryGetValue(attributeName, out var existingParser))
                {
                    throw new InvalidOperationException(
                        $"Cannot add '{parser}' to registry because attribute '{attributeName}' is already registered by '{existingParser}'"
                    );
                }

                dictionary[attributeName] = parser;
            }
        }

        return dictionary.ToFrozenDictionary(attributeNameComparer);
    }
}
