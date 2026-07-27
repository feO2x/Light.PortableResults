using Microsoft.EntityFrameworkCore;

namespace NativeAotMovieRating.DatabaseAccess;

public sealed class MovieRatingDbContext : DbContext
{
    public MovieRatingDbContext(DbContextOptions<MovieRatingDbContext> options) : base(options) { }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieRating> MovieRatings => Set<MovieRating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MovieConfiguration());
        modelBuilder.ApplyConfiguration(new MovieRatingConfiguration());
    }
}
