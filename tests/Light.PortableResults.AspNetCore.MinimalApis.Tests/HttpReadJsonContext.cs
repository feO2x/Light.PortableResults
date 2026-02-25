using System.Text.Json.Serialization;
using Light.PortableResults.Http.Reading.Json;

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata
)]
[JsonSerializable(typeof(ContactDto))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(HttpReadSuccessResultPayload))]
[JsonSerializable(typeof(HttpReadFailureResultPayload))]
[JsonSerializable(typeof(HttpReadAutoSuccessResultPayload<ContactDto>))]
[JsonSerializable(typeof(HttpReadBareSuccessResultPayload<ContactDto>))]
[JsonSerializable(typeof(HttpReadWrappedSuccessResultPayload<ContactDto>))]
public sealed partial class HttpReadJsonContext : JsonSerializerContext;
