using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            DatabaseProvider.InMemory => services.AddSingleton<InMemoryMovieDatabase>(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider")
        };

    private static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration) =>
        services
            // A factory instead of a scoped DbContext: every session creates and disposes the DbContext
            // it owns, which is what makes the session a true humble object around the database.
           .AddDbContextFactory<MovieRatingDbContext>(
                options => options.UseNpgsql(
                    configuration.GetMovieRatingConnectionString(),
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
