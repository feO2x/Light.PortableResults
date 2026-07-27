using System;
using System.Collections.Generic;
using Light.SharedCore.Entities;

namespace NativeAotMovieRating.DatabaseAccess;

public sealed class Movie : GuidEntity
{
    public required string Title { get; set; } = string.Empty;

    // The setter exists for the MongoDB driver: it has no change tracker and needs a way to
    // populate the embedded ratings when it materializes a movie document.
    public List<MovieRating> Ratings { get; init; } = [];
}

public sealed class MovieRating : GuidEntity
{
    public required string Comment { get; set; } = string.Empty;
    public required Guid MovieId { get; set; }
    public required string UserName { get; set; } = string.Empty;
    public required int Rating { get; set; }
}
