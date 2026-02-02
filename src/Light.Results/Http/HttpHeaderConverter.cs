using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http;

public abstract class HttpHeaderConverter
{
    protected HttpHeaderConverter(ImmutableArray<string> supportedMetadataKeys)
    {
        if (supportedMetadataKeys.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                $"{nameof(supportedMetadataKeys)} must not be empty",
                nameof(supportedMetadataKeys)
            );
        }

        SupportedMetadataKeys = supportedMetadataKeys;
    }

    public ImmutableArray<string> SupportedMetadataKeys { get; }

    public abstract KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue value);
}
