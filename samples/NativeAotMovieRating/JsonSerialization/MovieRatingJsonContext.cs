using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
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
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
