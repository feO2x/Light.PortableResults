using System.Text.Json;
using System.Text.Json.Serialization;
using Light.Results.AspNetCore.Shared.Serialization;

namespace Light.Results.AspNetCore.MinimalApis.Serialization;

public static class JsonDefaults
{
    static JsonDefaults()
    {
        Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = LightProblemDetailsJsonContext.Default
        };
        Options.Converters.Add(new MetadataValueJsonConverter());
        Options.Converters.Add(new MetadataObjectJsonConverter());
    }

    public static JsonSerializerOptions Options { get; }
}
