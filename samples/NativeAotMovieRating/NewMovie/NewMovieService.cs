using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using Serilog;

namespace NativeAotMovieRating.NewMovie;

public sealed class NewMovieService
{
    private readonly ILogger _logger;
    private readonly IAddNewMovieSession _session;
    private readonly NewMovieValidator _validator;

    public NewMovieService(IAddNewMovieSession session, NewMovieValidator validator, ILogger logger)
    {
        _session = session;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Movie>> AddNewMovieAsync(NewMovieDto dto, CancellationToken cancellationToken = default)
    {
        var validationContext = _validator.ValidationContextFactory.CreateValidationContext();
        if (_validator.CheckForErrors(dto, validationContext, out var errorResult))
        {
            return Result<Movie>.Fail(errorResult.Errors);
        }

        var movie = await _session.GetMovieAsync(dto.MovieId, cancellationToken);
        if (movie is null)
        {
            var newMovie = new Movie
            {
                Id = dto.MovieId,
                Title = dto.MovieName
            };

            _session.AddMovie(newMovie);
            await _session.SaveChangesAsync(cancellationToken);

            _logger.Information(
                "Successfully added new movie {MovieTitle}",
                dto.MovieName
            );
            return Result<Movie>.Ok(newMovie);
        }

        return Result<Movie>.Ok(movie);
    }
}
