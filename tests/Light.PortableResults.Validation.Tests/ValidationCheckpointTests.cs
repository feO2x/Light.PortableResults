using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationCheckpointTests
{
    private static readonly DefaultValidationContextFactory ValidationContextFactory = new ();

    [Fact]
    public void TryGetNewErrors_ShouldOnlyExposeErrorsAddedAfterCheckpoint()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var checkpoint = context.CreateCheckpoint();

        context.AddError("first new", "FirstNew", ValidationTarget.Relative("first", isNormalized: true));
        context.AddError("second new", "SecondNew", ValidationTarget.Relative("second", isNormalized: true));

        checkpoint.HasNewErrors.Should().BeTrue();
        checkpoint.NewErrorCount.Should().Be(2);
        checkpoint.TryGetNewErrors(out var errors).Should().BeTrue();
        errors.Should().Equal(
            new Errors(
                new[]
                {
                    new Error
                    {
                        Message = "first new",
                        Code = "FirstNew",
                        Target = "first",
                        Category = ErrorCategory.Validation
                    },
                    new Error
                    {
                        Message = "second new",
                        Code = "SecondNew",
                        Target = "second",
                        Category = ErrorCategory.Validation
                    }
                }
            )
        );
    }

    [Fact]
    public void ValidateChildValue_ShouldReturnSuccessWithoutNewErrors_WhenContextAlreadyContainsEarlierFailures()
    {
        var validator = new TrimmedStringValidator(ValidationContextFactory);
        var context = ValidationContextFactory.CreateValidationContext();
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
    public void Validate_ShouldStillReturnFailure_WhenContextAlreadyContainsEarlierFailures()
    {
        var validator = new TrimmedStringValidator(ValidationContextFactory);
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));

        var result = validator.Validate("  Alice  ", context, ValidationTarget.Relative("name", isNormalized: true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Target.Should().Be("existing");
    }

    [Fact]
    public void ValidateChild_ShouldReturnSuccessForValidChild_WhenContextAlreadyContainsEarlierFailures()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));
        var child = new AddressDto { ZipCode = " 12345 " };

        var result = context
           .Check(child, target: "request.Address", displayName: "Address")
           .ValidateChild(new AddressCommandValidator(ValidationContextFactory));

        result.Should().Be(ValidatedValue.Success(new AddressCommand("12345")));
    }

    [Fact]
    public void ValidateItems_ShouldReturnSuccessForValidCollection_WhenContextAlreadyContainsEarlierFailures()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));
        var check = context.Check(new List<string> { " Alice ", "Bob " }, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(new StringLengthValidator(ValidationContextFactory));

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldStillNormalizeLaterItems_WhenEarlierItemsFailed()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var values = new List<string> { " ", " Alice " };
        var check = context.Check(values, target: "request.Tags", displayName: "Tags");

        var result = check.ValidateItems(
            (Func<Check<string>, ValidatedValue<string>>) (static itemCheck =>
            {
                var normalized = itemCheck.Value.Trim();
                if (normalized.Length == 0)
                {
                    itemCheck.AddError("tag must not be empty", "NotEmpty");
                    return ValidatedValue<string>.NoValue;
                }

                return ValidatedValue.Success(normalized);
            })
        );

        result.Should().Be(ValidatedValue<List<string>>.NoValue);
        values.Should().Equal(" ", "Alice");
    }

    [Fact]
    public async Task
        ValidateChildValueAsync_ShouldReturnSuccessWithoutNewErrors_WhenContextAlreadyContainsEarlierFailures()
    {
        var validator = new AsyncTrimmedStringValidator(ValidationContextFactory);
        var context = ValidationContextFactory.CreateValidationContext();
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
    public async Task
        ValidateItemsAsync_ShouldReturnSuccessForValidCollection_WhenContextAlreadyContainsEarlierFailures()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("existing", "Existing", ValidationTarget.Relative("existing", isNormalized: true));
        var check = context.Check(new List<string> { " Alice ", "Bob " }, target: "request.Tags", displayName: "Tags");

        var result = await check.ValidateItemsAsync(
            new AsyncStringLengthValidator(ValidationContextFactory),
            TestContext.Current.CancellationToken
        );

        result.TryGetValue(out var lengths).Should().BeTrue();
        lengths.Should().Equal(5, 3);
    }

    private sealed class TrimmedStringValidator : Validator<string>
    {
        public TrimmedStringValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<string> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            string value
        )
        {
            var check = context.Check(value).NormalizeTargetIfNecessary();
            return checkpoint.ToValidatedValue(check.Value);
        }
    }

    private sealed class AsyncTrimmedStringValidator : AsyncValidator<string>
    {
        public AsyncTrimmedStringValidator(IValidationContextFactory validationContextFactory)
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
            var check = context.Check(value).NormalizeTargetIfNecessary();
            return checkpoint.ToValidatedValue(check.Value);
        }
    }

    private sealed class StringLengthValidator : Validator<string, int>
    {
        public StringLengthValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<int> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            string value
        )
        {
            var check = context.Check(value).NormalizeTargetIfNecessary();
            if (check.Value.Length == 0)
            {
                check.AddError("tag must not be empty", "NotEmpty");
            }

            return checkpoint.ToValidatedValue(check.Value.Length);
        }
    }

    private sealed class AsyncStringLengthValidator : AsyncValidator<string, int>
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
            var check = context.Check(value).NormalizeTargetIfNecessary();
            if (check.Value.Length == 0)
            {
                check.AddError("tag must not be empty", "NotEmpty");
            }

            return checkpoint.ToValidatedValue(check.Value.Length);
        }
    }

    private sealed class AddressCommandValidator : Validator<AddressDto, AddressCommand>
    {
        public AddressCommandValidator(IValidationContextFactory validationContextFactory)
            : base(validationContextFactory) { }

        protected override ValidatedValue<AddressCommand> PerformValidation(
            ValidationContext context,
            ValidationCheckpoint checkpoint,
            AddressDto value
        )
        {
            var zipCode = context.Check(value.ZipCode).NormalizeTargetIfNecessary();
            var normalizedZipCode = zipCode.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedZipCode))
            {
                zipCode.AddError("zipCode must not be empty", "NotEmpty");
            }

            return checkpoint.HasNewErrors ?
                ValidatedValue<AddressCommand>.NoValue :
                ValidatedValue.Success(new AddressCommand(normalizedZipCode));
        }
    }

    private sealed class AddressDto
    {
        public string? ZipCode { get; set; }
    }

    private sealed record AddressCommand(string ZipCode);
}
