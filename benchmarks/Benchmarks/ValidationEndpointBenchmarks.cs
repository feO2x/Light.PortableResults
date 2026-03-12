using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FluentValidation;
using FluentValidation.Results;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.Http;

namespace Benchmarks;

[MemoryDiagnoser]
public class SimpleValidationEndpointBenchmarks
{
    private FluentSimpleRequestValidator _fluentValidator = null!;
    private PortableSimpleRequestValidator _portableValidator = null!;
    private SimpleRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fluentValidator = new FluentSimpleRequestValidator();
        _portableValidator = new PortableSimpleRequestValidator(new DefaultValidationContextFactory());
        _request = new SimpleRequest { FirstName = " ", Age = 16 };
    }

    [Benchmark(Baseline = true)]
    public IResult FluentValidationEndpoint() => MapSimpleFluent(
        ValidationBenchmarkHelpers.Clone(_request),
        _fluentValidator
    );

    [Benchmark]
    public IResult PortableResultsValidationEndpoint() =>
        MapSimplePortable(ValidationBenchmarkHelpers.Clone(_request), _portableValidator);

    private static IResult MapSimpleFluent(SimpleRequest request, FluentSimpleRequestValidator validator)
    {
        request.FirstName = ValidationBenchmarkHelpers.NormalizeString(request.FirstName);
        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationBenchmarkHelpers.ToFailureResult(validationResult).ToMinimalApiResult();
        }

        var command = new SimpleCommand(request.FirstName!, request.Age);
        return Result<SimpleCommand>.Ok(command).ToMinimalApiResult();
    }

    private static IResult MapSimplePortable(SimpleRequest request, PortableSimpleRequestValidator validator)
    {
        return validator.Validate(request).ToMinimalApiResult();
    }
}

[MemoryDiagnoser]
public class ComplexValidationEndpointBenchmarks
{
    private FluentComplexRequestValidator _fluentValidator = null!;
    private PortableComplexRequestValidator _portableValidator = null!;
    private ComplexRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fluentValidator = new FluentComplexRequestValidator();
        _portableValidator = new PortableComplexRequestValidator(new DefaultValidationContextFactory());
        _request = new ComplexRequest
        {
            Email = "not-an-email",
            Address = new AddressRequest { ZipCode = " " },
            Items = new List<ItemRequest>
            {
                new () { Sku = " ", Quantity = 0 },
                new () { Sku = "ABC-123", Quantity = 1 }
            }
        };
    }

    [Benchmark(Baseline = true)]
    public IResult FluentValidationEndpoint() =>
        MapComplexFluent(ValidationBenchmarkHelpers.Clone(_request), _fluentValidator);

    [Benchmark]
    public IResult PortableResultsValidationEndpoint() =>
        MapComplexPortable(ValidationBenchmarkHelpers.Clone(_request), _portableValidator);

    [Benchmark]
    public int PortableResultsNestedValidationOnly() =>
        _portableValidator.Validate(ValidationBenchmarkHelpers.Clone(_request)).Errors.Count;

    private static IResult MapComplexFluent(ComplexRequest request, FluentComplexRequestValidator validator)
    {
        ValidationBenchmarkHelpers.NormalizeComplexRequest(request);
        var validationResult = validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return ValidationBenchmarkHelpers.ToFailureResult(validationResult).ToMinimalApiResult();
        }

        var command = new ComplexCommand(
            request.Email!,
            new AddressCommand(request.Address!.ZipCode!),
            new[]
            {
                new ItemCommand(request.Items![0].Sku!, request.Items[0].Quantity),
                new ItemCommand(request.Items[1].Sku!, request.Items[1].Quantity)
            }
        );
        return Result<ComplexCommand>.Ok(command).ToMinimalApiResult();
    }

    private static IResult MapComplexPortable(ComplexRequest request, PortableComplexRequestValidator validator)
    {
        return validator.Validate(request).ToMinimalApiResult();
    }
}

