using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public static class GetMoviesModule
{
    public static IServiceCollection AddGetMoviesModule(
        this IServiceCollection services,
        DatabaseProvider provider
    ) =>
        provider switch
        {
            DatabaseProvider.MongoDb => services.AddScoped<IGetMoviesSession, MongoGetMoviesSession>(),
            DatabaseProvider.InMemory => services.AddScoped<IGetMoviesSession, InMemoryGetMoviesSession>(),
            _ => services.AddScoped<IGetMoviesSession, EfGetMoviesSession>()
        };
}
