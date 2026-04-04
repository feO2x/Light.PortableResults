using System;
using BenchmarkDotNet.Attributes;
using FluentValidation;
using FluentValidation.Results;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.Mvc;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benchmarks;

[MemoryDiagnoser]
public class ValidFlatDtoValidationBenchmarks
{
    private readonly LightPortableResultsMovieRatingDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentValidationMovieRatingDtoValidator _singletonFluentValidationValidator = new ();

    private readonly MovieRatingDto _validDto = new ()
    {
        Id = Guid.CreateVersion7(),
        Comment = "This is a comment which has more than 10 and less than 1000 characters",
        Rating = 4
    };

    [Benchmark(Baseline = true)]
    public ValidationResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentValidationMovieRatingDtoValidator();
        var validationResult = validator.Validate(_validDto);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException("Validation shouldn't fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public ValidationResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_validDto);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException("Validation shouldn't fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public Result<MovieRatingDto> LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_validDto);
        if (!result.IsValid)
        {
            throw new InvalidOperationException("Validation shouldn't fail in this benchmark");
        }

        return result;
    }
}

[MemoryDiagnoser]
public class InvalidFlatDtoValidationBenchmarks
{
    private readonly MovieRatingDto _invalidDto = new ()
    {
        Id = Guid.Empty,
        Comment = "Too short",
        Rating = 0
    };

    private readonly LightPortableResultsMovieRatingDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentValidationMovieRatingDtoValidator _singletonFluentValidationValidator = new ();

    [Benchmark(Baseline = true)]
    public ValidationResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentValidationMovieRatingDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public ValidationResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public Result<MovieRatingDto> LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result;
    }
}

[MemoryDiagnoser]
public class InvalidFlatDtoValidationBenchmarksForMinimalApis
{
    private readonly MovieRatingDto _invalidDto = new ()
    {
        Id = Guid.Empty,
        Comment = "Too short",
        Rating = 0
    };

    private readonly LightPortableResultsMovieRatingDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentValidationMovieRatingDtoValidator _singletonFluentValidationValidator = new ();

    [Benchmark(Baseline = true)]
    public IResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentValidationMovieRatingDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    [Benchmark]
    public IResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    [Benchmark]
    public IResult LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result.ToMinimalApiResult();
    }
}

[MemoryDiagnoser]
public class InvalidFlatDtoValidationBenchmarksForMvc : ControllerBase
{
    private readonly MovieRatingDto _invalidDto = new ()
    {
        Id = Guid.Empty,
        Comment = "Too short",
        Rating = 0
    };

    private readonly LightPortableResultsMovieRatingDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentValidationMovieRatingDtoValidator _singletonFluentValidationValidator = new ();

    [Benchmark(Baseline = true)]
    public IActionResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentValidationMovieRatingDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem();
    }

    [Benchmark]
    public IActionResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        foreach (var error in validationResult.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem();
    }

    [Benchmark]
    public IActionResult LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != 3)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result.ToMvcActionResult();
    }
}

public sealed record MovieRatingDto
{
    public required Guid Id { get; set; }
    public required string Comment { get; set; } = string.Empty;
    public required int Rating { get; set; }
}

public sealed class FluentValidationMovieRatingDtoValidator : AbstractValidator<MovieRatingDto>
{
    public FluentValidationMovieRatingDtoValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().Length(10, 1000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

public sealed class LightPortableResultsMovieRatingDtoValidator : Validator<MovieRatingDto>
{
    public LightPortableResultsMovieRatingDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<MovieRatingDto> PerformValidation(ValidationContext context, MovieRatingDto dto)
    {
        context.Check(dto.Id).IsNotEmpty();
        dto.Comment = context.Check(dto.Comment).IsNotNullOrWhiteSpace().HasLengthIn(10, 1000);
        context.Check(dto.Rating).IsIn(1, 5);
        return ValidatedValue.Success(dto);
    }
}
