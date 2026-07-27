using System.Collections.Generic;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// The in-memory counterpart to PostgreSQL. It is registered as a singleton, which means all
/// in-memory sessions share this single list for the lifetime of the process.
/// </summary>
public sealed class InMemoryMovieDatabase
{
    public List<Movie> Movies { get; } = MovieSeedData.CreateMovies();
}
