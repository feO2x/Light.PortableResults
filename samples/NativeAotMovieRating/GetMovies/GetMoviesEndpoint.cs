using System;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace NativeAotMovieRating.GetMovies;

public static class GetMoviesEndpoint
{
    public static void MapGetMoviesEndpoint(this WebApplication app) =>
        app.MapGet("/api/movies", GetMovies);

    private static async Task<IResult> GetMovies(
        IGetMoviesSession session,
        IValidationContextFactory validationContextFactory,
        Guid? lastKnownMovieId = null,
        int take = 20,
        CancellationToken cancellationToken = default
    )
    {
        // This endpoint illustrates that you don't need to use validators. You can simply instantiate a
        // ValidationContext and use the Check extension methods to validate any value, for example, route or query
        // parameters. This code might seem a little verbose, but you can easily put it in a static method and
        // call it from any endpoint implementing pagination.
        var validationContext = validationContextFactory.CreateValidationContext();
        validationContext.Check(take).IsInBetween(1, 40);
        if (lastKnownMovieId.HasValue)
        {
            validationContext.Check(lastKnownMovieId.Value, target: nameof(lastKnownMovieId)).IsNotEmpty(
                $"{nameof(lastKnownMovieId)} must not be an empty GUID when it is set"
            );
        }

        if (validationContext.HasErrors)
        {
            return validationContext.ToFailureResult().ToMinimalApiResult();
        }

        var movies = await session.GetMoviesAsync(lastKnownMovieId, take, cancellationToken);
        // You can even use the ValidationContext to add errors after you interacted with other layers and easily
        // create RFC-9457-compatible Problem Detail responses.
        if (movies == null)
        {
            validationContext.AddError(
                new Error
                {
                    Message = "There is no movie with the specified ID",
                    Target = nameof(lastKnownMovieId),
                    Category = ErrorCategory.Validation, // Results in HTTP 404 Not Found
                    Code = "MovieNotFound", // Should be custom to your app and identify the error uniquely
                    Metadata = MetadataObject.Create((nameof(lastKnownMovieId), lastKnownMovieId!.Value.ToString()))
                }
            );
            return validationContext.ToFailureResult().ToMinimalApiResult();
        }

        return TypedResults.Ok(movies);
    }
}
