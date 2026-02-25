using System.Text.Json.Serialization;
using Light.PortableResults.Http.Writing;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(HttpResultForWriting<string>))]
[JsonSerializable(typeof(HttpResultForWriting<ContactDto>))]
[JsonSerializable(typeof(ContactDto))]
public sealed partial class ExtendedMinimalApiJsonContext : JsonSerializerContext;
