using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.NewMovie;

public static class NewMovieModule
{
    public static IServiceCollection AddNewMovieModule(this IServiceCollection services) =>
        services
           .AddScoped<IAddNewMovieSession, InMemoryAddNewMovieSession>()
           .AddScoped<NewMovieService>()
           .AddSingleton<NewMovieValidator>();
}
