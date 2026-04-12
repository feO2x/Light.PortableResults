using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.PortableResults.Http.Writing;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.JsonSerialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieRatingDto))]
[JsonSerializable(typeof(HttpResultForWriting<MovieRating>))]
[JsonSerializable(typeof(List<Movie>))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
