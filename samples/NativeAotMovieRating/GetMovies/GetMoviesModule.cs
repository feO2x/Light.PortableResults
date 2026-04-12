using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.GetMovies;

public static class GetMoviesModule
{
    public static IServiceCollection AddGetMoviesModule(this IServiceCollection services) =>
        services.AddScoped<IGetMoviesSession, InMemoryGetMoviesSession>();
}
