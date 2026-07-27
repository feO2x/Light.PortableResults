using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.DatabaseAccess;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.JsonSerialization;
using NativeAotMovieRating.NewMovie;
using NativeAotMovieRating.NewMovieRating;
using NativeAotMovieRating.OpenApi;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
   .WriteTo.Console()
   .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
   .CreateLogger();
var builder = WebApplication.CreateSlimBuilder(args);
builder.Host.UseSerilog(Log.Logger);

// The only place in the whole service that knows which third-party system the humble objects talk
// to. Every use case depends on its own session abstraction, so this switch is all it takes to run
// against PostgreSQL (docker compose up) or against the in-memory store.
var databaseProvider = builder.Configuration.GetDatabaseProvider();
Log.Information("Using the {DatabaseProvider} database provider", databaseProvider);

builder
   .Services
   .AddPortableResultsForMinimalApis()
   .AddValidationForPortableResults()
   .AddOpenApi()
   .AddPortableResultsOpenApi(contracts => contracts.RegisterBuiltInValidationErrors())
   .ConfigureJsonSerialization()
   .AddDatabaseAccess(builder.Configuration, databaseProvider)
   .AddGetMoviesModule(databaseProvider)
   .AddNewMovieRatingModule(databaseProvider)
   .AddNewMovieModule(databaseProvider)
   .AddHealthChecks();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseHealthChecks("/health");
app.MapOpenApiAndScalar();
app.MapGetMoviesEndpoint();
app.MapAddMovieRatingEndpoint();
app.MapNewMovieEndpoint();
app.RedirectHomeToDocs();

try
{
    await app.RunAsync();
}
finally
{
    await app.DisposeAsync();
    await Log.CloseAndFlushAsync();
}
