using System;
using System.Collections.Generic;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// A header selection strategy that excludes headers contained in a predefined deny list.
/// </summary>
public sealed class DenyListHeaderSelectionStrategy : IHttpHeaderSelectionStrategy
{
    private readonly HashSet<string> _deniedHeaderNames;

    /// <summary>
    /// Initializes a new instance of <see cref="DenyListHeaderSelectionStrategy" />.
    /// This strategy uses <see cref="StringComparer.OrdinalIgnoreCase" /> to compare header names in a
    /// case-insensitive manner.
    /// </summary>
    /// <param name="deniedHeaderNames">The header names that should be excluded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deniedHeaderNames" /> is <c>null</c>.</exception>
    public DenyListHeaderSelectionStrategy(params IEnumerable<string> deniedHeaderNames)
        : this(StringComparer.OrdinalIgnoreCase, deniedHeaderNames) { }


    /// <summary>
    /// Initializes a new instance of <see cref="DenyListHeaderSelectionStrategy" />.
    /// </summary>
    /// <param name="comparer">The comparer to use for header names.</param>
    /// <param name="deniedHeaderNames">The list of denied header names.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deniedHeaderNames" /> is <c>null</c>.</exception>
    public DenyListHeaderSelectionStrategy(
        IEqualityComparer<string> comparer,
        params IEnumerable<string> deniedHeaderNames
    )
        : this(
            new HashSet<string>(
                deniedHeaderNames ?? throw new ArgumentNullException(nameof(deniedHeaderNames)),
                comparer
            )
        ) { }

    /// <summary>
    /// Creates a new instance of <see cref="DenyListHeaderSelectionStrategy" />.
    /// </summary>
    /// <param name="deniedHeaderNames">The set of header names that should be excluded.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="deniedHeaderNames" /> is <see langword="null" />.</exception>
    public DenyListHeaderSelectionStrategy(HashSet<string> deniedHeaderNames) => _deniedHeaderNames =
        deniedHeaderNames ?? throw new ArgumentNullException(nameof(deniedHeaderNames));

    /// <summary>
    /// Returns <see langword="true" /> when the supplied header name is not present in the deny list.
    /// </summary>
    /// <param name="headerName">The header name being evaluated.</param>
    public bool ShouldInclude(string headerName) => !_deniedHeaderNames.Contains(headerName);
}
