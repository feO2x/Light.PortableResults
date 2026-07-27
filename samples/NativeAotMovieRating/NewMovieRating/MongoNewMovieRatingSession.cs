using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public sealed class MongoNewMovieRatingSession : MongoSession, INewMovieRatingSession
{
    private readonly IMongoCollection<Movie> _movies;
    private Movie? _loadedMovie;

    public MongoNewMovieRatingSession(IMongoClient client, IMongoCollection<Movie> movies) : base(client) =>
        _movies = movies;

    public async Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        var clientSession = await GetTransactionAsync(cancellationToken);
        _loadedMovie = await _movies
                            .Find(clientSession, movie => movie.Id == movieId)
                            .FirstOrDefaultAsync(cancellationToken);
        return _loadedMovie;
    }

    // The MongoDB driver has no change tracker, so the session remembers the aggregate it handed
    // out and writes it back as a whole. Because the ratings are embedded in the movie document,
    // the caller appending to Movie.Ratings turns into a single document replacement here.
    protected override Task ApplyChangesAsync(
        IClientSessionHandle clientSession,
        CancellationToken cancellationToken
    )
    {
        var movie = _loadedMovie;
        return movie is null ?
            Task.CompletedTask :
            _movies.ReplaceOneAsync(
                clientSession,
                storedMovie => storedMovie.Id == movie.Id,
                movie,
                cancellationToken: cancellationToken
            );
    }
}
