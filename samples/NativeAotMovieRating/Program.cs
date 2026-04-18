using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using NativeAotMovieRating.JsonSerialization;
using Scalar.AspNetCore;
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
   .AddValidationForPortableResults()
   .ConfigureJsonSerialization()
   .AddInMemoryDatabase()
   .AddGetMoviesModule()
   .AddAddMovieRatingModule()
   .AddHealthChecks()
   .Services
   .AddOpenApi();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseHealthChecks("/");
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("Native AOT Movie Rating API"));
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
