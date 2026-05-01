using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public sealed class InMemoryNewMovieRatingSession : INewMovieRatingSession
{
    private readonly InMemoryMovieDatabase _database;

    public InMemoryNewMovieRatingSession(InMemoryMovieDatabase database)
    {
        _database = database;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var movie = _database.Movies.FirstOrDefault(x => x.Id == movieId);
        return Task.FromResult(movie);
    }
}
