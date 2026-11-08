using System;
using Light.PortableResults.Validation;

namespace Light.PortableResults.Documentation.Tests.Walkthrough;

// The README walkthrough shows the following code verbatim (step 2). Keep both in sync - ReadmeSnippetTests
// fails when they drift apart.

public sealed record MovieRatingDto
{
    public required Guid Id { get; init; }
    public required Guid MovieId { get; init; }
    public required string UserName { get; set; }
    public required string Comment { get; set; }
    public required int Rating { get; init; }
}

public sealed class MovieRatingValidator : Validator<MovieRatingDto>
{
    public MovieRatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<MovieRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        MovieRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();

        // Strings are trimmed by default - assign the result to keep the normalized value.
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();

        context.Check(dto.Rating).IsInRange(1, 5);

        return checkpoint.ToValidatedValue(dto);
    }
}
