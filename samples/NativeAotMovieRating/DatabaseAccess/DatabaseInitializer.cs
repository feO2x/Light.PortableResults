using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Brings the PostgreSQL database up to date before the first request is served: applies all
/// pending migrations and inserts the seed movies when the database is still empty.
/// </summary>
public sealed class DatabaseInitializer : IHostedService
{
    private readonly IDbContextFactory<MovieRatingDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public DatabaseInitializer(IDbContextFactory<MovieRatingDbContext> dbContextFactory, ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.Information("Applying pending database migrations");
        // Migrating does not go through the configured retry strategy on its own, so the very first
        // connection attempt would fail outright when the database container is still booting.
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(dbContext.Database.MigrateAsync, cancellationToken);

        if (await dbContext.Movies.AnyAsync(cancellationToken))
        {
            _logger.Information("The database already contains movies, skipping seeding");
            return;
        }

        var movies = MovieSeedData.CreateMovies();
        dbContext.Movies.AddRange(movies);
        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Seeded the database with {MovieCount} movies", movies.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
