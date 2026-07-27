using System.Threading;
using System.Threading.Tasks;
using Light.SharedCore.DatabaseAccessAbstractions;
using MongoDB.Driver;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Base class for the MongoDB sessions of use cases that write. It maps
/// <see cref="ISession.SaveChangesAsync" /> onto a real MongoDB transaction, which is exactly the
/// same promise the Entity Framework sessions make - the calling business logic cannot tell the
/// difference.
/// </summary>
public abstract class MongoSession : ISession
{
    private readonly IMongoClient _client;
    private IClientSessionHandle? _clientSession;

    protected MongoSession(IMongoClient client) => _client = client;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var clientSession = await GetTransactionAsync(cancellationToken);
        await ApplyChangesAsync(clientSession, cancellationToken);
        await clientSession.CommitTransactionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_clientSession is null)
        {
            return;
        }

        // Anything that was not committed explicitly is rolled back - a caller that returns an
        // error result halfway through must not leave a partial write behind.
        if (_clientSession.IsInTransaction)
        {
            await _clientSession.AbortTransactionAsync();
        }

        _clientSession.Dispose();
    }

    /// <summary>
    /// Writes everything this session has accumulated. It runs inside the transaction that
    /// <see cref="SaveChangesAsync" /> commits afterwards.
    /// </summary>
    protected abstract Task ApplyChangesAsync(IClientSessionHandle clientSession, CancellationToken cancellationToken);

    /// <summary>
    /// Starts the transaction on first use so that a session which is resolved but never touched
    /// does not pay for a round trip.
    /// </summary>
    protected async ValueTask<IClientSessionHandle> GetTransactionAsync(CancellationToken cancellationToken)
    {
        if (_clientSession is null)
        {
            _clientSession = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            _clientSession.StartTransaction();
        }

        return _clientSession;
    }
}
