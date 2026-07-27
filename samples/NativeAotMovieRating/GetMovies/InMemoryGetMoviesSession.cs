using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public sealed class InMemoryGetMoviesSession : IGetMoviesSession
{
    private readonly InMemoryMovieDatabase _database;

    public InMemoryGetMoviesSession(InMemoryMovieDatabase database) => _database = database;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task<List<Movie>?> GetMoviesAsync(
        Guid? lastKnownMovieId,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // The same sort order as the PostgreSQL session so that both implementations honor the
        // same contract - callers must be able to swap them without noticing.
        var orderedMovies = _database
           .Movies
           .OrderBy(movie => movie.Title, StringComparer.Ordinal)
           .ThenBy(movie => movie.Id)
           .ToList();

        if (!lastKnownMovieId.HasValue)
        {
            return Task.FromResult<List<Movie>?>(orderedMovies.Take(take).ToList());
        }

        var index = orderedMovies.FindIndex(movie => movie.Id == lastKnownMovieId.Value);
        return Task.FromResult(
            index == -1 ? null : orderedMovies.Skip(index + 1).Take(take).ToList()
        );
    }
}
