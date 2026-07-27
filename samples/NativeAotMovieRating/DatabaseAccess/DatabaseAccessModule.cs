using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace NativeAotMovieRating.DatabaseAccess;

public static class DatabaseAccessModule
{
    public static IServiceCollection AddDatabaseAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseProvider provider
    ) =>
        provider switch
        {
            DatabaseProvider.Postgres => services.AddPostgres(configuration),
            DatabaseProvider.MongoDb => services.AddMongoDb(configuration),
            DatabaseProvider.InMemory => services.AddSingleton<InMemoryMovieDatabase>(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider")
        };

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        // The class maps are global state inside the driver, so they are registered once here
        // rather than from within a session.
        MongoDbClassMaps.Register();

        var mongoUrl = MongoUrl.Create(configuration.GetMongoDbConnectionString());
        return services
              // The client is thread-safe and owns the connection pool, so a single instance is
              // shared. The per-request unit of work is the IClientSessionHandle a session starts.
           .AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl))
           .AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName))
           .AddSingleton(
                sp => sp.GetRequiredService<IMongoDatabase>()
                        .GetCollection<Movie>(MongoDbClassMaps.MoviesCollectionName)
            )
           .AddHostedService<MongoDatabaseInitializer>();
    }

    private static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration) =>
        services
            // A factory instead of a scoped DbContext: every session creates and disposes the DbContext
            // it owns, which is what makes the session a true humble object around the database.
           .AddDbContextFactory<MovieRatingDbContext>(
                options => options.UseNpgsql(
                    configuration.GetPostgresConnectionString(),
                    // The database usually starts alongside the service, so the first connection
                    // attempts may well hit a container that is not accepting connections yet.
                    npgsql => npgsql.EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null
                    )
                )
            )
           .AddHostedService<DatabaseInitializer>();
}
