using System.Text.Json.Serialization;
using Light.Results.Http.Writing;

namespace Light.Results.AspNetCore.MinimalApis.Tests;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(HttpResultForWriting<string>))]
[JsonSerializable(typeof(HttpResultForWriting<ContactDto>))]
[JsonSerializable(typeof(ContactDto))]
public sealed partial class ExtendedMinimalApiJsonContext : JsonSerializerContext;
