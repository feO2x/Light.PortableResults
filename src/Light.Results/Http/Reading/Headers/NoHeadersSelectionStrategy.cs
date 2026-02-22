namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// A header selection strategy that excludes every HTTP header.
/// </summary>
public sealed class NoHeadersSelectionStrategy : IHttpHeaderSelectionStrategy
{
    /// <summary>
    /// Gets the singleton instance that can be reused to avoid extra allocations.
    /// </summary>
    public static NoHeadersSelectionStrategy Instance { get; } = new ();

    /// <summary>
    /// Always returns <see langword="false" /> to indicate that no header should be included.
    /// </summary>
    /// <param name="headerName">The header name being evaluated.</param>
    /// <returns>Always <see langword="false" />.</returns>
    public bool ShouldInclude(string headerName) => false;
}
