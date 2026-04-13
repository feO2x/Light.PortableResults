using Microsoft.Extensions.DependencyInjection;

namespace NativeAotMovieRating.AddMovieRating;

public static class AddMovieRatingModule
{
    public static IServiceCollection AddAddMovieRatingModule(this IServiceCollection services) =>
        services
           .AddScoped<IAddMovieRatingSession, InMemoryAddMovieRatingSession>()
           .AddScoped<AddMovieRatingService>()
           .AddSingleton<MovieRatingValidator>();
}
