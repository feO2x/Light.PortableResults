using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Creates the index the keyset pagination relies on and inserts the seed movies when the
/// collection is still empty.
/// </summary>
public sealed class MongoDatabaseInitializer : IHostedService
{
    private readonly ILogger _logger;
    private readonly IMongoCollection<Movie> _movies;

    public MongoDatabaseInitializer(IMongoCollection<Movie> movies, ILogger logger)
    {
        _movies = movies;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Movies are paginated with a keyset over (Title, _id), so the index must cover both fields
        // in exactly that order.
        var keysetIndex = new CreateIndexModel<Movie>(
            Builders<Movie>.IndexKeys.Ascending(movie => movie.Title).Ascending(movie => movie.Id)
        );
        await _movies.Indexes.CreateOneAsync(keysetIndex, cancellationToken: cancellationToken);

        var existingMovieCount = await _movies.CountDocumentsAsync(
            FilterDefinition<Movie>.Empty,
            cancellationToken: cancellationToken
        );
        if (existingMovieCount > 0L)
        {
            _logger.Information("The database already contains movies, skipping seeding");
            return;
        }

        var movies = MovieSeedData.CreateMovies();
        await _movies.InsertManyAsync(movies, cancellationToken: cancellationToken);
        _logger.Information("Seeded the database with {MovieCount} movies", movies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
