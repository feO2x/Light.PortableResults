using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovie;

public static class NewMovieModule
{
    public static IServiceCollection AddNewMovieModule(this IServiceCollection services, DatabaseProvider provider) =>
        services
           .AddSession(provider)
           .AddScoped<NewMovieService>()
           .AddSingleton<NewMovieValidator>();

    private static IServiceCollection AddSession(this IServiceCollection services, DatabaseProvider provider) =>
        provider switch
        {
            DatabaseProvider.InMemory => services.AddScoped<IAddNewMovieSession, InMemoryAddNewMovieSession>(),
            _ => services.AddScoped<IAddNewMovieSession, EfAddNewMovieSession>()
        };
}
