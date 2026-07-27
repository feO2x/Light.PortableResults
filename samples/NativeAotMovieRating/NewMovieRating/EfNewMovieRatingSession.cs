using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public sealed class EfNewMovieRatingSession : INewMovieRatingSession
{
    private readonly MovieRatingDbContext _dbContext;

    public EfNewMovieRatingSession(IDbContextFactory<MovieRatingDbContext> dbContextFactory) =>
        _dbContext = dbContextFactory.CreateDbContext();

    public ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    // The existing ratings must be loaded because the caller checks whether the new rating is a
    // duplicate and then appends to the collection - change tracking turns that into an INSERT.
    public Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default) =>
        _dbContext
           .Movies
           .Include(movie => movie.Ratings)
           .FirstOrDefaultAsync(movie => movie.Id == movieId, cancellationToken);
}
