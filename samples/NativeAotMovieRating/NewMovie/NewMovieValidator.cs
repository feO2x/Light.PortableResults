using Light.PortableResults.Validation;

namespace NativeAotMovieRating.NewMovie;

public sealed class NewMovieValidator : Validator<NewMovieDto>
{
    public NewMovieValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<NewMovieDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        NewMovieDto value
    )
    {
        context.Check(value.MovieId).IsNotEmpty();
        context.Check(value.MovieName).IsNotNullOrWhiteSpace();
        return checkpoint.ToValidatedValue(value);
    }
}
