# Movie Rating sample

A Minimal API service that demonstrates Light.PortableResults together with the Humble Object
pattern for data access. The same three use cases run unchanged against PostgreSQL, MongoDB or an
in-memory store.

## Running it

Both databases come up together; the service talks to PostgreSQL by default:

```bash
docker compose up -d
dotnet run
```

The first start creates the schema and seeds the movies. Then use `requests.http` to exercise the
endpoints, or open <http://localhost:5000/docs> for the Scalar API reference.

To reset the databases completely:

```bash
docker compose down -v && docker compose up -d
```

## Switching the database provider

`DatabaseProvider` in `appsettings.json` selects which third-party system the humble objects talk
to. All three implement exactly the same session contracts - including the sort order of the keyset
pagination - so no business logic changes when you flip the switch:

```bash
DatabaseProvider=MongoDb dotnet run
DatabaseProvider=InMemory dotnet run   # no Docker needed
```

## How the data access is structured

Each vertical slice owns the abstraction it needs, and nothing more:

| Slice            | Abstraction              | PostgreSQL                | MongoDB                      | In-memory                       |
|------------------|--------------------------|---------------------------|------------------------------|---------------------------------|
| `GetMovies`      | `IGetMoviesSession`      | `EfGetMoviesSession`      | `MongoGetMoviesSession`      | `InMemoryGetMoviesSession`      |
| `NewMovie`       | `IAddNewMovieSession`    | `EfAddNewMovieSession`    | `MongoAddNewMovieSession`    | `InMemoryAddNewMovieSession`    |
| `NewMovieRating` | `INewMovieRatingSession` | `EfNewMovieRatingSession` | `MongoNewMovieRatingSession` | `InMemoryNewMovieRatingSession` |

`DatabaseAccess/` holds everything that is not slice-specific - the entities, the provider-specific
infrastructure, the seed data and the composition-root switch.

### PostgreSQL

The EF Core sessions are registered against `IDbContextFactory<MovieRatingDbContext>` rather than a
scoped `DbContext`: every session creates the `DbContext` it owns and disposes it again, so the
session really is the boundary around the database. `SaveChangesAsync` is EF Core's own.

Ratings live in their own table with a foreign key back to the movie.

### MongoDB

The driver is used directly, without the EF Core provider. Two things differ from EF Core, and both
are visible in the code:

- **Ratings are embedded in the movie document** rather than stored in a collection of their own.
  They are only ever read and written through the movie they belong to, which makes the movie the
  natural aggregate.
- **There is no change tracker.** `MongoNewMovieRatingSession` therefore remembers the aggregate it
  handed out and writes it back as a whole in `SaveChangesAsync`. Because the ratings are embedded,
  that is a single atomic document replacement.

`MongoSession` maps `ISession.SaveChangesAsync` onto a real MongoDB transaction: it starts an
`IClientSessionHandle` on first use, commits it in `SaveChangesAsync`, and aborts it on dispose if
the caller never committed. That is why `docker-compose.yml` runs mongod as a single-node replica
set - MongoDB only offers transactions on a replica set. The `mongo-init` service performs the
`rs.initiate`, and the member advertises itself as `localhost:27017` so that the service running on
the host machine can resolve whatever host the primary reports during replica set discovery.

You can watch the transactions being used:

```bash
docker exec movie-rating-mongo mongosh movierating --quiet \
  --eval 'printjson(db.serverStatus().transactions)'
```

Requests that end in an error result - an unknown movie, a duplicate rating - show up as aborted
transactions, because those paths return before calling `SaveChangesAsync`.

## Working with migrations

Only PostgreSQL has a schema to migrate. `dotnet-ef` is pinned in this folder's `dotnet-tools.json`:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --output-dir DatabaseAccess/Migrations --namespace NativeAotMovieRating.DatabaseAccess.Migrations
```

Migrations are applied automatically at startup by `DatabaseInitializer`. `MongoDatabaseInitializer`
does the equivalent for MongoDB: it creates the keyset index and seeds the collection.
