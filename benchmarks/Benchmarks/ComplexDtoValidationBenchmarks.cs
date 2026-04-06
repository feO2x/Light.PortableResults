using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FluentValidation;
using FluentValidation.Results;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.AspNetCore.Mvc;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Benchmarks;

[MemoryDiagnoser]
public class ValidComplexDtoValidationBenchmarks
{
    private readonly PortablePurchaseOrderDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentPurchaseOrderDtoValidator _singletonFluentValidationValidator =
        FluentPurchaseOrderDtoValidator.CreateSingleton();

    private readonly PurchaseOrderDto _validDto = ComplexDtoValidationBenchmarkData.CreateValidDto();

    [Benchmark(Baseline = true)]
    public ValidationResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentPurchaseOrderDtoValidator();
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
    public Result<PurchaseOrderDto> LightPortableResults()
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
public class InvalidComplexDtoValidationBenchmarks
{
    private readonly PurchaseOrderDto _invalidDto = ComplexDtoValidationBenchmarkData.CreateInvalidDto();

    private readonly PortablePurchaseOrderDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentPurchaseOrderDtoValidator _singletonFluentValidationValidator =
        FluentPurchaseOrderDtoValidator.CreateSingleton();

    [Benchmark(Baseline = true)]
    public ValidationResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentPurchaseOrderDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public ValidationResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return validationResult;
    }

    [Benchmark]
    public Result<PurchaseOrderDto> LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result;
    }
}

[MemoryDiagnoser]
public class InvalidComplexDtoValidationBenchmarksForMinimalApis
{
    private readonly PurchaseOrderDto _invalidDto = ComplexDtoValidationBenchmarkData.CreateInvalidDto();

    private readonly PortablePurchaseOrderDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentPurchaseOrderDtoValidator _singletonFluentValidationValidator =
        FluentPurchaseOrderDtoValidator.CreateSingleton();

    [Benchmark(Baseline = true)]
    public IResult FluentValidationScopedOrTransient()
    {
        var validator = new FluentPurchaseOrderDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    [Benchmark]
    public IResult FluentValidationSingleton()
    {
        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return TypedResults.ValidationProblem(validationResult.ToDictionary());
    }

    [Benchmark]
    public IResult LightPortableResults()
    {
        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result.ToMinimalApiResult();
    }
}

[MemoryDiagnoser]
public class InvalidComplexDtoValidationBenchmarksForMvc : ControllerBase
{
    private readonly PurchaseOrderDto _invalidDto = ComplexDtoValidationBenchmarkData.CreateInvalidDto();

    private readonly PortablePurchaseOrderDtoValidator _lightPortableResultsValidator =
        new (new DefaultValidationContextFactory());

    private readonly FluentPurchaseOrderDtoValidator _singletonFluentValidationValidator =
        FluentPurchaseOrderDtoValidator.CreateSingleton();

    [Benchmark(Baseline = true)]
    public IActionResult FluentValidationScopedOrTransient()
    {
        ModelState.Clear();

        var validator = new FluentPurchaseOrderDtoValidator();
        var validationResult = validator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        ComplexDtoValidationBenchmarkData.CopyErrorsToModelState(ModelState, validationResult.Errors);
        return ValidationProblem();
    }

    [Benchmark]
    public IActionResult FluentValidationSingleton()
    {
        ModelState.Clear();

        var validationResult = _singletonFluentValidationValidator.Validate(_invalidDto);
        if (validationResult.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        ComplexDtoValidationBenchmarkData.CopyErrorsToModelState(ModelState, validationResult.Errors);
        return ValidationProblem();
    }

    [Benchmark]
    public IActionResult LightPortableResults()
    {
        ModelState.Clear();

        var result = _lightPortableResultsValidator.Validate(_invalidDto);
        if (result.Errors.Count != ComplexDtoValidationBenchmarkData.InvalidErrorCount)
        {
            throw new InvalidOperationException("Validation should fail in this benchmark");
        }

        return result.ToMvcActionResult();
    }
}

public sealed record PurchaseOrderDto
{
    public required Guid OrderId { get; set; }
    public required string CustomerEmail { get; set; } = string.Empty;
    public required ShippingAddressDto ShippingAddress { get; set; }
    public required List<string> Tags { get; set; }
    public required List<OrderItemDto> Items { get; set; }
}

public sealed record ShippingAddressDto
{
    public required string RecipientName { get; set; } = string.Empty;
    public required string Street { get; set; } = string.Empty;
    public required string PostalCode { get; set; } = string.Empty;
    public required string CountryCode { get; set; } = string.Empty;
}

public sealed record OrderItemDto
{
    public required string Sku { get; set; } = string.Empty;
    public required int Quantity { get; set; }
    public required decimal UnitPrice { get; set; }
}

public sealed class FluentPurchaseOrderDtoValidator : AbstractValidator<PurchaseOrderDto>
{
    public FluentPurchaseOrderDtoValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerEmail).EmailAddress();
        RuleFor(x => x.ShippingAddress)
           .SetValidator(ShippingAddressValidator ?? new FluentShippingAddressDtoValidator());
        RuleForEach(x => x.Tags).SetValidator(TagValidator ?? new FluentTagValidator());
        RuleForEach(x => x.Items).SetValidator(OrderItemValidator ?? new FluentOrderItemDtoValidator());
    }

    public FluentShippingAddressDtoValidator? ShippingAddressValidator { get; set; }
    public FluentTagValidator? TagValidator { get; set; }
    public FluentOrderItemDtoValidator? OrderItemValidator { get; set; }

