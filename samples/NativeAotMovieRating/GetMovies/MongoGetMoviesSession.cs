using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public sealed class MongoGetMoviesSession : IGetMoviesSession
{
    private static readonly SortDefinition<Movie> SortByKeyset =
        Builders<Movie>.Sort.Ascending(movie => movie.Title).Ascending(movie => movie.Id);

    private readonly IMongoCollection<Movie> _movies;

    public MongoGetMoviesSession(IMongoCollection<Movie> movies) => _movies = movies;

    // This use case only reads a single document at a time, so there is nothing to commit and no
    // transaction to start - which is why there is nothing to dispose either.
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async Task<List<Movie>?> GetMoviesAsync(
        Guid? lastKnownMovieId,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        if (!lastKnownMovieId.HasValue)
        {
            return await _movies
                        .Find(FilterDefinition<Movie>.Empty)
                        .Sort(SortByKeyset)
                        .Limit(take)
                        .ToListAsync(cancellationToken);
        }

        // Keyset pagination needs the sort key of the last known movie, not just its ID. Looking it
        // up separately doubles as the existence check the endpoint turns into a MovieNotFound error.
        var lastKnownMovie = await _movies
                                  .Find(movie => movie.Id == lastKnownMovieId.Value)
                                  .Project(movie => new { movie.Title, movie.Id })
                                  .FirstOrDefaultAsync(cancellationToken);
        if (lastKnownMovie is null)
        {
            return null;
        }

        var filter = Builders<Movie>.Filter.Or(
            Builders<Movie>.Filter.Gt(movie => movie.Title, lastKnownMovie.Title),
            Builders<Movie>.Filter.And(
                Builders<Movie>.Filter.Eq(movie => movie.Title, lastKnownMovie.Title),
                Builders<Movie>.Filter.Gt(movie => movie.Id, lastKnownMovie.Id)
            )
        );
        return await _movies.Find(filter).Sort(SortByKeyset).Limit(take).ToListAsync(cancellationToken);
    }
}
