using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using NativeAotMovieRating.JsonSerialization;
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
   .AddValidationForLightPortableResults()
   .ConfigureJsonSerialization()
   .AddInMemoryDatabase()
   .AddGetMoviesModule()
   .AddAddMovieRatingModule()
   .AddHealthChecks();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseHealthChecks("/");
app.MapGetMoviesEndpoint();
app.MapAddMovieRatingEndpoint();

try
{
    await app.RunAsync();
}
finally
{
    await app.DisposeAsync();
    await Log.CloseAndFlushAsync();
}
