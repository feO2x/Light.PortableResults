using System;
using Light.PortableResults.Validation;

namespace Light.PortableResults.Documentation.Tests.Walkthrough;

// The README walkthrough shows the following code verbatim (step 1). Keep both in sync - ReadmeSnippetTests
// fails when they drift apart.

public sealed record MovieRating(Guid Id, Guid MovieId, string UserName, string Comment, int Rating);

public sealed class AddMovieRatingService
{
    private readonly MovieRatingValidator _validator;

    public AddMovieRatingService(MovieRatingValidator validator) => _validator = validator;

    public Result<MovieRating> AddMovieRating(MovieRatingDto dto)
    {
        if (_validator.CheckForErrors(dto, out var failure))
        {
            return Result<MovieRating>.Fail(failure.Errors);
        }

        var rating = new MovieRating(dto.Id, dto.MovieId, dto.UserName, dto.Comment, dto.Rating);
        return Result<MovieRating>.Ok(rating);
    }
}
