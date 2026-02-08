using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http.Writing.Headers;

/// <summary>
/// Default implementation of <see cref="IHttpHeaderConversionService" /> using a converter registry.
/// </summary>
public sealed class DefaultHttpHeaderConversionService : IHttpHeaderConversionService
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultHttpHeaderConversionService" />.
    /// </summary>
    /// <param name="converters">The converters keyed by metadata key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converters" /> is <see langword="null" />.</exception>
    public DefaultHttpHeaderConversionService(FrozenDictionary<string, HttpHeaderConverter> converters) =>
        Converters = converters ?? throw new ArgumentNullException(nameof(converters));

    /// <summary>
    /// Gets the converters keyed by metadata key.
    /// </summary>
    public FrozenDictionary<string, HttpHeaderConverter> Converters { get; }

    /// <summary>
    /// Converts a metadata value into an HTTP header.
    /// </summary>
    /// <param name="metadataKey">The metadata key.</param>
    /// <param name="metadataValue">The metadata value.</param>
    /// <returns>The header key and value pair.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadataKey" /> is <see langword="null" />.</exception>
    public KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue metadataValue) =>
        Converters.TryGetValue(metadataKey, out var targetConverter) ?
            targetConverter.PrepareHttpHeader(metadataKey, metadataValue) :
            new KeyValuePair<string, StringValues>(metadataKey, metadataValue.ToString());
}
