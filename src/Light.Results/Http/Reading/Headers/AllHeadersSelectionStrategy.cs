namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// A header selection strategy that includes every HTTP header.
/// </summary>
public sealed class AllHeadersSelectionStrategy : IHttpHeaderSelectionStrategy
{
    /// <summary>
    /// Gets the singleton instance that can be reused to avoid extra allocations.
    /// </summary>
    public static AllHeadersSelectionStrategy Instance { get; } = new ();

    /// <summary>
    /// Always returns <see langword="true" /> to indicate that all headers should be included.
    /// </summary>
    /// <param name="headerName">The header name being evaluated.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool ShouldInclude(string headerName) => true;
}
