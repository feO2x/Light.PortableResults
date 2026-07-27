using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NativeAotMovieRating.DatabaseAccess;

public sealed class MovieRatingConfiguration : IEntityTypeConfiguration<MovieRating>
{
    public void Configure(EntityTypeBuilder<MovieRating> builder)
    {
        builder.ToTable("MovieRatings");
        builder.HasKey(rating => rating.Id);
        builder.Property(rating => rating.Id).ValueGeneratedNever();
        builder.Property(rating => rating.UserName).IsRequired().HasMaxLength(100);
        builder.Property(rating => rating.Comment).IsRequired().HasMaxLength(1000);
        builder.Property(rating => rating.Rating).IsRequired();
        builder.HasIndex(rating => rating.MovieId);
    }
}
