using System.Text.Json.Serialization;
using Light.Results.Metadata;

namespace Light.Results.AspNetCore.MinimalApis.Serialization;

/// <summary>
/// Source-generated JSON serializer context for <see cref="AspNetCore.Serialization.LightProblemDetailsJsonContext.LightProblemDetails" />.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(LightProblemDetails))]
[JsonSerializable(typeof(MetadataValue))]
[JsonSerializable(typeof(MetadataObject))]
public partial class LightProblemDetailsJsonContext : JsonSerializerContext;
