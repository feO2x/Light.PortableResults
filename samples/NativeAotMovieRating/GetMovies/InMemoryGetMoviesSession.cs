using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NativeAotMovieRating.InMemoryDatabaseAccess;

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
        return lastKnownMovieId.HasValue ?
            Task.FromResult(GetRangeAfterMovieId(lastKnownMovieId.Value, take)) :
            Task.FromResult<List<Movie>?>(_database.Movies.Take(take).ToList());
    }

    private List<Movie>? GetRangeAfterMovieId(Guid lastKnownMovieId, int take)
    {
        var index = _database.Movies.FindIndex(x => x.Id == lastKnownMovieId);
        if (index == -1)
            return null;

        var remaining = _database.Movies.Count - (index + 1);
        var count = Math.Min(take, remaining);
        return _database.Movies.GetRange(index + 1, count);
    }
}
