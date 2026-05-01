using System;

namespace NativeAotMovieRating.NewMovie;

public sealed record NewMovieDto
{
    public required Guid MovieId { get; init; }
    public required string MovieName { get; init; }
}
