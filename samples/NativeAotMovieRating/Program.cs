using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.InMemoryDatabaseAccess;
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
builder
   .Services
   .AddPortableResultsForMinimalApis()
   .AddPortableResultsOpenApi()
   .AddValidationForPortableResults()
   .ConfigureJsonSerialization()
   .AddInMemoryDatabase()
   .AddGetMoviesModule()
   .AddNewMovieRatingModule()
   .AddNewMovieModule()
   .AddHealthChecks()
   .Services
   .AddOpenApi();

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
