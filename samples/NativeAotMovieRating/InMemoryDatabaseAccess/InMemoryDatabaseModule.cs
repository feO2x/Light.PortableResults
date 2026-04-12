using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.InMemoryDatabaseAccess;

public static class InMemoryDatabaseModule
{
    public static IServiceCollection AddInMemoryDatabase(this IServiceCollection services) =>
        services.AddSingleton<InMemoryMovieDatabase>();
}
