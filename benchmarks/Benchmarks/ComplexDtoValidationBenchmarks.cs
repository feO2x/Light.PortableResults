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
        PortablePurchaseOrderDtoValidator.CreateSingleton(new DefaultValidationContextFactory());

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
        PortablePurchaseOrderDtoValidator.CreateSingleton(new DefaultValidationContextFactory());

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
        PortablePurchaseOrderDtoValidator.CreateSingleton(new DefaultValidationContextFactory());

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
        PortablePurchaseOrderDtoValidator.CreateSingleton(new DefaultValidationContextFactory());

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
        RuleForEach(x => x.Tags).Cascade(CascadeMode.Stop).NotEmpty().Length(2, 30);
        RuleForEach(x => x.Items).SetValidator(OrderItemValidator ?? new FluentOrderItemDtoValidator());
    }

    public FluentShippingAddressDtoValidator? ShippingAddressValidator { get; set; }
    public FluentOrderItemDtoValidator? OrderItemValidator { get; set; }

    public static FluentPurchaseOrderDtoValidator CreateSingleton() =>
        new ()
        {
            ShippingAddressValidator = new FluentShippingAddressDtoValidator(),
            OrderItemValidator = new FluentOrderItemDtoValidator()
        };
}

public sealed class FluentShippingAddressDtoValidator : AbstractValidator<ShippingAddressDto>
{
    public FluentShippingAddressDtoValidator()
    {
        RuleFor(x => x.RecipientName).NotEmpty();
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.PostalCode).NotEmpty().Length(4, 12);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2, 2);
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

    public PortablePurchaseOrderDtoValidator(
        IValidationContextFactory validationContextFactory,
        PortableShippingAddressDtoValidator addressValidator,
        PortableOrderItemDtoValidator itemValidator
    )
        : base(validationContextFactory)
    {
        _addressValidator = addressValidator ?? throw new ArgumentNullException(nameof(addressValidator));
        _itemValidator = itemValidator ?? throw new ArgumentNullException(nameof(itemValidator));
    }

    public static PortablePurchaseOrderDtoValidator CreateSingleton(IValidationContextFactory validationContextFactory)
    {
        if (validationContextFactory is null)
        {
            throw new ArgumentNullException(nameof(validationContextFactory));
        }

        return new PortablePurchaseOrderDtoValidator(
            validationContextFactory,
            new PortableShippingAddressDtoValidator(validationContextFactory),
            new PortableOrderItemDtoValidator(validationContextFactory)
        );
    }

    protected override ValidatedValue<PurchaseOrderDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        PurchaseOrderDto dto
    )
    {
        context.Check(dto.OrderId).IsNotEmpty();
        dto.CustomerEmail = context.Check(dto.CustomerEmail).IsEmail();
        context.Check(dto.ShippingAddress).ValidateChild(_addressValidator);
        // We don't need IsNotNullOrWhiteSpace because of the default string normalization
        context.Check(dto.Tags).IsNotNull().ValidateItems(
            static (Check<string> tag) => tag.HasLengthIn(2, 30)
        );
        context.Check(dto.Items).ValidateItems(_itemValidator);
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class PortableShippingAddressDtoValidator : Validator<ShippingAddressDto>
{
    public PortableShippingAddressDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<ShippingAddressDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        ShippingAddressDto dto
    )
    {
        dto.RecipientName = context.Check(dto.RecipientName).IsNotNullOrWhiteSpace();
        dto.Street = context.Check(dto.Street).IsNotNullOrWhiteSpace();
        // We don't need IsNotNullOrWhiteSpace because of the default string normalization
        dto.PostalCode = context.Check(dto.PostalCode).HasLengthIn(4, 12);
        dto.CountryCode = context.Check(dto.CountryCode).HasLengthIn(2, 2);
        return checkpoint.ToValidatedValue(dto);
    }
}

public sealed class PortableOrderItemDtoValidator : Validator<OrderItemDto>
{
    public PortableOrderItemDtoValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<OrderItemDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        OrderItemDto dto
    )
    {
        context.Check(dto.Sku).IsNotNullOrWhiteSpace();
        context.Check(dto.Quantity).IsGreaterThanOrEqualTo(1);
        context.Check(dto.UnitPrice).IsGreaterThan(0m);
        return checkpoint.ToValidatedValue(dto);
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
