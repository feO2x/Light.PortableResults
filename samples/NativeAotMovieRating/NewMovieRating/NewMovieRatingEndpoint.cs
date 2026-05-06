using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public static class NewMovieRatingEndpoint
{
    public static void MapAddMovieRatingEndpoint(this WebApplication app) =>
        app.MapPut("/api/moviesRatings", AddMovieRating)
           .WithName("AddMovieRating")
           .WithTags("Movie Ratings")
           .WithSummary("Adds or updates a movie rating.")
           .WithDescription(
                "Validates the request and stores the movie rating. Returns the stored rating on success, or a rich Light.PortableResults problem details response on validation or lookup failures."
           )
           .Produces<MovieRating>()
           .ProducesPortableValidationProblemFor<NewMovieRatingValidator>(
                configure: x => x
                   .UseFormat(ValidationProblemSerializationFormat.Rich)
            )
           .ProducesPortableProblem();

    private static async Task<IResult> AddMovieRating(
        NewMovieRatingDto dto,
        NewMovieRatingService service,
        CancellationToken cancellationToken = default
    )
    {
        var result = await service.AddMovieRatingAsync(dto, cancellationToken);
        return result.ToMinimalApiResult();
    }
}
