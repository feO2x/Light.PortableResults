using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovie;

public sealed class EfAddNewMovieSession : IAddNewMovieSession
{
    private readonly MovieRatingDbContext _dbContext;

    public EfAddNewMovieSession(IDbContextFactory<MovieRatingDbContext> dbContextFactory) =>
        _dbContext = dbContextFactory.CreateDbContext();

    public ValueTask DisposeAsync() => _dbContext.DisposeAsync();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default) =>
        _dbContext.Movies.FirstOrDefaultAsync(movie => movie.Id == movieId, cancellationToken);

    public void AddMovie(Movie movie) => _dbContext.Movies.Add(movie);
}
