using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.MinimalApis.Serialization;

/// <summary>
/// Source-generated JSON serializer context for Light.Results for Minimal APIs.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(MetadataValue))]
[JsonSerializable(typeof(MetadataObject))]
[JsonSerializable(typeof(Result))]
public sealed partial class LightResultsMinimalApiJsonContext : JsonSerializerContext;
