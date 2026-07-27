# Movie Rating sample

A Minimal API service that demonstrates Light.PortableResults together with the Humble Object
pattern for data access.

## Running it

The service talks to a real PostgreSQL database by default:

```bash
docker compose up -d
dotnet run
```

The first start applies the EF Core migrations and seeds the movies. Then use `requests.http` to
exercise the endpoints, or open <http://localhost:5000/docs> for the Scalar API reference.

To reset the database completely:

```bash
docker compose down -v && docker compose up -d
```

## Switching the database provider

`DatabaseProvider` in `appsettings.json` selects which third-party system the humble objects talk
to. Set it to `InMemory` to run without Docker:

```bash
DatabaseProvider=InMemory dotnet run
```

Both providers implement exactly the same session contracts, including the sort order of the keyset
pagination, so no business logic changes when you flip the switch.

## How the data access is structured

Each vertical slice owns the abstraction it needs, and nothing more:

| Slice             | Abstraction              | PostgreSQL             | In-memory                     |
|-------------------|--------------------------|------------------------|-------------------------------|
| `GetMovies`       | `IGetMoviesSession`      | `EfGetMoviesSession`   | `InMemoryGetMoviesSession`    |
| `NewMovie`        | `IAddNewMovieSession`    | `EfAddNewMovieSession` | `InMemoryAddNewMovieSession`  |
| `NewMovieRating`  | `INewMovieRatingSession` | `EfNewMovieRatingSession` | `InMemoryNewMovieRatingSession` |

The EF Core sessions are registered against `IDbContextFactory<MovieRatingDbContext>` rather than a
scoped `DbContext`: every session creates the `DbContext` it owns and disposes it again, so the
session really is the boundary around the database.

`DatabaseAccess/` holds everything that is not slice-specific - the entities, the `DbContext` and
its configuration, the migrations, the seed data and the composition-root switch.

## Working with migrations

`dotnet-ef` is pinned in this folder's `dotnet-tools.json`:

```bash
dotnet tool restore
dotnet ef migrations add <Name> --output-dir DatabaseAccess/Migrations --namespace NativeAotMovieRating.DatabaseAccess.Migrations
```

Migrations are applied automatically at startup by `DatabaseInitializer`.
