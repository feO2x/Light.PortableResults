using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NativeAotMovieRating.DatabaseAccess;

public sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movies");
        builder.HasKey(movie => movie.Id);
        builder.Property(movie => movie.Id).ValueGeneratedNever();
        builder.Property(movie => movie.Title).IsRequired().HasMaxLength(200);

        // Movies are paginated with a keyset over (Title, Id), so the index must cover both columns
        // in exactly that order to keep the pagination query a plain index range scan.
        builder.HasIndex(movie => new { movie.Title, movie.Id });

        builder
           .HasMany(movie => movie.Ratings)
           .WithOne()
           .HasForeignKey(rating => rating.MovieId)
           .OnDelete(DeleteBehavior.Cascade);
    }
}