internal sealed class PortableSimpleRequestValidator : Validator<SimpleRequest, SimpleCommand>
{
    public PortableSimpleRequestValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<SimpleCommand> PerformValidation(ValidationContext context, SimpleRequest value)
    {
        var firstName = context.Check(value.FirstName).NormalizeTargetIfNecessary();
        value.FirstName = firstName.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value.FirstName))
        {
            firstName.AddError("firstName must not be empty", "NotEmpty");
        }

        if (value.Age < 18)
        {
            context.Check(value.Age).AddError("age must be at least 18", "Adult");
        }

        if (context.HasErrors)
        {
            return ValidatedValue<SimpleCommand>.NoValue;
        }

        return ValidatedValue.Success(new SimpleCommand(value.FirstName, value.Age));
    }
}

internal sealed class PortableComplexRequestValidator : Validator<ComplexRequest, ComplexCommand>
{
    private readonly PortableAddressValidator _addressValidator;
    private readonly PortableItemValidator _itemValidator;

    public PortableComplexRequestValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _addressValidator = new PortableAddressValidator(validationContextFactory);
        _itemValidator = new PortableItemValidator(validationContextFactory);
    }

    protected override ValidatedValue<ComplexCommand> PerformValidation(ValidationContext context, ComplexRequest value)
    {
        var email = context.Check(value.Email).NormalizeTargetIfNecessary();
        value.Email = email.Value ?? string.Empty;
        if (!value.Email.Contains("@", StringComparison.Ordinal))
        {
            email.AddError("email must be an email address", "Email");
        }

        AddressCommand? addressCommand = null;
        if (value.Address is not null)
        {
            var addressOutcome = ValidateChild(_addressValidator, value.Address, context.For(value.Address));
            if (addressOutcome.TryGetValue(out var validatedAddress))
            {
                addressCommand = validatedAddress;
            }
        }

        var items = value.Items ?? new List<ItemRequest>();
        var itemCommands = new ItemCommand[items.Count];
        var itemsContext = context.ForMember("items", isNormalized: true);
        for (var i = 0; i < items.Count; i++)
        {
            var childContext = itemsContext.ForIndex(i);
            var itemOutcome = ValidateChild(_itemValidator, items[i], childContext);
            if (itemOutcome.TryGetValue(out var validatedItem))
            {
                itemCommands[i] = validatedItem;
            }
        }

        if (context.HasErrors)
        {
            return ValidatedValue<ComplexCommand>.NoValue;
        }

        return ValidatedValue.Success(new ComplexCommand(value.Email, addressCommand!, itemCommands));
    }
}

internal sealed class PortableAddressValidator : Validator<AddressRequest, AddressCommand>
{
    public PortableAddressValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<AddressCommand> PerformValidation(ValidationContext context, AddressRequest value)
    {
        var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
        value.ZipCode = zipCode.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value.ZipCode))
        {
            zipCode.AddError("zipCode must not be empty", "NotEmpty");
        }

        if (context.HasErrors)
        {
            return ValidatedValue<AddressCommand>.NoValue;
        }

        return ValidatedValue.Success(new AddressCommand(value.ZipCode));
    }
}

internal sealed class PortableItemValidator : Validator<ItemRequest, ItemCommand>
{
    public PortableItemValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<ItemCommand> PerformValidation(ValidationContext context, ItemRequest value)
    {
        var sku = context.Check(value.Sku).NormalizeTargetIfNecessary();
        value.Sku = sku.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value.Sku))
        {
            sku.AddError("sku must not be empty", "NotEmpty");
        }

        if (value.Quantity < 1)
        {
            context.Check(value.Quantity).AddError("quantity must be at least 1", "MinQuantity");
        }

        if (context.HasErrors)
        {
            return ValidatedValue<ItemCommand>.NoValue;
        }

        return ValidatedValue.Success(new ItemCommand(value.Sku, value.Quantity));
    }
}

internal sealed class FluentSimpleRequestValidator : AbstractValidator<SimpleRequest>
{
    public FluentSimpleRequestValidator()
    {
        RuleFor(x => x.FirstName)
           .Custom(
                (value, context) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        context.AddFailure(
                            new ValidationFailure("firstName", "firstName must not be empty")
                            {
                                ErrorCode = "NotEmpty"
                            }
                        );
                    }
                }
            );

        RuleFor(x => x.Age)
           .Custom(
                (value, context) =>
                {
                    if (value < 18)
                    {
                        context.AddFailure(
                            new ValidationFailure("age", "age must be at least 18")
                            {
                                ErrorCode = "Adult"
                            }
                        );
                    }
                }
            );
    }
}

