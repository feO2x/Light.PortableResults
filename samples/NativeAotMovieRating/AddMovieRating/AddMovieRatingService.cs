using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults;
using Light.PortableResults.Metadata;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using Serilog;

namespace NativeAotMovieRating.AddMovieRating;

public sealed class AddMovieRatingService
{
    private readonly ILogger _logger;
    private readonly IAddMovieRatingSession _session;
    private readonly MovieRatingValidator _validator;

    public AddMovieRatingService(MovieRatingValidator validator, IAddMovieRatingSession session, ILogger logger)
    {
        _validator = validator;
        _session = session;
        _logger = logger;
    }

    public async Task<Result<MovieRating>> AddMovieRatingAsync(
        MovieRatingDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var validationContext = _validator.ValidationContextFactory.CreateValidationContext();
        if (_validator.CheckForErrors(dto, validationContext, out var errorResult))
        {
            return Result<MovieRating>.Fail(errorResult.Errors);
        }

        var movie = await _session.GetMovieAsync(dto.MovieId, cancellationToken);
        if (movie is null)
        {
            validationContext
               .Check(dto.MovieId)
               .AddError(
                    "There is no movie with the specified ID",
                    code: "MovieNotFound",
                    MetadataObject.Create(("movieId", dto.MovieId.ToString()))
                );
            return Result<MovieRating>.Fail(validationContext.Errors);
        }

        var existingRating = movie.Ratings.Find(x => x.Id == dto.Id);
        if (existingRating is not null)
        {
            _logger.Information("Movie Rating {MovieRatingId} already exists", dto.Id);
            return Result<MovieRating>.Ok(existingRating);
        }

        var newRating = new MovieRating
        {
            Id = dto.Id,
            Rating = dto.Rating,
            MovieId = dto.MovieId,
            UserName = dto.UserName,
            Comment = dto.Comment
        };
        movie.Ratings.Add(newRating);
        await _session.SaveChangesAsync(cancellationToken);
        _logger.Information(
            "Added movie rating {MovieRatingId} for movie {MovieTitle} by user {UserName}",
            dto.Id,
            movie.Title,
            newRating.UserName
        );
        return Result<MovieRating>.Ok(newRating);
    }
}
