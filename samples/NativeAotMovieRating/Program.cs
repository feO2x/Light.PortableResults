using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.Shared;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using NativeAotMovieRating.AddMovieRating;
using NativeAotMovieRating.GetMovies;
using NativeAotMovieRating.InMemoryDatabaseAccess;
using NativeAotMovieRating.JsonSerialization;
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
   .AddValidationForPortableResults()
   .ConfigureJsonSerialization()
   .AddInMemoryDatabase()
   .AddGetMoviesModule()
   .AddAddMovieRatingModule()
   .AddHealthChecks()
   .Services
   .AddOpenApi(
        options => options.CreateSchemaReferenceId = type =>
            PortableResultsOpenApiNamingConventions.TryCreateSchemaReferenceId(type) ??
            OpenApiOptions.CreateDefaultSchemaReferenceId(type)
    );

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseHealthChecks("/health");
app.MapOpenApiAndScalar();
app.MapGetMoviesEndpoint();
app.MapAddMovieRatingEndpoint();
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
