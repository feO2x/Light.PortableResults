using System.Collections.Generic;
using Light.Results.Metadata;

namespace Light.Results.Http;

/// <summary>
/// Parses HTTP headers into metadata values.
/// </summary>
public interface IHttpHeaderParsingService
{
    /// <summary>
    /// Parses the specified header into a metadata entry.
    /// </summary>
    /// <param name="headerName">The header name.</param>
    /// <param name="values">The header values.</param>
    /// <param name="annotation">The annotation to apply to the parsed value.</param>
    /// <returns>The metadata key and value pair.</returns>
    KeyValuePair<string, MetadataValue> ParseHeader(
        string headerName,
        IReadOnlyList<string> values,
        MetadataValueAnnotation annotation
    );
}
