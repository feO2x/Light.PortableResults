using System.Collections.Generic;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http;

/// <summary>
/// Converts metadata values into HTTP headers.
/// </summary>
public interface IHttpHeaderConversionService
{
    /// <summary>
    /// Converts a metadata value into an HTTP header.
    /// </summary>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="metadataValue">The metadata value.</param>
    /// <returns>The header key and value pair.</returns>
    KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue metadataValue);
}
