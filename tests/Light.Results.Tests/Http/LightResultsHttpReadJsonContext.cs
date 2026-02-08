using System;
using System.Text.Json.Serialization;

namespace Light.Results.Tests.Http;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(Result<HttpReadContactDto>))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(HttpReadContactDto))]
public sealed partial class LightResultsHttpReadJsonContext : JsonSerializerContext;

public sealed record HttpReadContactDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}
