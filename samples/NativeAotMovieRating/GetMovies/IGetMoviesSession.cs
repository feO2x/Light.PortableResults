using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NativeAotMovieRating.InMemoryDatabaseAccess;

namespace NativeAotMovieRating.GetMovies;

public interface IGetMoviesSession : IAsyncDisposable
{
    Task<List<Movie>?> GetMoviesAsync(Guid? lastKnownMovieId, int take, CancellationToken cancellationToken = default);
}
