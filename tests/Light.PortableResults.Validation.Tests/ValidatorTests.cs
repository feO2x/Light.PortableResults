using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidatorTests
{
    private static readonly DefaultValidationContextFactory ValidationContextFactory = new ();

    [Fact]
    public void Validator_ShouldReturnNormalizedValueOnSuccess()
    {
        var validator = new PersonValidator(ValidationContextFactory);
        var dto = new PersonDto
        {
            FirstName = "  Alice  ",
            Age = 42,
            Address = new AddressDto { ZipCode = " 12345 " }
        };

        var outcome = validator.Validate(dto);

        outcome.IsValid.Should().BeTrue();
        outcome.Value.Should().BeSameAs(dto);
        outcome.Value.FirstName.Should().Be("Alice");
        outcome.Value.Address!.ZipCode.Should().Be("12345");
    }

    [Fact]
    public void Validator_ShouldAccumulateFailuresWithFlatHierarchicalTargets()
    {
        var validator = new PersonValidator(ValidationContextFactory);
        var dto = new PersonDto
        {
            FirstName = " ",
            Age = 16,
            Address = new AddressDto { ZipCode = " " },
            Addresses = new List<AddressDto> { new () { ZipCode = " " } }
        };

        var outcome = validator.Validate(dto);

        outcome.IsValid.Should().BeFalse();
        outcome.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "firstName must not be empty",
                        Code = "NotEmpty",
                        Target = "firstName",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "age must be at least 18",
                        Code = "Adult",
                        Target = "age",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "zipCode must not be empty",
                        Code = "NotEmpty",
                        Target = "address.zipCode",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "zipCode must not be empty",
                        Code = "NotEmpty",
                        Target = "addresses[0].zipCode",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void Validator_ShouldCreateAutomaticNullValidationErrorForRootObject()
    {
        var validator = new PersonValidator(ValidationContextFactory);
        PersonDto? dto = null;

        var outcome = validator.Validate(dto);

        outcome.IsValid.Should().BeFalse();
        outcome.Errors.Should().ContainSingle();
        outcome.Errors.First.Should().Be(
            new Error
            {
                Message = "dto must not be null",
                Code = "NotNull",
                Target = "",
                Category = ErrorCategory.Validation
            }
        );
    }

    [Fact]
    public void CheckForErrors_ShouldProduceFailureResultForEndpointCode()
    {
        var validator = new PersonValidator(ValidationContextFactory);
        var dto = new PersonDto { FirstName = "Alice", Age = 16 };

        var hasErrors = validator.CheckForErrors(dto, out var failure);

        hasErrors.Should().BeTrue();
        failure.IsValid.Should().BeFalse();
        failure.Errors.Should().ContainSingle();
        failure.Errors[0].Target.Should().Be("age");
    }

    [Fact]
    public void TransformedValidator_ShouldReturnValidatedOutputOnSuccess()
    {
        var validator = new RegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = "  Alice  ", Email = "alice@example.com" };

        var isValid = validator.TryValidate(dto, out var command, out var failure);

        isValid.Should().BeTrue();
        command.Should().Be(new CreatePersonCommand("Alice", "alice@example.com"));
        failure.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TransformedValidator_ShouldReturnFailureAndNoValidatedOutputOnError()
    {
        var validator = new RegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = " ", Email = "not-an-email" };

        var isValid = validator.TryValidate(dto, out CreatePersonCommand? command, out var failure);

        isValid.Should().BeFalse();
        command.Should().BeNull();
        failure.IsValid.Should().BeFalse();
        failure.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task AsyncValidator_ShouldReturnOutcomeOnSuccess()
    {
        var validator = new AsyncRegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = "  Alice  ", Email = "alice@example.com" };

        var outcome = await validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

        outcome.IsValid.Should().BeTrue();
        outcome.Value.Should().Be(new CreatePersonCommand("Alice", "alice@example.com"));
    }

    [Fact]
    public async Task AsyncValidator_ShouldPropagateCancellation()
    {
        var validator = new CancelingAsyncPersonValidator(ValidationContextFactory);
        var dto = new PersonDto { FirstName = "Alice", Age = 42 };
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        await cancellationTokenSource.CancelAsync();

        Func<Task> act = async () => await validator.ValidateAsync(dto, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class PersonValidator : Validator<PersonDto>
    {
        private readonly AddressValidator _addressValidator;

        public PersonValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory)
        {
            _addressValidator = new AddressValidator(validationContextFactory);
        }

        protected override PersonDto PerformValidation(ValidationContext context, PersonDto value)
        {
            var firstName = context.Check(value.FirstName).NormalizeTargetIfNecessary();
            value.FirstName = firstName.Value;
            if (string.IsNullOrWhiteSpace(firstName.Value))
            {
                firstName.AddError("firstName must not be empty", "NotEmpty");
            }

            if (value.Age < 18)
            {
                context.Check(value.Age).AddError("age must be at least 18", "Adult");
            }

            if (value.Address is not null)
            {
                _addressValidator.Validate(value.Address, context.For(value.Address));
            }

            if (value.Addresses is not null)
            {
                var addressesContext = context.ForMember("addresses", isNormalized: true);
                for (var i = 0; i < value.Addresses.Count; i++)
                {
                    var childContext = addressesContext.ForIndex(i);
                    _addressValidator.Validate(value.Addresses[i], childContext);
                }
            }

            return value;
        }
    }

    private sealed class AddressValidator : Validator<AddressDto>
    {
        public AddressValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override AddressDto PerformValidation(ValidationContext context, AddressDto value)
        {
            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            value.ZipCode = zipCode.Value;
            if (string.IsNullOrWhiteSpace(zipCode.Value))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return value;
        }
    }

    private sealed class RegistrationValidator : Validator<RegistrationDto, CreatePersonCommand?>
    {
        public RegistrationValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override CreatePersonCommand? PerformValidation(ValidationContext context, RegistrationDto value)
        {
            var firstName = context.Check(value.FirstName).NormalizeTargetIfNecessary();
            var email = context.Check(value.Email).NormalizeTargetIfNecessary();
            var normalizedFirstName = firstName.Value ?? string.Empty;
            var normalizedEmail = email.Value ?? string.Empty;
            value.FirstName = normalizedFirstName;
            value.Email = normalizedEmail;

            if (string.IsNullOrWhiteSpace(normalizedFirstName))
            {
                firstName.AddError("firstName must not be empty", "NotEmpty");
            }

            if (!normalizedEmail.Contains("@", StringComparison.Ordinal))
            {
                email.AddError("email must be an email address", "Email");
            }

            return new CreatePersonCommand(normalizedFirstName, normalizedEmail);
        }
    }

    private sealed class AsyncRegistrationValidator : AsyncValidator<RegistrationDto, CreatePersonCommand?>
    {
        public AsyncRegistrationValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<CreatePersonCommand?> PerformValidationAsync(
            ValidationContext context,
            RegistrationDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new RegistrationValidator(ValidationContextFactory).Validate(value, context).Value;
        }
    }

    private sealed class CancelingAsyncPersonValidator : AsyncValidator<PersonDto>
    {
        public CancelingAsyncPersonValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<PersonDto> PerformValidationAsync(
            ValidationContext context,
            PersonDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
            return value;
        }
    }

    private sealed class PersonDto
    {
        public string? FirstName { get; set; }

        public int Age { get; set; }

        public AddressDto? Address { get; set; }

        public List<AddressDto>? Addresses { get; set; }
    }

    private sealed class AddressDto
    {
        public string? ZipCode { get; set; }
    }

    private sealed class RegistrationDto
    {
        public string? FirstName { get; set; }

        public string? Email { get; set; }
    }

    private sealed record CreatePersonCommand(string FirstName, string Email);
}
