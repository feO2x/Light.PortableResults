using System;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.AddMovieRating;

public interface IAddMovieRatingSession : ISession
{
    Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
}
