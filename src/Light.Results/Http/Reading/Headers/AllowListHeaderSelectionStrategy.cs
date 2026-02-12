using System;
using System.Collections.Generic;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// A header selection strategy that only allows headers contained in a predefined allow list.
/// </summary>
public sealed class AllowListHeaderSelectionStrategy : IHttpHeaderSelectionStrategy
{
    private readonly HashSet<string> _allowedHeaderNames;

    /// <summary>
    /// Initializes a new instance of <see cref="AllowListHeaderSelectionStrategy" />.
    /// This strategy uses <see cref="StringComparer.OrdinalIgnoreCase" /> to compare header names in a
    /// case-insensitive manner.
    /// </summary>
    /// <param name="allowedHeaderNames">The list of allowed header names.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedHeaderNames" /> is <c>null</c>.</exception>
    public AllowListHeaderSelectionStrategy(params IEnumerable<string> allowedHeaderNames)
        : this(StringComparer.OrdinalIgnoreCase, allowedHeaderNames) { }

    /// <summary>
    /// Initializes a new instance of <see cref="AllowListHeaderSelectionStrategy" />.
    /// </summary>
    /// <param name="comparer">The comparer to use for header names.</param>
    /// <param name="allowedHeaderNames">The list of allowed header names.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedHeaderNames" /> is <c>null</c>.</exception>
    public AllowListHeaderSelectionStrategy(
        IEqualityComparer<string> comparer,
        params IEnumerable<string> allowedHeaderNames
    )
        : this(
            new HashSet<string>(
                allowedHeaderNames ?? throw new ArgumentNullException(nameof(allowedHeaderNames)),
                comparer
            )
        ) { }

    /// <summary>
    /// Creates a new instance of <see cref="AllowListHeaderSelectionStrategy" />.
    /// </summary>
    /// <param name="allowedHeaderNames">The set of header names that are allowed to pass the filter.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedHeaderNames" /> is <see langword="null" />.</exception>
    public AllowListHeaderSelectionStrategy(HashSet<string> allowedHeaderNames) =>
        _allowedHeaderNames = allowedHeaderNames ?? throw new ArgumentNullException(nameof(allowedHeaderNames));

    /// <summary>
    /// Returns <see langword="true" /> when the supplied header name exists in the allow list.
    /// </summary>
    /// <param name="headerName">The header name being evaluated.</param>
    public bool ShouldInclude(string headerName) => _allowedHeaderNames.Contains(headerName);
}
