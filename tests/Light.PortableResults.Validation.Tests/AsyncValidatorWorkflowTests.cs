using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class AsyncValidatorWorkflowTests
{
    [Fact]
    public async Task ValidateAsync_ShouldNormalizeNestedGraph_ForSameTypeValidator()
    {
        var validator = new AsyncPersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new PersonRequest
        {
            FirstName = "  Alice  ",
            Age = 42,
            PrimaryAddress = new AddressDto { ZipCode = " 12345 " },
            Addresses = [new AddressDto { ZipCode = " 54321 " }]
        };

        var result = await validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.Should().Be(Result<PersonRequest>.Ok(request));
        request.FirstName.Should().Be("Alice");
        request.PrimaryAddress.ZipCode.Should().Be("12345");
        request.Addresses[0].ZipCode.Should().Be("54321");
    }

    [Fact]
    public async Task ValidateAsync_ShouldCollectNestedFailures_ForSameTypeValidator()
    {
        var validator = new AsyncPersonValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = await validator.ValidateAsync(
            new PersonRequest
            {
                FirstName = " ",
                Age = 16,
                PrimaryAddress = new AddressDto { ZipCode = " " },
                Addresses = [new AddressDto { ZipCode = "12A" }]
            },
            TestContext.Current.CancellationToken
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    ValidationWorkflowTestData.CreateValidationError(
                        "firstName must not be empty or whitespace",
                        "NotNullOrWhiteSpace",
                        "firstName"
                    ),
                    ValidationWorkflowTestData.CreateValidationError(
                            "Age must be greater than or equal to 18",
                            "Adult",
                            "age"
                        ) with
                        {
                            Metadata = MetadataObject.Create(
                                (ValidationErrorMetadataKeys.ComparativeValue, 18)
                            )
                        },
                    ValidationWorkflowTestData.CreateValidationError(
                        "primaryAddress.zipCode must not be empty or whitespace",
                        "NotNullOrWhiteSpace",
                        "primaryAddress.zipCode"
                    ),
                    ValidationWorkflowTestData.CreateValidationError(
                        "addresses[0].zipCode must contain only digits",
                        "DigitsOnly",
                        "addresses[0].zipCode"
                    )
                }
            )
        );
    }

    [Fact]
    public async Task ValidateAsync_ShouldHonorExplicitTarget_ForSameTypeValidator()
    {
        var validator = new AsyncPersonValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = await validator.ValidateAsync(
            null!,
            ValidationTarget.Absolute("payload.person", isNormalized: true),
            TestContext.Current.CancellationToken,
            displayName: "Person"
        );

        result.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "Person must not be null",
                    "NotNull",
                    "payload.person"
                )
            )
        );
    }

    [Fact]
    public async Task ValidateAsync_ShouldHonorProvidedContext_ForSameTypeValidator()
    {
        var validator = new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        const string name = "  Alice  ";

        var result = await validator.ValidateAsync(
            name,
            context,
            TestContext.Current.CancellationToken,
            displayName: "Name"
        );

        result.Should().Be(Result<string>.Ok("Alice"));
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateChildValueAsync_ShouldIgnoreEarlierContextFailures_ForSameTypeValidator()
    {
        var validator = new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var validatedValue = await validator.ValidateChildValueAsync(
            "  Alice  ",
            context,
            TestContext.Current.CancellationToken,
            ValidationTarget.Relative("name", isNormalized: true),
            "Name"
        );

        validatedValue.Should().Be(ValidatedValue.Success("Alice"));
    }

    [Fact]
    public async Task ValidateChildValueAsync_ShouldSupportCallerExpressionOverload_ForSameTypeValidator()
    {
        var validator = new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        const string name = "  Alice  ";

        var validatedValue = await validator.ValidateChildValueAsync(
            name,
            context,
            TestContext.Current.CancellationToken
        );

        validatedValue.Should().Be(ValidatedValue.Success("Alice"));
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ShouldTransformValidatedOutput_ForTransformingValidator()
    {
        var validator = new AsyncRegistrationValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = await validator.ValidateAsync(
            new RegistrationRequest
            {
                FirstName = "  Alice  ",
                Email = " alice@example.com ",
                Address = new AddressDto { ZipCode = " 12345 " }
            },
            TestContext.Current.CancellationToken
        );

        result.Should().Be(
            Result<RegistrationCommand>.Ok(
                new RegistrationCommand("Alice", "alice@example.com", new AddressCommand("12345"))
            )
        );
    }

    [Fact]
    public async Task ValidateAsync_ShouldHonorProvidedContext_ForTransformingValidator()
    {
        var validator = new AsyncRegistrationValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var result = await validator.ValidateAsync(
            new RegistrationRequest
            {
                FirstName = "  Alice  ",
                Email = "alice@example.com",
                Address = new AddressDto { ZipCode = "12345" }
            },
            context,
            TestContext.Current.CancellationToken,
            target: "request"
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(ValidationWorkflowTestData.CreateValidationError("existing", "Existing", "existing"))
        );
    }

    [Fact]
    public async Task ValidateChildValueAsync_ShouldIgnoreEarlierContextFailures_ForTransformingValidator()
    {
        var validator = new AsyncAddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var validatedValue = await validator.ValidateChildValueAsync(
            new AddressDto { ZipCode = " 12345 " },
            context,
            TestContext.Current.CancellationToken,
            ValidationTarget.Relative("address", isNormalized: true),
            "Address"
        );

        validatedValue.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
    }

    [Fact]
    public async Task ValidateChildValueAsync_ShouldSupportCallerExpressionOverload_ForTransformingValidator()
    {
        var validator = new AsyncAddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var address = new AddressDto { ZipCode = " 12345 " };

        var validatedValue = await validator.ValidateChildValueAsync(
            address,
            context,
            TestContext.Current.CancellationToken
        );

        validatedValue.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_ShouldHonorExplicitTarget_ForTransformingValidator()
    {
        var validator = new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = await validator.ValidateAsync(
            null!,
            ValidationTarget.Absolute("payload.length", isNormalized: true),
            TestContext.Current.CancellationToken,
            displayName: "Text"
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "Text must not be null",
                    "NotNull",
                    "payload.length"
                )
            )
        );
    }

    [Fact]
    public async Task ValidateAsync_ShouldPropagateCancellation()
    {
        var validator = new AsyncPersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        await cancellationTokenSource.CancelAsync();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await validator.ValidateAsync(
            new PersonRequest
            {
                FirstName = "Alice",
                Age = 42,
                PrimaryAddress = new AddressDto { ZipCode = "12345" },
                Addresses = []
            },
            cancellationTokenSource.Token
        );

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
