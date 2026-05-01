using System;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.NewMovie;

public interface IAddNewMovieSession : ISession
{
    Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default);

    void AddMovie(Movie movie);
}
