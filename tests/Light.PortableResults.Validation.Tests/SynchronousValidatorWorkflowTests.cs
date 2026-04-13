using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class SynchronousValidatorWorkflowTests
{
    [Fact]
    public void Validate_ShouldNormalizeNestedGraph_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new PersonRequest
        {
            FirstName = "  Alice  ",
            Age = 42,
            PrimaryAddress = new AddressDto { ZipCode = " 12345 " },
            Addresses = [new AddressDto { ZipCode = " 54321 " }]
        };

        var result = validator.Validate(request);

        result.Should().Be(Result<PersonRequest>.Ok(request));
        request.FirstName.Should().Be("Alice");
        request.PrimaryAddress.ZipCode.Should().Be("12345");
        request.Addresses[0].ZipCode.Should().Be("54321");
    }

    [Fact]
    public void Validate_ShouldCollectNestedFailures_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new PersonRequest
        {
            FirstName = " ",
            Age = 16,
            PrimaryAddress = new AddressDto { ZipCode = " " },
            Addresses = [new AddressDto { ZipCode = "12A" }]
        };

        var result = validator.Validate(request);

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
    public void Validate_ShouldHonorExplicitTarget_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = validator.Validate(
            null!,
            ValidationTarget.Absolute("payload.person", isNormalized: true),
            displayName: "Person"
        );

        result.IsValid.Should().BeFalse();
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
    public void Validate_ShouldHonorProvidedContext_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var result = validator.Validate(
            new PersonRequest
            {
                FirstName = "  Alice  ",
                Age = 42,
                PrimaryAddress = new AddressDto { ZipCode = " 12345 " },
                Addresses = [new AddressDto { ZipCode = " 54321 " }]
            },
            context,
            target: "request"
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(ValidationWorkflowTestData.CreateValidationError("existing", "Existing", "existing"))
        );
    }

    [Fact]
    public void TryValidate_ShouldReturnValidatedValue_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new PersonRequest
        {
            FirstName = "  Alice  ",
            Age = 42,
            PrimaryAddress = new AddressDto { ZipCode = " 12345 " },
            Addresses = []
        };

        var isValid = validator.TryValidate(request, out var validatedRequest, out var failure);

        isValid.Should().BeTrue();
        validatedRequest.Should().BeSameAs(request);
        validatedRequest.FirstName.Should().Be("Alice");
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CheckForErrors_ShouldMaterializeFailure_ForSameTypeValidator()
    {
        var validator = new PersonValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new PersonRequest
        {
            FirstName = "Alice",
            Age = 16,
            PrimaryAddress = new AddressDto { ZipCode = "12345" },
            Addresses = []
        };

        var hasErrors = validator.CheckForErrors(request, out var failure);

        hasErrors.Should().BeTrue();
        failure.IsValid.Should().BeFalse();
        failure.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                        "Age must be greater than or equal to 18",
                        "Adult",
                        "age"
                    ) with
                    {
                        Metadata = MetadataObject.Create(
                            (ValidationErrorMetadataKeys.ComparativeValue, 18)
                        )
                    }
            )
        );
    }

    [Fact]
    public void ValidateChildValue_ShouldIgnoreEarlierContextFailures_ForSameTypeValidator()
    {
        var validator = new TrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var validatedValue = validator.ValidateChildValue(
            "  Alice  ",
            context,
            ValidationTarget.Relative("name", isNormalized: true),
            "Name"
        );

        validatedValue.Should().Be(ValidatedValue.Success("Alice"));
    }

    [Fact]
    public void ValidateChildValue_ShouldSupportCallerExpressionOverload_ForSameTypeValidator()
    {
        var validator = new TrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        const string name = "  Alice  ";

        var validatedValue = validator.ValidateChildValue(name, context);

        validatedValue.Should().Be(ValidatedValue.Success("Alice"));
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldTransformValidatedOutput_ForTransformingValidator()
    {
        var validator = new RegistrationValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var request = new RegistrationRequest
        {
            FirstName = "  Alice  ",
            Email = " alice@example.com ",
            Address = new AddressDto { ZipCode = " 12345 " }
        };

        var result = validator.Validate(request);

        result.Should().Be(
            Result<RegistrationCommand>.Ok(
                new RegistrationCommand("Alice", "alice@example.com", new AddressCommand("12345"))
            )
        );
    }

    [Fact]
    public void Validate_ShouldHonorExplicitTarget_ForTransformingValidator()
    {
        var validator = new RegistrationValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var result = validator.Validate(
            null!,
            ValidationTarget.Absolute("payload.registration", isNormalized: true),
            displayName: "Registration"
        );

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "Registration must not be null",
                    "NotNull",
                    "payload.registration"
                )
            )
        );
    }

    [Fact]
    public void TryValidate_And_CheckForErrors_ShouldCoverTransformingPublicWrappers()
    {
        var validator = new RegistrationValidator(ValidationWorkflowTestData.ValidationContextFactory);

        var isValid = validator.TryValidate(
            new RegistrationRequest
            {
                FirstName = "  Alice  ",
                Email = " alice@example.com ",
                Address = new AddressDto { ZipCode = " 12345 " }
            },
            out var command,
            out var successFailure
        );
        var hasErrors = validator.CheckForErrors(
            new RegistrationRequest
            {
                FirstName = " ",
                Email = "not-an-email",
                Address = new AddressDto { ZipCode = " " }
            },
            out var failure
        );

        isValid.Should().BeTrue();
        command.Should().Be(new RegistrationCommand("Alice", "alice@example.com", new AddressCommand("12345")));
        successFailure.IsValid.Should().BeTrue();
        hasErrors.Should().BeTrue();
        failure.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    ValidationWorkflowTestData.CreateValidationError(
                        "firstName must not be empty or whitespace",
                        "NotNullOrWhiteSpace",
                        "firstName"
                    ),
                    ValidationWorkflowTestData.CreateValidationError(
                        "email must be an email address",
                        "Email",
                        "email"
                    ),
                    ValidationWorkflowTestData.CreateValidationError(
                        "address.zipCode must not be empty or whitespace",
                        "NotNullOrWhiteSpace",
                        "address.zipCode"
                    )
                }
            )
        );
    }

    [Fact]
    public void ValidateChildValue_ShouldIgnoreEarlierContextFailures_ForTransformingValidator()
    {
        var validator = new AddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var validatedValue = validator.ValidateChildValue(
            new AddressDto { ZipCode = " 12345 " },
            context,
            ValidationTarget.Relative("address", isNormalized: true),
            "Address"
        );

        validatedValue.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
    }

    [Fact]
    public void ValidateChildValue_ShouldSupportCallerExpressionOverload_ForTransformingValidator()
    {
        var validator = new AddressCommandValidator(ValidationWorkflowTestData.ValidationContextFactory);
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var address = new AddressDto { ZipCode = " 12345 " };

        var validatedValue = validator.ValidateChildValue(address, context);

        validatedValue.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
        context.HasErrors.Should().BeFalse();
    }
}
