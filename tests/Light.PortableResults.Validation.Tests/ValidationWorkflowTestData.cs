using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Light.PortableResults.Validation.Targeting;

namespace Light.PortableResults.Validation.Tests;

public static class ValidationWorkflowTestData
{
    public static readonly DefaultValidationContextFactory ValidationContextFactory = DefaultValidationContextFactory.Create();

    public static Error CreateValidationError(
        string message,
        string? code,
        string? target,
        ErrorCategory category = ErrorCategory.Validation
    ) =>
        new ()
        {
            Message = message,
            Code = code,
            Target = target,
            Category = category
        };
}

public sealed class PersonRequest
{
    public string FirstName { get; set; } = null!;

    public int Age { get; set; }

    public AddressDto PrimaryAddress { get; set; } = null!;

    public List<AddressDto> Addresses { get; set; } = new ();
}

public sealed class RegistrationRequest
{
    public string FirstName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public AddressDto Address { get; set; } = null!;
}

public sealed class AddressDto
{
    public string ZipCode { get; set; } = null!;
}

// ReSharper disable NotAccessedPositionalProperty.Global
public sealed record AddressCommand(string ZipCode);

public sealed record RegistrationCommand(string FirstName, string Email, AddressCommand Address);
// ReSharper restore NotAccessedPositionalProperty.Global

public sealed class TrimmedRequiredTextValidator : Validator<string>
{
    public TrimmedRequiredTextValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<string> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value
    )
    {
        var text = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        text.IsNotNullOrWhiteSpace(shortCircuitOnError: true);
        return checkpoint.ToValidatedValue(text.Value);
    }
}

public sealed class EmailTextValidator : Validator<string>
{
    public EmailTextValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<string> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value
    )
    {
        var email = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        email.IsNotNullOrWhiteSpace(shortCircuitOnError: true).IsEmail();
        return checkpoint.ToValidatedValue(email.Value);
    }
}

public sealed class ZipCodeValidator : Validator<string>
{
    public ZipCodeValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<string> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value
    )
    {
        var zipCode = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        zipCode.IsNotNullOrWhiteSpace(shortCircuitOnError: true).ContainsOnlyDigits();
        return checkpoint.ToValidatedValue(zipCode.Value);
    }
}

public sealed class AddressValidator : Validator<AddressDto>
{
    private readonly ZipCodeValidator _zipCodeValidator;

    public AddressValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _zipCodeValidator = new ZipCodeValidator(validationContextFactory);
    }

    protected override ValidatedValue<AddressDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        AddressDto value
    )
    {
        var zipCode = context.Check(value.ZipCode, displayName: "Zip code").ValidateChild(_zipCodeValidator);
        if (zipCode.TryGetValue(out var validatedZipCode))
        {
            value.ZipCode = validatedZipCode;
        }

        return checkpoint.ToValidatedValue(value);
    }
}

public sealed class AddressCommandValidator : Validator<AddressDto, AddressCommand>
{
    private readonly ZipCodeValidator _zipCodeValidator;

    public AddressCommandValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _zipCodeValidator = new ZipCodeValidator(validationContextFactory);
    }

    protected override ValidatedValue<AddressCommand> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        AddressDto value
    )
    {
        var zipCode = context.Check(value.ZipCode, displayName: "Zip code").ValidateChild(_zipCodeValidator);
        if (!zipCode.TryGetValue(out var validatedZipCode))
        {
            return ValidatedValue<AddressCommand>.NoValue;
        }

        return checkpoint.HasNewErrors ?
            ValidatedValue<AddressCommand>.NoValue :
            ValidatedValue.Success(new AddressCommand(validatedZipCode));
    }
}

public sealed class PersonValidator : Validator<PersonRequest>
{
    private readonly AddressValidator _addressValidator;
    private readonly TrimmedRequiredTextValidator _nameValidator;

