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

        var result = validator.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Value.Should().BeSameAs(dto);
        result.Value.FirstName.Should().Be("Alice");
        result.Value.Address!.ZipCode.Should().Be("12345");
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

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(
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

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First.Should().Be(
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

        var result = validator.Validate(dto);

        result.Should().Be(Result<CreatePersonCommand>.Ok(new CreatePersonCommand("Alice", "alice@example.com")));
    }

    [Fact]
    public void TryValidate_ShouldReturnValidatedOutputOnSuccess()
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

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.TryGetValue(out var command).Should().BeFalse();
        command.Should().BeNull();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void TryValidate_ShouldReturnFailureAndNoValidatedOutputOnError()
    {
        var validator = new RegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = " ", Email = "not-an-email" };

        var isValid = validator.TryValidate(dto, out var command, out var failure);

        isValid.Should().BeFalse();
        command.Should().BeNull();
        failure.IsValid.Should().BeFalse();
        failure.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void TransformedValidator_ShouldNotConstructOutputWhenValidationFails()
    {
        CountingCreatePersonCommand.Reset();
        var validator = new CountingRegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = " ", Email = "not-an-email" };

        var result = validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        CountingCreatePersonCommand.InstanceCount.Should().Be(0);
    }

    [Fact]
    public async Task AsyncValidator_ShouldReturnResultOnSuccess()
    {
        var validator = new AsyncRegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = "  Alice  ", Email = "alice@example.com" };

        var result = await validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

        result.Should().Be(Result<CreatePersonCommand>.Ok(new CreatePersonCommand("Alice", "alice@example.com")));
    }

    [Fact]
    public async Task AsyncValidator_ShouldReturnFailureAndNoValidatedOutputOnError()
    {
        var validator = new AsyncRegistrationValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = " ", Email = "not-an-email" };

        var result = await validator.ValidateAsync(dto, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.TryGetValue(out var command).Should().BeFalse();
        command.Should().BeNull();
        result.Errors.Should().HaveCount(2);
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

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        Func<Task> act = async () => await validator.ValidateAsync(dto, cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Validator_ShouldThrow_WhenImplementationReturnsNoValueWithoutErrors()
    {
        var validator = new BrokenValidator(ValidationContextFactory);
        var dto = new RegistrationDto { FirstName = "Alice", Email = "alice@example.com" };

        var act = () => validator.Validate(dto);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*did not produce a validated value*");
    }

    private sealed class PersonValidator : Validator<PersonDto>
    {
        private readonly AddressValidator _addressValidator;

        public PersonValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory)
        {
            _addressValidator = new AddressValidator(validationContextFactory);
        }

        protected override ValidatedValue<PersonDto> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            PersonDto value
        )
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
                context.Check(value.Address).ValidateChild(_addressValidator);
            }

            if (value.Addresses is not null)
            {
                context.Check(value.Addresses).ValidateItems(_addressValidator);
            }

            return checkpoint.ToValidatedValue(value);
        }
    }

    private sealed class AddressValidator : Validator<AddressDto>
    {
        public AddressValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<AddressDto> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            AddressDto value
        )
        {
            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            value.ZipCode = zipCode.Value;
            if (string.IsNullOrWhiteSpace(zipCode.Value))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return checkpoint.ToValidatedValue(value);
        }
    }

    private sealed class RegistrationValidator : Validator<RegistrationDto, CreatePersonCommand>
    {
        public RegistrationValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<CreatePersonCommand> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            RegistrationDto value
        )
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

            return checkpoint.HasNewErrors ?
                ValidatedValue<CreatePersonCommand>.NoValue :
                ValidatedValue.Success(new CreatePersonCommand(normalizedFirstName, normalizedEmail));
        }
    }

    private sealed class AsyncRegistrationValidator : AsyncValidator<RegistrationDto, CreatePersonCommand>
    {
        private readonly RegistrationValidator _registrationValidator;

        public AsyncRegistrationValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) =>
            _registrationValidator = new RegistrationValidator(validationContextFactory);

        protected override async ValueTask<ValidatedValue<CreatePersonCommand>> PerformValidationAsync(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            RegistrationDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            var child = context.Check(value).ValidateChild(_registrationValidator);
            return checkpoint.HasNewErrors ?
                ValidatedValue<CreatePersonCommand>.NoValue :
                ValidatedValue.Success(child.Value);
        }
    }

    private sealed class CountingRegistrationValidator : Validator<RegistrationDto, CountingCreatePersonCommand>
    {
        public CountingRegistrationValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<CountingCreatePersonCommand> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            RegistrationDto value
        )
        {
            var firstName = context.Check(value.FirstName).NormalizeTargetIfNecessary();
            var email = context.Check(value.Email).NormalizeTargetIfNecessary();
            var normalizedFirstName = firstName.Value ?? string.Empty;
            var normalizedEmail = email.Value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedFirstName))
            {
                firstName.AddError("firstName must not be empty", "NotEmpty");
            }

            if (!normalizedEmail.Contains("@", StringComparison.Ordinal))
            {
                email.AddError("email must be an email address", "Email");
            }

            return checkpoint.HasNewErrors ?
                ValidatedValue<CountingCreatePersonCommand>.NoValue :
                ValidatedValue.Success(new CountingCreatePersonCommand(normalizedFirstName, normalizedEmail));
        }
    }

    private sealed class CancelingAsyncPersonValidator : AsyncValidator<PersonDto>
    {
        public CancelingAsyncPersonValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override async ValueTask<ValidatedValue<PersonDto>> PerformValidationAsync(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            PersonDto value,
            CancellationToken cancellationToken
        )
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
            return checkpoint.ToValidatedValue(value);
        }
    }

    private sealed class BrokenValidator : Validator<RegistrationDto, CreatePersonCommand>
    {
        public BrokenValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<CreatePersonCommand> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            RegistrationDto value
        ) =>
            ValidatedValue<CreatePersonCommand>.NoValue;
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

    private sealed class CountingCreatePersonCommand
    {
        public CountingCreatePersonCommand(string firstName, string email)
        {
            FirstName = firstName;
            Email = email;
            InstanceCount++;
        }

        public static int InstanceCount { get; private set; }

        public string FirstName { get; }

        public string Email { get; }

        public static void Reset() => InstanceCount = 0;
    }
}
