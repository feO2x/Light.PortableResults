using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public static class GetMoviesEndpoint
{
    public static void MapGetMoviesEndpoint(this WebApplication app) =>
        app.MapGet("/api/movies", GetMovies)
           .WithName("GetMovies")
           .WithTags("Movies")
           .WithSummary("Returns a paginated list of movies.")
           .WithDescription(
                "Supports keyset pagination via the optional lastKnownMovieId parameter and a configurable " +
                "page size via the take parameter (1-40). Returns a rich Light.PortableResults problem " +
                "details response when input validation fails or when the lastKnownMovieId does not exist."
           )
           .Produces<List<Movie>>()
           .ProducesPortableValidationProblem(
                configure: x => x
                   .UseFormat(ValidationProblemSerializationFormat.Rich)
                   .WithErrorCodes(ValidationErrorCodes.NotEmpty)
                   .WithInRangeError<int>()
            );

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
