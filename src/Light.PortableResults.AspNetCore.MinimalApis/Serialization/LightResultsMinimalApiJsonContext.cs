using System.Text.Json.Serialization;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Metadata;

namespace Light.PortableResults.AspNetCore.MinimalApis.Serialization;

/// <summary>
/// Source-generated JSON serializer context for Light.PortableResults for Minimal APIs.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(MetadataValue))]
[JsonSerializable(typeof(MetadataObject))]
[JsonSerializable(typeof(HttpResultForWriting))]
public sealed partial class LightResultsMinimalApiJsonContext : JsonSerializerContext;
