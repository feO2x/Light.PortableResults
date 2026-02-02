using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http;

public sealed class DefaultHttpHeaderConversionService : IHttpHeaderConversionService
{
    public DefaultHttpHeaderConversionService(FrozenDictionary<string, HttpHeaderConverter> converters) =>
        Converters = converters ?? throw new ArgumentNullException(nameof(converters));

    public FrozenDictionary<string, HttpHeaderConverter> Converters { get; }

    public KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue metadataValue) =>
        Converters.TryGetValue(metadataKey, out var targetConverter) ?
            targetConverter.PrepareHttpHeader(metadataKey, metadataValue) :
            new KeyValuePair<string, StringValues>(metadataKey, metadataValue.ToString());
}
