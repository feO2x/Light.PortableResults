using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.NewMovieRating;

public static class NewMovieRatingModule
{
    public static IServiceCollection AddNewMovieRatingModule(this IServiceCollection services) =>
        services
           .AddScoped<INewMovieRatingSession, InMemoryNewMovieRatingSession>()
           .AddScoped<NewMovieRatingService>()
           .AddSingleton<NewMovieRatingValidator>();
}
