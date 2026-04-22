using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.PortableResults.Http.Writing;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using NativeAotMovieRating.NewMovie;

namespace NativeAotMovieRating.JsonSerialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(MovieRatingDto))]
[JsonSerializable(typeof(MovieRating))]
[JsonSerializable(typeof(HttpResultForWriting<MovieRating>))]
[JsonSerializable(typeof(HttpResultForWriting<Movie>))]
[JsonSerializable(typeof(List<Movie>))]
[JsonSerializable(typeof(NewMovieDto))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(int))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
