using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CheckExtensionsTests
{
    private static readonly DefaultValidationContextFactory ValidationContextFactory = new ();

    [Fact]
    public void ValidateChild_ShouldNormalizeRawTargetsOnlyOnce()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var addressValidator = new AddressValidator(ValidationContextFactory);

        var result = context
           .Check(new AddressDto { ZipCode = " " }, target: "request.Address", displayName: "Address")
           .ValidateChild(addressValidator);

        result.Should().Be(ValidatedValue<AddressDto>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zipCode must not be empty",
                    Code = "NotEmpty",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ValidateChild_ShouldRespectExplicitAbsoluteTargetsWithinCurrentScope()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        var check = addressContext
           .Check(
                new AddressDto { ZipCode = " " },
                ValidationTarget.Absolute("address", isNormalized: true),
                displayName: "Address"
            )
           .NormalizeTargetIfNecessary();

        var result = check.ValidateChild(new AddressValidator(ValidationContextFactory));

        result.Should().Be(ValidatedValue<AddressDto>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zipCode must not be empty",
                    Code = "NotEmpty",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldRespectExplicitAbsoluteCollectionTargetsWithinCurrentScope()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var addressesContext = context.ForMember("addresses", isNormalized: true);
        var check = addressesContext
           .Check(
                new List<AddressDto> { new () { ZipCode = " " } },
                ValidationTarget.Absolute("addresses", isNormalized: true),
                displayName: "Addresses"
            )
           .NormalizeTargetIfNecessary();

        var result = check.ValidateItems(new AddressValidator(ValidationContextFactory));

        result.Should().Be(ValidatedValue<List<AddressDto>>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zipCode must not be empty",
                    Code = "NotEmpty",
                    Target = "addresses[0].zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldReturnNoValueForShortCircuitedNullCollection()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> tags = null!;
        var check = context
           .Check(tags, NoOpStringValueNormalizer.Instance, target: "request.Tags", displayName: "Tags")
           .ShortCircuit();
        var wasCalled = false;

        var result = check.ValidateItems(
            (Action<Check<string>>) (_ => wasCalled = true)
        );

        result.Should().Be(ValidatedValue<IReadOnlyList<string>>.NoValue);
        wasCalled.Should().BeFalse();
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ValidateItems_ShouldThrowForNullCollectionWhenNotShortCircuited()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> tags = null!;
        var check = context.Check(
            tags,
            NoOpStringValueNormalizer.Instance,
            target: "request.Tags",
            displayName: "Tags"
        );

        Action act = () => check.ValidateItems(
            (Action<Check<string>>) (static _ => { })
        );

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Guard nullable collections explicitly*");
    }

    [Fact]
    public void ValidateItems_ShouldSupportDelegateBasedValidationForStringItems()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> tags = new List<string> { " ", "Alpha", "  " };
        var check = context.Check(tags, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(
            (Action<Check<string>>) (static itemCheck =>
            {
                if (string.IsNullOrWhiteSpace(itemCheck.Value))
                {
                    itemCheck.AddError("tag must not be empty", "NotEmpty");
                }
            })
        );

        result.Should().Be(ValidatedValue<IReadOnlyList<string>>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "tag must not be empty",
                        Code = "NotEmpty",
                        Target = "tags[0]",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "tag must not be empty",
                        Code = "NotEmpty",
                        Target = "tags[2]",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldSupportDelegateBasedValidationForIntItems()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<int> quantities = new List<int> { -1, 2 };
        var check = context.Check(quantities, target: "request.Quantities", displayName: "Quantities");

        var result = check.ValidateItems(
            (Action<Check<int>>) (static itemCheck =>
            {
                if (itemCheck.Value < 0)
                {
                    itemCheck.AddError("quantity must be zero or greater", "NonNegative");
                }
            })
        );

        result.Should().Be(ValidatedValue<IReadOnlyList<int>>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "quantity must be zero or greater",
                    Code = "NonNegative",
                    Target = "quantities[0]",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldSupportDelegateBasedNormalization()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var quantities = new List<int> { 1, 2, 3 };
        var check = context.Check(quantities, target: "request.Quantities", displayName: "Quantities");

        var result = check.ValidateItems(
            (Func<Check<int>, ValidatedValue<int>>) (static itemCheck => ValidatedValue.Success(itemCheck.Value + 1))
        );

        result.TryGetValue(out var validatedQuantities).Should().BeTrue();
        validatedQuantities.Should().BeSameAs(quantities);
        quantities.Should().Equal(2, 3, 4);
    }

    [Fact]
    public void ValidateItems_ShouldTransformArrayItems()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var tags = new[] { " Alice ", "Bob " };
        var check = context.Check(tags, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(new StringLengthValidator(ValidationContextFactory));

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldTransformListItems()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var tags = new List<string> { " Alice ", "Bob " };
        var check = context.Check(tags, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(new StringLengthValidator(ValidationContextFactory));

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldTransformImmutableArrayItems()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var tags = ImmutableArray.Create(" Alice ", "Bob ");
        var check = context.Check(tags, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(new StringLengthValidator(ValidationContextFactory));

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    [Fact]
    public async Task ValidateChildAsync_ShouldSupportSameTypeChildValidation()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var check = context.Check(
            new AddressDto { ZipCode = " " },
            target: "request.Address",
            displayName: "Address"
        );

        var result = await check.ValidateChildAsync(
            new AsyncAddressValidator(ValidationContextFactory),
            TestContext.Current.CancellationToken
        );

        result.Should().Be(ValidatedValue<AddressDto>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "zipCode must not be empty",
                    Code = "NotEmpty",
                    Target = "address.zipCode",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public async Task ValidateChildAsync_ShouldSupportTransformingChildValidation()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var check = context.Check(
            new AddressDto { ZipCode = " 12345 " },
            target: "request.Address",
            displayName: "Address"
        );

        var result = await check.ValidateChildAsync(
            new AsyncAddressTransformValidator(ValidationContextFactory),
            TestContext.Current.CancellationToken
        );

        result.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportValidatorBasedItemValidation()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var tags = new List<string> { " Alice ", "Bob " };
        var check = context.Check(tags, target: "request.Tags", displayName: "Tags");

        var result = await check.ValidateItemsAsync(
            new AsyncStringLengthValidator(ValidationContextFactory),
            TestContext.Current.CancellationToken
        );

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportDelegateBasedValidation()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<int> quantities = new List<int> { 1, -2 };
        var check = context.Check(quantities, target: "request.Quantities", displayName: "Quantities");

        var result = await check.ValidateItemsAsync(
            (Func<Check<int>, CancellationToken, ValueTask>) (async (itemCheck, cancellationToken) =>
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                if (itemCheck.Value < 0)
                {
                    itemCheck.AddError("quantity must be zero or greater", "NonNegative");
                }
            }),
            TestContext.Current.CancellationToken
        );

        result.Should().Be(ValidatedValue<IReadOnlyList<int>>.NoValue);
        context.ToErrors().Should().Equal(
            new Errors(
                new Error
                {
                    Message = "quantity must be zero or greater",
                    Code = "NonNegative",
                    Target = "quantities[1]",
                    Category = ErrorCategory.Validation
                }
            )
        );
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportDelegateBasedNormalization()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var quantities = new List<int> { 1, 2 };
        var check = context.Check(quantities, target: "request.Quantities", displayName: "Quantities");

        var result = await check.ValidateItemsAsync(
            (Func<Check<int>, CancellationToken, ValueTask<ValidatedValue<int>>>) (
                async (itemCheck, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    return ValidatedValue.Success(itemCheck.Value * 2);
                }),
            TestContext.Current.CancellationToken
        );

        result.TryGetValue(out var validatedQuantities).Should().BeTrue();
        validatedQuantities.Should().BeSameAs(quantities);
        quantities.Should().Equal(2, 4);
    }

    private sealed class AddressDto
    {
        public string? ZipCode { get; set; }
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local -- required for test scenario
    private sealed record AddressCommand(string ZipCode);

    private sealed class AddressValidator : Validator<AddressDto>
    {
        public AddressValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<AddressDto> PerformValidation(ValidationContext context, AddressDto value)
        {
            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            value.ZipCode = zipCode.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value.ZipCode))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return context.HasErrors ? ValidatedValue<AddressDto>.NoValue : ValidatedValue.Success(value);
        }
    }

    private sealed class AsyncAddressValidator : AsyncValidator<AddressDto>
    {
        public AsyncAddressValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<ValidatedValue<AddressDto>> PerformValidationAsync(
            ValidationContext context,
            AddressDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            value.ZipCode = zipCode.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value.ZipCode))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return context.HasErrors ? ValidatedValue<AddressDto>.NoValue : ValidatedValue.Success(value);
        }
    }

    private sealed class AsyncAddressTransformValidator : AsyncValidator<AddressDto, AddressCommand>
    {
        public AsyncAddressTransformValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<ValidatedValue<AddressCommand>> PerformValidationAsync(
            ValidationContext context,
            AddressDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            var normalizedZipCode = zipCode.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedZipCode))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return context.HasErrors ?
                ValidatedValue<AddressCommand>.NoValue :
                ValidatedValue.Success(new AddressCommand(normalizedZipCode));
        }
    }

    private sealed class StringLengthValidator : Validator<string, int>
    {
        public StringLengthValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<int> PerformValidation(ValidationContext context, string value)
        {
            var text = context.Check(value).NormalizeTargetIfNecessary();
            if (text.Value.Length == 0)
            {
                text.AddError("tag must not be empty", "NotEmpty");
            }

            return context.HasErrors ? ValidatedValue<int>.NoValue : ValidatedValue.Success(text.Value.Length);
        }
    }

    private sealed class AsyncStringLengthValidator : AsyncValidator<string, int>
    {
        public AsyncStringLengthValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<ValidatedValue<int>> PerformValidationAsync(
            ValidationContext context,
            string value,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var text = context.Check(value).NormalizeTargetIfNecessary();
            if (text.Value.Length == 0)
            {
                text.AddError("tag must not be empty", "NotEmpty");
            }

            return context.HasErrors ? ValidatedValue<int>.NoValue : ValidatedValue.Success(text.Value.Length);
        }
    }
}
