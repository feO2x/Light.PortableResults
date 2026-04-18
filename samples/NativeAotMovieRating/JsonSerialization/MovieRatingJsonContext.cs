using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.Http.Writing;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.JsonSerialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieRatingDto))]
[JsonSerializable(typeof(MovieRating))]
[JsonSerializable(typeof(HttpResultForWriting<MovieRating>))]
[JsonSerializable(typeof(List<Movie>))]
// Primitive/parameter types that appear in endpoint signatures. Microsoft.AspNetCore.OpenApi
// requests JsonTypeInfo for each of them when building the OpenAPI document, and source-gen-only
// JSON (AOT mode) will not resolve them implicitly.
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(int))]
// Schema-only types registered so Microsoft.AspNetCore.OpenApi can emit JSON schemas under
// the AOT-friendly source-gen serializer options configured for this app.
[JsonSerializable(typeof(PortableRichValidationProblemDetails))]
[JsonSerializable(typeof(PortableProblemDetails))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
