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
    InMemory
}

public static class DatabaseConfiguration
{
    public const string ProviderKey = "DatabaseProvider";
    public const string ConnectionStringName = "MovieRatingDatabase";

    public static DatabaseProvider GetDatabaseProvider(this IConfiguration configuration) =>
        Enum.TryParse<DatabaseProvider>(configuration[ProviderKey], ignoreCase: true, out var provider) ?
            provider :
            DatabaseProvider.Postgres;

    public static string GetMovieRatingConnectionString(this IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName) ??
        throw new InvalidOperationException(
            $"The connection string \"{ConnectionStringName}\" is not configured. Set it in appsettings.json or via " +
            $"the ConnectionStrings__{ConnectionStringName} environment variable."
        );
}
