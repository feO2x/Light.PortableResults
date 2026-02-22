using System.Net.Http.Headers;
using Light.Results.Metadata;

namespace Light.Results.Http.Reading.Headers;

/// <summary>
/// An <see cref="IHttpHeaderParsingService" /> that never reads any headers.
/// </summary>
public sealed class ParseNoHttpHeadersService : IHttpHeaderParsingService
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ParseNoHttpHeadersService Instance { get; } = new ();

    /// <summary>
    /// Always returns <see langword="null" /> because no headers are read.
    /// </summary>
    /// <param name="responseHeaders">The HTTP response headers (ignored).</param>
    /// <param name="contentHeaders">The optional HTTP content headers (ignored).</param>
    /// <returns>Always <see langword="null" />.</returns>
    public MetadataObject? ReadHeaderMetadata(
        HttpResponseHeaders responseHeaders,
        HttpContentHeaders? contentHeaders
    ) => null;
}
