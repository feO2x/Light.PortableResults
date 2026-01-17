using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.AspNetCore.Shared.Serialization;

namespace Light.Results.AspNetCore.MinimalApis.Serialization;

/// <summary>
/// Provides default JSON serializer options for Light.Results Minimal API serialization.
/// </summary>
public static class JsonDefaults
{
    static JsonDefaults()
    {
        Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = LightResultsMinimalApiJsonContext.Default
        };
        Options.Converters.Add(new MetadataValueJsonConverter());
        Options.Converters.Add(new MetadataObjectJsonConverter());
    }

    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions" /> configured for
    /// serializing <see cref="LightProblemDetailsResult" /> instances.
    /// </summary>
    public static JsonSerializerOptions Options { get; }
}
