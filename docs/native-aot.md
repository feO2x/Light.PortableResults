# Native AOT and trimming

Light.PortableResults avoids reflection on its serialization paths, so applications that use it can be trimmed and published with Native AOT. This page lists which packages are compatible, what you have to register in a `JsonSerializerContext`, and how the library resolves JSON contracts for its own types.

**Packages:** `Light.PortableResults`, `Light.PortableResults.Validation`, `Light.PortableResults.AspNetCore.MinimalApis`, `Light.PortableResults.AspNetCore.OpenApi`, and `Light.PortableResults.Validation.OpenApi`.

## Compatible packages

| Package | Native AOT |
| --- | --- |
| `Light.PortableResults` | Compatible (`net10.0` asset) |
| `Light.PortableResults.Validation` | Compatible (`net10.0` asset) |
| `Light.PortableResults.AspNetCore.MinimalApis` | Compatible |
| `Light.PortableResults.AspNetCore.OpenApi` | Compatible |
| `Light.PortableResults.Validation.OpenApi` | Compatible; the generated OpenAPI contracts use no reflection |
| `Light.PortableResults.AspNetCore.Mvc` | Not compatible, because ASP.NET Core MVC itself is not |

These assets are built with `IsAotCompatible`, which enables the trimming and AOT analyzers. The `netstandard2.0` assets of `Light.PortableResults` and `Light.PortableResults.Validation` are not annotated. CI publishes the [Native AOT sample](../samples/NativeAotMovieRating) for `linux-x64` so that trim and AOT regressions fail the build.

## What you have to register

You only register **your own result value types** with a `JsonSerializerContext`. Every envelope and payload type that Light.PortableResults owns is resolved by the library itself, so you never declare closed library wrappers such as `CloudEventsEnvelopeForWriting<MovieRating>` or `HttpReadAutoSuccessResultPayload<MovieRating>`. Combining source-generated resolvers cannot synthesize those closed generics from separate contracts anyway.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieRating))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
```

Compose the options by setting the resolver and then adding the Light.PortableResults converters:

```csharp
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Http.Reading;

var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    TypeInfoResolver = MovieRatingJsonContext.Default
};
serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();

var writeOptions = new PortableResultsCloudEventsWriteOptions
{
    SerializerOptions = serializerOptions,
    Source = "urn:movies:rating-service"
};

byte[] cloudEvent = result.ToCloudEvent(successType: "movie.rated", options: writeOptions);
```

The same composition applies to `AddDefaultPortableResultsCloudEventsReadJsonConverters` and `AddDefaultPortableResultsHttpReadJsonConverters`. Non-generic `Result` values require **no** consumer registration at all: every type involved in writing and reading them belongs to Light.PortableResults.

If a value type is missing from your context, the affected entry point throws an `InvalidOperationException` that names the unresolved type and tells you to register it, instead of failing deep inside `System.Text.Json`.

## HTTP writing with ASP.NET Core

The HTTP write path is the exception: `LightResult` and `LightResult<T>` serialize `HttpResultForWriting` and `HttpResultForWriting<T>` through the configured resolver, so those wrappers have to be declared in a context. `PortableResultsMinimalApiJsonContext` (namespace `Light.PortableResults.AspNetCore.MinimalApis.Serialization`) declares the non-generic `HttpResultForWriting`, `MetadataObject`, and `MetadataValue`; declare `HttpResultForWriting<T>` for each of your value types in your own context:

```csharp
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(NewMovieRatingDto))]
[JsonSerializable(typeof(MovieRating))]
[JsonSerializable(typeof(HttpResultForWriting<MovieRating>))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, MovieRatingJsonContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, PortableResultsMinimalApiJsonContext.Default);
});
```

Use `WebApplication.CreateSlimBuilder` for the smallest footprint; the [sample](../samples/NativeAotMovieRating) shows a complete application with validation and OpenAPI. Native AOT publishing requires a platform linker such as clang or gcc.

## How a contract is resolved

For each library-owned type, `PortableResultsJsonContracts` resolves in this order:

1. When the options carry no resolver and reflection-based serialization is enabled, the reflection resolver is materialized so that the shared default options keep working exactly as before.
2. A contract supplied by the configured `TypeInfoResolver` wins. Declaring a library type in your own context therefore still overrides the library.
3. Otherwise the converter that the options select for the type is used, following the normal `JsonSerializerOptions` precedence — the first entry in `Converters` that can convert the type. A converter you insert **before** the Light.PortableResults defaults keeps its precedence; the library converter is only reached when nothing else matches. Contracts created this way are cached per `JsonSerializerOptions` instance and can be created after the options became read-only. Creating one also makes the options read-only, so register every converter before the first read or write.
