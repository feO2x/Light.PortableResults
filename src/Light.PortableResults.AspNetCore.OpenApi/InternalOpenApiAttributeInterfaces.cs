using System;
using Light.PortableResults.SharedJsonSerialization;

namespace Light.PortableResults.AspNetCore.OpenApi;

internal interface IPortableSuccessResponseOpenApiAttribute
{
    Type ValueType { get; }
    MetadataSerializationMode MetadataSerializationMode { get; }
    bool HasMetadataSerializationModeOverride { get; }
}
