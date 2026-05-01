using System;

namespace NativeAotMovieRating.NewMovieRating;

public sealed record NewMovieRatingDto
{
    public required Guid Id { get; init; }
    public required Guid MovieId { get; init; }
    public required string UserName { get; set; } = string.Empty;
    public required string Comment { get; set; } = string.Empty;
    public required int Rating { get; init; }
}
