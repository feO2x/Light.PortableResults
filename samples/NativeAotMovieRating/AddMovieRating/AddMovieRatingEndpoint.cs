using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NativeAotMovieRating.AddMovieRating;

public static class AddMovieRatingEndpoint
{
    public static void MapAddMovieRatingEndpoint(this WebApplication app) =>
        app.MapPut("/api/moviesRatings", AddMovieRating);

    private static async Task<IResult> AddMovieRating(
        MovieRatingDto dto,
        AddMovieRatingService service,
        CancellationToken cancellationToken = default
    )
    {
        var result = await service.AddMovieRatingAsync(dto, cancellationToken);
        return result.ToMinimalApiResult();
    }
}
