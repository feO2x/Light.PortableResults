using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// A source-generated resolver that declares the consumer's result value type and nothing else.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieDto))]
public sealed partial class MovieJsonContext : JsonSerializerContext;

/// <summary>
/// A resolver that supplies no contract for any type. It proves that non-generic results require no consumer
/// registration, because every type involved is resolved by Light.PortableResults itself.
/// </summary>
public sealed class NoContractsJsonTypeInfoResolver : IJsonTypeInfoResolver
{
    public static NoContractsJsonTypeInfoResolver Instance { get; } = new ();

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
}
