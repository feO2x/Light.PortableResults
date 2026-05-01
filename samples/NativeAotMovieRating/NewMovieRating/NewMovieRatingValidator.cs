using Light.PortableResults.Validation;

namespace NativeAotMovieRating.NewMovieRating;

public sealed class NewMovieRatingValidator : Validator<NewMovieRatingDto>
{
    public NewMovieRatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<NewMovieRatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        NewMovieRatingDto dto
    )
    {
        context.Check(dto.Id).IsNotEmpty();
        context.Check(dto.MovieId).IsNotEmpty();
        dto.Comment = context.Check(dto.Comment).HasLengthIn(10, 1000);
        dto.UserName = context.Check(dto.UserName).IsNotNullOrWhiteSpace();
        context.Check(dto.Rating).IsInBetween(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}
