using System.Collections.Generic;
using Light.Results.Metadata;
using Microsoft.Extensions.Primitives;

namespace Light.Results.Http;

public interface IHttpHeaderConversionService
{
    KeyValuePair<string, StringValues> PrepareHttpHeader(string metadataKey, MetadataValue metadataValue);
}