    public static FluentPurchaseOrderDtoValidator CreateSingleton() =>
        new ()
        {
            ShippingAddressValidator = new FluentShippingAddressDtoValidator(),
            TagValidator = new FluentTagValidator(),
            OrderItemValidator = new FluentOrderItemDtoValidator()
        };
}

public sealed class FluentShippingAddressDtoValidator : AbstractValidator<ShippingAddressDto>
{
    public FluentShippingAddressDtoValidator()
    {
        RuleFor(x => x.RecipientName).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.PostalCode).Cascade(CascadeMode.Stop).NotEmpty().Length(4, 12);
        RuleFor(x => x.CountryCode).Length(2, 2);
    }
}

public sealed class FluentTagValidator : AbstractValidator<string>
{
    public FluentTagValidator()
    {
        RuleFor(x => x).Cascade(CascadeMode.Stop).NotEmpty().Length(2, 30);
    }
}

public sealed class FluentOrderItemDtoValidator : AbstractValidator<OrderItemDto>
{
    public FluentOrderItemDtoValidator()
    {
        RuleFor(x => x.Sku).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.UnitPrice).GreaterThan(0m);
    }
}

public sealed class PortablePurchaseOrderDtoValidator : Validator<PurchaseOrderDto>
{
    private readonly PortableShippingAddressDtoValidator _addressValidator;
    private readonly PortableOrderItemDtoValidator _itemValidator;
    private readonly PortableTagValidator _tagValidator;

    public PortablePurchaseOrderDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _addressValidator = new PortableShippingAddressDtoValidator(validationContextFactory);
        _tagValidator = new PortableTagValidator(validationContextFactory);
        _itemValidator = new PortableOrderItemDtoValidator(validationContextFactory);
    }

    protected override ValidatedValue<PurchaseOrderDto> PerformValidation(
        ValidationContext context,
        PurchaseOrderDto dto
    )
    {
        context.Check(dto.OrderId).IsNotEmpty();
        dto.CustomerEmail = context.Check(dto.CustomerEmail).IsEmail();
        context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);
        context.Check(dto.Tags).IsNotNull().ValidateItems(_tagValidator);
        context.Check(dto.Items).IsNotNull().ValidateItems(_itemValidator);
        return ValidatedValue.Success(dto);
    }
}

public sealed class PortableShippingAddressDtoValidator : Validator<ShippingAddressDto>
{
    public PortableShippingAddressDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<ShippingAddressDto> PerformValidation(
        ValidationContext context,
        ShippingAddressDto dto
    )
    {
        context.Check(dto.RecipientName).IsNotNullOrWhiteSpace();
        context.Check(dto.Street).IsNotNullOrWhiteSpace();
        context.Check(dto.PostalCode).IsNotNullOrWhiteSpace(shortCircuitOnError: true).HasLengthIn(4, 12);
        context.Check(dto.CountryCode).IsNotNullOrWhiteSpace(shortCircuitOnError: true).HasLengthIn(2, 2);
        return ValidatedValue.Success(dto);
    }
}

public sealed class PortableTagValidator : Validator<string>
{
    public PortableTagValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<string> PerformValidation(ValidationContext context, string value)
    {
        context
           .Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true), displayName: "tag")
           .IsNotNullOrWhiteSpace(shortCircuitOnError: true)
           .HasLengthIn(2, 30);
        return ValidatedValue.Success(value);
    }
}

public sealed class PortableOrderItemDtoValidator : Validator<OrderItemDto>
{
    public PortableOrderItemDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<OrderItemDto> PerformValidation(ValidationContext context, OrderItemDto dto)
    {
        context.Check(dto.Sku).IsNotNullOrWhiteSpace();
        context.Check(dto.Quantity).IsGreaterThanOrEqualTo(1);
        context.Check(dto.UnitPrice).IsGreaterThan(0m);
        return ValidatedValue.Success(dto);
    }
}

public static class ComplexDtoValidationBenchmarkData
{
    public const int InvalidErrorCount = 9;

    public static PurchaseOrderDto CreateValidDto() =>
        new ()
        {
            OrderId = Guid.CreateVersion7(),
            CustomerEmail = "alex@example.com",
            ShippingAddress = new ShippingAddressDto
            {
                RecipientName = "Alex Morgan",
                Street = "42 Market Street",
                PostalCode = "10115",
                CountryCode = "DE"
            },
            Tags = new List<string> { "priority", "gift", "retail" },
            Items = new List<OrderItemDto>
            {
                new () { Sku = "BK-1001", Quantity = 1, UnitPrice = 19.99m },
                new () { Sku = "BK-2003", Quantity = 2, UnitPrice = 7.50m },
                new () { Sku = "BK-3308", Quantity = 1, UnitPrice = 49.00m }
            }
        };

    public static PurchaseOrderDto CreateInvalidDto() =>
        new ()
        {
            OrderId = Guid.Empty,
            CustomerEmail = "not-an-email",
            ShippingAddress = new ShippingAddressDto
            {
                RecipientName = "Alex Morgan",
                Street = "42 Market Street",
                PostalCode = " ",
                CountryCode = "D"
            },
            Tags = ["priority", " ", "x"],
            Items =
            [
                new OrderItemDto { Sku = " ", Quantity = 0, UnitPrice = 0m },
                new OrderItemDto { Sku = "BK-2003", Quantity = 2, UnitPrice = 7.50m },
                new OrderItemDto { Sku = "BK-3308", Quantity = 1, UnitPrice = 49.00m }
            ]
        };

    public static void CopyErrorsToModelState(ModelStateDictionary modelState, IList<ValidationFailure> errors)
    {
        for (var i = 0; i < errors.Count; i++)
        {
            var error = errors[i];
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
