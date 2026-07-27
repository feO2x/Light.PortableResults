using System;
using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using NativeAotMovieRating.DatabaseAccess;

namespace NativeAotMovieRating.NewMovieRating;

public interface INewMovieRatingSession : ISession
{
    Task<Movie?> GetMovieAsync(Guid movieId, CancellationToken cancellationToken = default);
}
