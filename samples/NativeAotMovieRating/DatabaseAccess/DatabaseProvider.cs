using System;
using Microsoft.Extensions.Configuration;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Selects which third-party system the humble objects of this service talk to. Because every use
/// case only depends on its own session or client abstraction, swapping the provider is a
/// composition-root concern - no business logic changes.
/// </summary>
public enum DatabaseProvider
{
    Postgres,
    MongoDb,
    InMemory
}

public static class DatabaseConfiguration
{
    public const string ProviderKey = "DatabaseProvider";
    public const string PostgresConnectionStringName = "MovieRatingPostgres";
    public const string MongoDbConnectionStringName = "MovieRatingMongoDb";

    public static DatabaseProvider GetDatabaseProvider(this IConfiguration configuration) =>
        Enum.TryParse<DatabaseProvider>(configuration[ProviderKey], ignoreCase: true, out var provider) ?
            provider :
            DatabaseProvider.Postgres;

    public static string GetPostgresConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString(PostgresConnectionStringName);

    public static string GetMongoDbConnectionString(this IConfiguration configuration) =>
        configuration.GetRequiredConnectionString(MongoDbConnectionStringName);

    private static string GetRequiredConnectionString(this IConfiguration configuration, string name) =>
        configuration.GetConnectionString(name) ??
        throw new InvalidOperationException(
            $"The connection string \"{name}\" is not configured. Set it in appsettings.json or via the " +
            $"ConnectionStrings__{name} environment variable."
        );
}
