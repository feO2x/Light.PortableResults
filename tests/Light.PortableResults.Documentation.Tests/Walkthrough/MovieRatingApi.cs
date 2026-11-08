using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Light.PortableResults.Documentation.Tests.Walkthrough;

// The README walkthrough shows the service registration and the endpoint of this app verbatim (step 3).
// Only the in-memory test server is test-specific. Keep both in sync - ReadmeSnippetTests fails when they
// drift apart.
public static class MovieRatingApi
{
    public static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services
            .AddPortableResultsForMinimalApis()
            .AddValidationForPortableResults()
            .Configure<PortableResultsHttpWriteOptions>(
                x => x.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich
            )
            .AddSingleton<MovieRatingValidator>()
            .AddSingleton<AddMovieRatingService>();

        // Test-specific: host the app in memory instead of on Kestrel.
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapPost("/api/movieRatings", (MovieRatingDto dto, AddMovieRatingService service) =>
        {
            Result<MovieRating> result = service.AddMovieRating(dto);
            return result.ToMinimalApiResult();
        });

        return app;
    }
}
