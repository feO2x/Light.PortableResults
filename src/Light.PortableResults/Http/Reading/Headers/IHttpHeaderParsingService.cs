using System.Net.Http.Headers;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.Http.Reading.Headers;

/// <summary>
/// Reads HTTP headers into a <see cref="MetadataObject" />.
/// </summary>
public interface IHttpHeaderParsingService
{
    /// <summary>
    /// Reads the headers from the specified response and content headers into a <see cref="MetadataObject" />.
    /// Returns <see langword="null" /> when no headers are selected or no metadata entries are produced.
    /// </summary>
    /// <param name="responseHeaders">The response-level headers.</param>
    /// <param name="contentHeaders">The content-level headers, or <see langword="null" /> when no content is present.</param>
    /// <returns>The metadata object containing the parsed headers, or <see langword="null" />.</returns>
    MetadataObject? ReadHeaderMetadata(HttpResponseHeaders responseHeaders, HttpContentHeaders? contentHeaders);
}
