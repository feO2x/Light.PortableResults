using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public sealed class EfGetMoviesSession : IGetMoviesSession
{
    private readonly MovieRatingDbContext _dbContext;

    public EfGetMoviesSession(IDbContextFactory<MovieRatingDbContext> dbContextFactory) =>
        _dbContext = dbContextFactory.CreateDbContext();

    public ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    public async Task<List<Movie>?> GetMoviesAsync(
        Guid? lastKnownMovieId,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        if (!lastKnownMovieId.HasValue)
        {
            return await OrderedMovies().Take(take).ToListAsync(cancellationToken);
        }

        // Keyset pagination needs the sort key of the last known movie, not just its ID. Looking it
        // up separately doubles as the existence check the endpoint turns into a MovieNotFound error.
        var lastKnownMovie = await _dbContext
           .Movies
           .AsNoTracking()
           .Where(movie => movie.Id == lastKnownMovieId.Value)
           .Select(movie => new { movie.Title, movie.Id })
           .FirstOrDefaultAsync(cancellationToken);
        if (lastKnownMovie is null)
        {
            return null;
        }

        return await OrderedMovies()
           .Where(
                movie => string.Compare(movie.Title, lastKnownMovie.Title) > 0 ||
                    (movie.Title == lastKnownMovie.Title && movie.Id.CompareTo(lastKnownMovie.Id) > 0)
            )
           .Take(take)
           .ToListAsync(cancellationToken);
    }

    // This session only reads, so change tracking would be pure overhead.
    private IQueryable<Movie> OrderedMovies() =>
        _dbContext
           .Movies
           .AsNoTracking()
           .Include(movie => movie.Ratings)
           .OrderBy(movie => movie.Title)
           .ThenBy(movie => movie.Id);
}
