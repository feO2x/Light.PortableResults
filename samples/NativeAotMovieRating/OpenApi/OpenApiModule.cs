using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Scalar.AspNetCore;

namespace NativeAotMovieRating.OpenApi;

public static class OpenApiModule
{
    public static void MapOpenApiAndScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/docs", options => options.WithTitle("Native AOT Movie Rating API"));
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "Native AOT Movie Rating API - Swagger UI";
            options.RoutePrefix = "swagger";
            options.SwaggerEndpoint("/openapi/v1.json", "Native AOT Movie Rating API v1");
        });
    }

    public static void RedirectHomeToDocs(this WebApplication app) =>
        app.MapGet("/", () => TypedResults.LocalRedirect("/docs"));
}
