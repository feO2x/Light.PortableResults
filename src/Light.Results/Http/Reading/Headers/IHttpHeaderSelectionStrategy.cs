namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// Strategy for deciding whether a header should be included in metadata deserialization.
/// </summary>
public interface IHttpHeaderSelectionStrategy
{
    /// <summary>
    /// Returns <see langword="true" /> when the specified header should be included.
    /// </summary>
    /// <param name="headerName">The HTTP header name.</param>
    /// <returns><see langword="true" /> if the header should be included; otherwise, <see langword="false" />.</returns>
    bool ShouldInclude(string headerName);
}
