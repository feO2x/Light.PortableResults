using Light.PortableResults.Validation;

namespace NativeAotMovieRating.AddMovieRating;

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
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();
        context.Check(dto.Rating).IsIn(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}
