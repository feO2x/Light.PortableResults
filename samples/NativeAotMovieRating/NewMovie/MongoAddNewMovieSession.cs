using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovie;

public sealed class MongoAddNewMovieSession : MongoSession, IAddNewMovieSession
{
    private readonly IMongoCollection<Movie> _movies;
    private Movie? _newMovie;

    public MongoAddNewMovieSession(IMongoClient client, IMongoCollection<Movie> movies) : base(client) =>
        _movies = movies;

    public async Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        var clientSession = await GetTransactionAsync(cancellationToken);
        return await _movies
                    .Find(clientSession, movie => movie.Id == movieId)
                    .FirstOrDefaultAsync(cancellationToken);
    }

    public void AddMovie(Movie movie) => _newMovie = movie;

    protected override Task ApplyChangesAsync(
        IClientSessionHandle clientSession,
        CancellationToken cancellationToken
    ) =>
        _newMovie is null ?
            Task.CompletedTask :
            _movies.InsertOneAsync(clientSession, _newMovie, cancellationToken: cancellationToken);
}
