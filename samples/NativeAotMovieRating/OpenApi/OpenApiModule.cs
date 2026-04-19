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
    }

    public static void RedirectHomeToDocs(this WebApplication app) =>
        app.MapGet("/", () => TypedResults.LocalRedirect("/docs"));
}
