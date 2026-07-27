using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Used by the dotnet-ef tooling ("dotnet ef migrations add ...") so that it does not need to boot
/// the whole web application to obtain a <see cref="MovieRatingDbContext" />.
/// </summary>
public sealed class MovieRatingDbContextFactory : IDesignTimeDbContextFactory<MovieRatingDbContext>
{
    public MovieRatingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: true)
           .AddJsonFile("appsettings.Development.json", optional: true)
           .AddEnvironmentVariables()
           .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MovieRatingDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetMovieRatingConnectionString());
        return new MovieRatingDbContext(optionsBuilder.Options);
    }
}
