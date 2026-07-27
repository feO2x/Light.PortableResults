using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public static class NewMovieRatingModule
{
    public static IServiceCollection AddNewMovieRatingModule(
        this IServiceCollection services,
        DatabaseProvider provider
    ) =>
        services
           .AddSession(provider)
           .AddScoped<NewMovieRatingService>()
           .AddSingleton<NewMovieRatingValidator>();

    private static IServiceCollection AddSession(this IServiceCollection services, DatabaseProvider provider) =>
        provider switch
        {
            DatabaseProvider.MongoDb => services.AddScoped<INewMovieRatingSession, MongoNewMovieRatingSession>(),
            DatabaseProvider.InMemory => services.AddScoped<INewMovieRatingSession, InMemoryNewMovieRatingSession>(),
            _ => services.AddScoped<INewMovieRatingSession, EfNewMovieRatingSession>()
        };
}