internal sealed class FluentComplexRequestValidator : AbstractValidator<ComplexRequest>
{
    public FluentComplexRequestValidator()
    {
        RuleFor(x => x.Email)
           .Custom(
                (value, context) =>
                {
                    if (value is null || !value.Contains("@", StringComparison.Ordinal))
                    {
                        context.AddFailure(
                            new ValidationFailure("email", "email must be an email address")
                            {
                                ErrorCode = "Email"
                            }
                        );
                    }
                }
            );

        RuleFor(x => x.Address)
           .Custom(
                (value, context) =>
                {
                    if (value is not null && string.IsNullOrWhiteSpace(value.ZipCode))
                    {
                        context.AddFailure(
                            new ValidationFailure("address.zipCode", "zipCode must not be empty")
                            {
                                ErrorCode = "NotEmpty"
                            }
                        );
                    }
                }
            );

        RuleForEach(x => x.Items)
           .Custom(
                (value, context) =>
                {
                    if (value is null)
                    {
                        return;
                    }

                    var propertyName = context.PropertyPath;
                    if (string.IsNullOrWhiteSpace(value.Sku))
                    {
                        context.AddFailure(
                            new ValidationFailure(propertyName + ".sku", "sku must not be empty")
                            {
                                ErrorCode = "NotEmpty"
                            }
                        );
                    }

                    if (value.Quantity < 1)
                    {
                        context.AddFailure(
                            new ValidationFailure(propertyName + ".quantity", "quantity must be at least 1")
                            {
                                ErrorCode = "MinQuantity"
                            }
                        );
                    }
                }
            );
    }
}

internal sealed class SimpleRequest
{
    public string? FirstName { get; set; }

    public int Age { get; set; }
}

internal sealed class ComplexRequest
{
    public string? Email { get; set; }

    public AddressRequest? Address { get; set; }

    public List<ItemRequest>? Items { get; set; }
}

internal sealed class AddressRequest
{
    public string? ZipCode { get; set; }
}

internal sealed class ItemRequest
{
    public string? Sku { get; set; }

    public int Quantity { get; set; }
}

internal sealed record SimpleCommand(string FirstName, int Age);

internal sealed record ComplexCommand(string Email, AddressCommand Address, ItemCommand[] Items);

internal sealed record AddressCommand(string ZipCode);

internal sealed record ItemCommand(string Sku, int Quantity);

internal static class ValidationBenchmarkHelpers
{
    public static readonly DefaultValidationContextFactory ValidationContextFactory = new ();

    public static Result ToFailureResult(ValidationResult validationResult)
    {
        var errors = new Error[validationResult.Errors.Count];
        for (var i = 0; i < validationResult.Errors.Count; i++)
        {
            var failure = validationResult.Errors[i];
            errors[i] = new Error
            {
                Message = failure.ErrorMessage,
                Code = failure.ErrorCode,
                Target = failure.PropertyName,
                Category = ErrorCategory.Validation
            };
        }

        return Result.Fail(errors);
    }

    public static string NormalizeString(string? value) => value?.Trim() ?? string.Empty;

    public static void NormalizeComplexRequest(ComplexRequest request)
    {
        request.Email = NormalizeString(request.Email);
        if (request.Address is not null)
        {
            request.Address.ZipCode = NormalizeString(request.Address.ZipCode);
        }

        if (request.Items is null)
        {
            return;
        }

        foreach (var item in request.Items)
        {
            item.Sku = NormalizeString(item.Sku);
        }
    }

    public static SimpleRequest Clone(SimpleRequest request) =>
        new () { FirstName = request.FirstName, Age = request.Age };

    public static ComplexRequest Clone(ComplexRequest request)
    {
        var clone = new ComplexRequest
        {
            Email = request.Email,
            Address = request.Address is null ? null : new AddressRequest { ZipCode = request.Address.ZipCode },
            Items = new List<ItemRequest>()
        };

        if (request.Items is not null)
        {
            foreach (var item in request.Items)
            {
                clone.Items.Add(new ItemRequest { Sku = item.Sku, Quantity = item.Quantity });
            }
        }

        return clone;
    }
}
