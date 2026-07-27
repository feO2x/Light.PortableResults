using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovie;

public sealed class InMemoryAddNewMovieSession : IAddNewMovieSession
{
    private readonly InMemoryMovieDatabase _database;

    public InMemoryAddNewMovieSession(InMemoryMovieDatabase database)
    {
        _database = database;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_database.Movies.FirstOrDefault(x => x.Id == movieId));
    }

    public void AddMovie(Movie movie) => _database.Movies.Add(movie);
}
