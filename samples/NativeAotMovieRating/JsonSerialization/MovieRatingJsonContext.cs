using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Light.PortableResults.Http.Writing;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using NativeAotMovieRating.NewMovie;
using NativeAotMovieRating.NewMovieRating;

namespace NativeAotMovieRating.JsonSerialization;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(NewMovieRatingDto))]
[JsonSerializable(typeof(MovieRating))]
[JsonSerializable(typeof(HttpResultForWriting<MovieRating>))]
[JsonSerializable(typeof(HttpResultForWriting<Movie>))]
[JsonSerializable(typeof(List<Movie>))]
[JsonSerializable(typeof(NewMovieDto))]
[JsonSerializable(typeof(Guid?))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(MovieNotFoundMetadata))]
public sealed partial class MovieRatingJsonContext : JsonSerializerContext;