    public PersonValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _nameValidator = new TrimmedRequiredTextValidator(validationContextFactory);
        _addressValidator = new AddressValidator(validationContextFactory);
    }

    protected override ValidatedValue<PersonRequest> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        PersonRequest value
    )
    {
        var firstName = context.Check(value.FirstName, displayName: "First name").ValidateChild(_nameValidator);
        if (firstName.TryGetValue(out var validatedFirstName))
        {
            value.FirstName = validatedFirstName;
        }

        context.Check(value.Age, displayName: "Age")
           .IsGreaterThanOrEqualTo(18, new ErrorOverrides { Code = "Adult" });

        var primaryAddress = context
           .Check(value.PrimaryAddress, displayName: "Primary address")
           .ValidateChild(_addressValidator);
        if (primaryAddress.TryGetValue(out var validatedPrimaryAddress))
        {
            value.PrimaryAddress = validatedPrimaryAddress;
        }

        var addresses = context.Check(value.Addresses, displayName: "Addresses").ValidateItems(_addressValidator);
        if (addresses.TryGetValue(out var validatedAddresses))
        {
            value.Addresses = validatedAddresses;
        }

        return checkpoint.ToValidatedValue(value);
    }
}

public sealed class RegistrationValidator : Validator<RegistrationRequest, RegistrationCommand>
{
    private readonly AddressCommandValidator _addressValidator;
    private readonly EmailTextValidator _emailValidator;
    private readonly TrimmedRequiredTextValidator _nameValidator;

    public RegistrationValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _nameValidator = new TrimmedRequiredTextValidator(validationContextFactory);
        _emailValidator = new EmailTextValidator(validationContextFactory);
        _addressValidator = new AddressCommandValidator(validationContextFactory);
    }

    protected override ValidatedValue<RegistrationCommand> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        RegistrationRequest value
    )
    {
        var firstName = context.Check(value.FirstName, displayName: "First name").ValidateChild(_nameValidator);
        var email = context.Check(value.Email, displayName: "Email").ValidateChild(_emailValidator);
        var address = context.Check(value.Address, displayName: "Address").ValidateChild(_addressValidator);

        if (!firstName.TryGetValue(out var validatedFirstName) ||
            !email.TryGetValue(out var validatedEmail) ||
            !address.TryGetValue(out var validatedAddress))
        {
            return ValidatedValue<RegistrationCommand>.NoValue;
        }

        return checkpoint.HasNewErrors ?
            ValidatedValue<RegistrationCommand>.NoValue :
            ValidatedValue.Success(new RegistrationCommand(validatedFirstName, validatedEmail, validatedAddress));
    }
}

public sealed class StringLengthValidator : Validator<string, int>
{
    public StringLengthValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<int> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value
    )
    {
        var text = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        text.IsNotNullOrWhiteSpace(shortCircuitOnError: true);
        return checkpoint.ToValidatedValue(text.Value.Length);
    }
}

public sealed class AsyncTrimmedRequiredTextValidator : AsyncValidator<string>
{
    public AsyncTrimmedRequiredTextValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override async ValueTask<ValidatedValue<string>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var text = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        text.IsNotNullOrWhiteSpace(shortCircuitOnError: true);
        return checkpoint.ToValidatedValue(text.Value);
    }
}

public sealed class AsyncEmailTextValidator : AsyncValidator<string>
{
    public AsyncEmailTextValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override async ValueTask<ValidatedValue<string>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var email = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        email.IsNotNullOrWhiteSpace(shortCircuitOnError: true).IsEmail();
        return checkpoint.ToValidatedValue(email.Value);
    }
}

public sealed class AsyncZipCodeValidator : AsyncValidator<string>
{
    public AsyncZipCodeValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override async ValueTask<ValidatedValue<string>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var zipCode = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        zipCode.IsNotNullOrWhiteSpace(shortCircuitOnError: true).ContainsOnlyDigits();
        return checkpoint.ToValidatedValue(zipCode.Value);
    }
}

public sealed class AsyncAddressValidator : AsyncValidator<AddressDto>
{
    private readonly AsyncZipCodeValidator _zipCodeValidator;

    public AsyncAddressValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _zipCodeValidator = new AsyncZipCodeValidator(validationContextFactory);
    }

    protected override async ValueTask<ValidatedValue<AddressDto>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        AddressDto value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var zipCode = await context.Check(value.ZipCode, displayName: "Zip code")
           .ValidateChildAsync(_zipCodeValidator, cancellationToken);

        if (zipCode.TryGetValue(out var validatedZipCode))
        {
            value.ZipCode = validatedZipCode;
        }

        return checkpoint.ToValidatedValue(value);
    }
}

public sealed class AsyncAddressCommandValidator : AsyncValidator<AddressDto, AddressCommand>
{
    private readonly AsyncZipCodeValidator _zipCodeValidator;

