using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NativeAotMovieRating.NewMovie;

public static class AddNewMovieEndpoint
{
    public static void MapNewMovieEndpoint(this WebApplication app) =>
        app
           .MapPut("/api/movies", NewMovieRating)
           .WithName("AddNewMovie")
           .WithTags("Movies")
           .WithSummary("Adds a new movie.")
           .WithDescription(
                "Validates the request and stores a new movie. Returns the stored movie on success, or a rich Light.PortableResults problem details response on validation failures."
            )
           .Produces<NewMovieDto>()
           .ProducesPortableValidationProblem(configure: x => x.UseFormat(ValidationProblemSerializationFormat.Rich))
           .ProducesPortableProblem();

    private static async Task<IResult> NewMovieRating(
        NewMovieDto dto,
        NewMovieService service,
        CancellationToken cancellationToken = default
    )
    {
        var result = await service.AddNewMovieAsync(dto, cancellationToken);
        return result.ToMinimalApiResult();
    }
}