    public AsyncAddressCommandValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _zipCodeValidator = new AsyncZipCodeValidator(validationContextFactory);
    }

    protected override async ValueTask<ValidatedValue<AddressCommand>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        AddressDto value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var zipCode = await context.Check(value.ZipCode, displayName: "Zip code")
           .ValidateChildAsync(_zipCodeValidator, cancellationToken);

        if (!zipCode.TryGetValue(out var validatedZipCode))
        {
            return ValidatedValue<AddressCommand>.NoValue;
        }

        return checkpoint.HasNewErrors ?
            ValidatedValue<AddressCommand>.NoValue :
            ValidatedValue.Success(new AddressCommand(validatedZipCode));
    }
}

public sealed class AsyncPersonValidator : AsyncValidator<PersonRequest>
{
    private readonly AsyncAddressValidator _addressValidator;
    private readonly AsyncTrimmedRequiredTextValidator _nameValidator;

    public AsyncPersonValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _nameValidator = new AsyncTrimmedRequiredTextValidator(validationContextFactory);
        _addressValidator = new AsyncAddressValidator(validationContextFactory);
    }

    protected override async ValueTask<ValidatedValue<PersonRequest>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        PersonRequest value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var firstName = await context
           .Check(value.FirstName, displayName: "First name")
           .ValidateChildAsync(_nameValidator, cancellationToken);
        if (firstName.TryGetValue(out var validatedFirstName))
        {
            value.FirstName = validatedFirstName;
        }

        context.Check(value.Age, displayName: "Age")
           .IsGreaterThanOrEqualTo(18, new ErrorOverrides { Code = "Adult" });

        var primaryAddress = await context.Check(value.PrimaryAddress, displayName: "Primary address")
           .ValidateChildAsync(_addressValidator, cancellationToken);
        if (primaryAddress.TryGetValue(out var validatedPrimaryAddress))
        {
            value.PrimaryAddress = validatedPrimaryAddress;
        }

        var addresses = await context.Check(value.Addresses, displayName: "Addresses")
           .ValidateItemsAsync(_addressValidator, cancellationToken);
        if (addresses.TryGetValue(out var validatedAddresses))
        {
            value.Addresses = validatedAddresses;
        }

        return checkpoint.ToValidatedValue(value);
    }
}

public sealed class AsyncRegistrationValidator : AsyncValidator<RegistrationRequest, RegistrationCommand>
{
    private readonly AsyncAddressCommandValidator _addressValidator;
    private readonly AsyncEmailTextValidator _emailValidator;
    private readonly AsyncTrimmedRequiredTextValidator _nameValidator;

    public AsyncRegistrationValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory)
    {
        _nameValidator = new AsyncTrimmedRequiredTextValidator(validationContextFactory);
        _emailValidator = new AsyncEmailTextValidator(validationContextFactory);
        _addressValidator = new AsyncAddressCommandValidator(validationContextFactory);
    }

    protected override async ValueTask<ValidatedValue<RegistrationCommand>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        RegistrationRequest value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var firstName = await context.Check(value.FirstName, displayName: "First name")
           .ValidateChildAsync(_nameValidator, cancellationToken);
        var email = await context.Check(value.Email, displayName: "Email")
           .ValidateChildAsync(_emailValidator, cancellationToken);
        var address = await context.Check(value.Address, displayName: "Address")
           .ValidateChildAsync(_addressValidator, cancellationToken);

        if (!firstName.TryGetValue(out var validatedFirstName) ||
            !email.TryGetValue(out var validatedEmail) ||
            !address.TryGetValue(out var validatedAddress))
        {
            return ValidatedValue<RegistrationCommand>.NoValue;
        }

        return checkpoint.HasNewErrors ?
            ValidatedValue<RegistrationCommand>.NoValue :
            ValidatedValue.Success(new RegistrationCommand(validatedFirstName, validatedEmail, validatedAddress));
    }
}

public sealed class AsyncStringLengthValidator : AsyncValidator<string, int>
{
    public AsyncStringLengthValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override async ValueTask<ValidatedValue<int>> PerformValidationAsync(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        string value,
        CancellationToken cancellationToken
    )
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var text = context.Check(value, ValidationTarget.Relative(string.Empty, isNormalized: true));
        text.IsNotNullOrWhiteSpace(shortCircuitOnError: true);
        return checkpoint.ToValidatedValue(text.Value.Length);
    }
}

public enum OrderStatus
{
    // ReSharper disable once UnusedMember.Global -- required for testing
    Pending,
    Approved
}
