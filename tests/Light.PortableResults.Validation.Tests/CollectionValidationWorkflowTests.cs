using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Validation.Normalization;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class CollectionValidationWorkflowTests
{
    [Fact]
    public void ValidateChild_ShouldComposeTargetsAcrossNestedScopes()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        var result = context
           .Check(new AddressDto { ZipCode = " " }, target: "request.PrimaryAddress", displayName: "Primary address")
           .ValidateChild(new AddressValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<AddressDto>.NoValue);
        context.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "primaryAddress.zipCode must not be empty or whitespace",
                    "NotNullOrWhiteSpace",
                    "primaryAddress.zipCode"
                )
            )
        );
    }

    [Fact]
    public void ValidateChild_ShouldTreatExplicitAbsoluteTargetsAsAlreadyComposed()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        var check = addressContext
           .Check(
                new AddressDto { ZipCode = " " },
                ValidationTarget.Absolute("address", isNormalized: true),
                displayName: "Address"
            )
           .NormalizeTargetIfNecessary();

        var result = check.ValidateChild(new AddressValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<AddressDto>.NoValue);
        context.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "address.zipCode must not be empty or whitespace",
                    "NotNullOrWhiteSpace",
                    "address.zipCode"
                )
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldValidateMutableCollectionsWithValidators()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var addresses = new List<AddressDto>
        {
            new () { ZipCode = " " },
            new () { ZipCode = " 12345 " }
        };

        var result = context
           .Check(addresses, target: "request.Addresses", displayName: "Addresses")
           .ValidateItems(new AddressValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<List<AddressDto>>.NoValue);
        addresses[1].ZipCode.Should().Be("12345");
        context.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "addresses[0].zipCode must not be empty or whitespace",
                    "NotNullOrWhiteSpace",
                    "addresses[0].zipCode"
                )
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldValidateImmutableArraysWithValidators()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = context
           .Check(
                ImmutableArray.Create(new AddressDto { ZipCode = "12A" }),
                target: "request.Addresses",
                displayName: "Addresses"
            )
           .ValidateItems(new AddressValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<ImmutableArray<AddressDto>>.NoValue);
        context.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "addresses[0].zipCode must contain only digits",
                    "DigitsOnly",
                    "addresses[0].zipCode"
                )
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldSupportDelegateBasedItemValidation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        var result = context
           .Check(new List<int> { 1, -2 }, target: "request.Quantities", displayName: "Quantities")
           .ValidateItems(
                (Action<Check<int>>) (static itemCheck =>
                {
                    if (itemCheck.Value < 0)
                    {
                        itemCheck.AddError("Quantity must be zero or greater", "NonNegative");
                    }
                })
            );

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
        context.Errors.Should().Equal(
            new Errors(
                ValidationWorkflowTestData.CreateValidationError(
                    "Quantity must be zero or greater",
                    "NonNegative",
                    "quantities[1]"
                )
            )
        );
    }

    [Fact]
    public void ValidateItems_ShouldSupportDelegateBasedNormalization()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var tags = new List<string> { " Alice ", " Bob " };

        var result = context
           .Check(tags, target: "request.Tags", displayName: "Tags")
           .ValidateItems(
                (Func<Check<string>, ValidatedValue<string>>) (
                    static itemCheck => ValidatedValue.Success(itemCheck.Value.Trim())
                )
            );

        result.TryGetValue(out var validatedTags).Should().BeTrue();
        validatedTags.Should().BeSameAs(tags);
        tags.Should().Equal("Alice", "Bob");
    }

    [Fact]
    public void ValidateItems_ShouldTransformArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = context
           .Check(new[] { " Alice ", "Bob " }, target: "request.Tags")
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.TryGetValue(out var validatedArray).Should().BeTrue();
        validatedArray.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldTransformLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = context
           .Check(new List<string> { " Alice ", "Bob " }, target: "request.ListTags")
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.TryGetValue(out var validatedList).Should().BeTrue();
        validatedList.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldTransformImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = context
           .Check(
                ImmutableArray.Create(" Alice ", "Bob "),
                target: "request.ImmutableTags"
            )
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.TryGetValue(out var validatedImmutable).Should().BeTrue();
        validatedImmutable.Should().Equal(5, 3);
    }

    [Fact]
    public void ValidateItems_ShouldHandleTransformingValidatorFailures_WhenUsingArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };

        var result = context
           .Check(arrayTags, target: "request.ArrayTags", displayName: "Array tags")
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<int[]>.NoValue);
        context.Errors.Should().Contain(error => error.Target == "arrayTags[0]" && error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void ValidateItems_ShouldHandleTransformingValidatorFailures_WhenUsingLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };

        var result = context
           .Check(listTags, target: "request.ListTags", displayName: "List tags")
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
        context.Errors.Should().Contain(error => error.Target == "listTags[0]" && error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public void ValidateItems_ShouldHandleTransformingValidatorFailures_WhenUsingImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = context
           .Check(immutableTags, target: "request.ImmutableTags", displayName: "Immutable tags")
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<ImmutableArray<int>>.NoValue);
        context.Errors.Should().Contain(
            error => error.Target == "immutableTags[0]" && error.Code == "NotNullOrWhiteSpace"
        );
    }

    [Fact]
    public void ValidateItems_ShouldShortCircuit_WhenUsingArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };

        var result = context
           .Check(arrayTags, target: "request.ShortCircuitedArrayTags")
           .ShortCircuit()
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<int[]>.NoValue);
    }

    [Fact]
    public void ValidateItems_ShouldShortCircuit_WhenUsingLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };

        var result = context
           .Check(listTags, target: "request.ShortCircuitedListTags")
           .ShortCircuit()
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
    }

    [Fact]
    public void ValidateItems_ShouldShortCircuit_WhenUsingImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = context
           .Check(immutableTags, target: "request.ShortCircuitedImmutableTags")
           .ShortCircuit()
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        result.Should().Be(ValidatedValue<ImmutableArray<int>>.NoValue);
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenArrayValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };
        Validator<string, int> nullValidator = null!;

        Action act = () => context
           .Check(arrayTags, target: "request.NullValidatorArrayTags")
           .ValidateItems(nullValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenListValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };
        Validator<string, int> nullValidator = null!;

        Action act = () => context
           .Check(listTags, target: "request.NullValidatorListTags")
           .ValidateItems(nullValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenImmutableArrayValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");
        Validator<string, int> nullValidator = null!;

        Action act = () => context
           .Check(immutableTags, target: "request.NullValidatorImmutableTags")
           .ValidateItems(nullValidator);

        act.Should().Throw<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenArrayIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        string[] nullableArrayTags = null!;

        Action act = () => context
           .Check(
                nullableArrayTags,
                NoOpValueNormalizer.Instance,
                target: "request.NullableArrayTags",
                displayName: "Nullable array tags"
            )
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenListIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        List<string> nullableListTags = null!;

        Action act = () => context
           .Check(
                nullableListTags,
                NoOpValueNormalizer.Instance,
                target: "request.NullableListTags",
                displayName: "Nullable list tags"
            )
           .ValidateItems(new StringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateItems_ShouldRespectShortCircuit_WhenCollectionIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> nullableTags = null!;

        var result = context
           .Check(nullableTags, NoOpValueNormalizer.Instance, target: "request.Tags", displayName: "Tags")
           .ShortCircuit()
           .ValidateItems((Action<Check<string>>) (static itemCheck => itemCheck.AddError("not reached", "Never")));

        result.Should().Be(ValidatedValue<IReadOnlyList<string>>.NoValue);
    }

    [Fact]
    public void ValidateItems_ShouldThrow_WhenCollectionIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> nullableTags = null!;

        Action act = () => context
           .Check(nullableTags, NoOpValueNormalizer.Instance, target: "request.Tags", displayName: "Tags")
           .ValidateItems((Action<Check<string>>) (static itemCheck => itemCheck.AddError("not reached", "Never")));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportValidatorOverload()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = await context
           .Check(new List<AddressDto> { new () { ZipCode = " 12345 " } }, target: "request.Addresses")
           .ValidateItemsAsync(
                new AsyncAddressValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedAddresses).Should().BeTrue();
        validatedAddresses.Should().NotBeNull();
        validatedAddresses![0].ZipCode.Should().Be("12345");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportDelegateOverload()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = await context
           .Check(new List<int> { 1, -2 }, target: "request.Quantities")
           .ValidateItemsAsync<List<int>, int>(
                static async (itemCheck, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    if (itemCheck.Value < 0)
                    {
                        itemCheck.AddError("Quantity must be zero or greater", "NonNegative");
                    }
                },
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
        context.Errors.Should().ContainSingle(
            error => error.Target == "quantities[1]" && error.Code == "NonNegative"
        );
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportNormalizationDelegate()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var normalizationSource = new List<int> { 1, 2 };
        var result = await context
           .Check(normalizationSource, target: "request.MutableQuantities")
           .ValidateItemsAsync<List<int>, int>(
                static async (itemCheck, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    return ValidatedValue.Success(itemCheck.Value * 2);
                },
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var normalizedQuantities).Should().BeTrue();
        normalizedQuantities.Should().BeSameAs(normalizationSource);
        normalizationSource.Should().Equal(2, 4);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportTransformingValidator()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var result = await context
           .Check(new[] { " Alice ", "Bob " }, target: "request.Tags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedLengths).Should().BeTrue();
        validatedLengths.Should().Equal(5, 3);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldPropagateCancellation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken
        );
        await cancellationTokenSource.CancelAsync();

#pragma warning disable xUnit1051
        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await context
           .Check(new List<int> { 1, 2 }, target: "request.Quantities")
           .ValidateItemsAsync<List<int>, int>(
                static async (itemCheck, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    _ = itemCheck.Value;
                },
                cancellationTokenSource.Token
            );
#pragma warning restore xUnit1051

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportReadOnlyLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<AddressDto> readOnlyAddresses = new List<AddressDto> { new () { ZipCode = "12345" } };

        var result = await context
           .Check(readOnlyAddresses, target: "request.ReadOnlyAddresses")
           .ValidateItemsAsync(
                new AsyncAddressValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedReadOnly).Should().BeTrue();
        validatedReadOnly.Should().NotBeNull();
        validatedReadOnly!.Count.Should().Be(1);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableAddresses = ImmutableArray.Create(new AddressDto { ZipCode = "54321" });

        var result = await context
           .Check(immutableAddresses, target: "request.ImmutableAddresses")
           .ValidateItemsAsync(
                new AsyncAddressValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedImmutableAddresses).Should().BeTrue();
        validatedImmutableAddresses.Length.Should().Be(1);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldTransformLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        var result = await context
           .Check(new List<string> { " Alice ", "Bob " }, target: "request.ListTags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedListLengths).Should().BeTrue();
        validatedListLengths.Should().Equal(5, 3);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldTransformImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        var result = await context
           .Check(ImmutableArray.Create(" Alice ", "Bob "), target: "request.ImmutableTags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.TryGetValue(out var validatedImmutableLengths).Should().BeTrue();
        validatedImmutableLengths.Should().Equal(5, 3);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldSupportReadOnlyDelegates()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<int> quantities = new List<int> { 1, 2 };

        var result = await context
           .Check(quantities, target: "request.Quantities", displayName: "Quantities")
           .ValidateItemsAsync<IReadOnlyList<int>, int>(
                static async (itemCheck, cancellationToken) =>
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    if (itemCheck.DisplayName!.EndsWith("[1]", StringComparison.Ordinal))
                    {
                        itemCheck.AddError("second item invalid", "SecondInvalid");
                    }
                },
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<IReadOnlyList<int>>.NoValue);
        context.Errors.Should()
           .ContainSingle(error => error.Target == "quantities[1]" && error.Code == "SecondInvalid");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldRespectShortCircuit()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        List<int> mutableQuantities = [1, 2];

        var result = await context
           .Check(mutableQuantities, target: "request.MutableQuantities")
           .ShortCircuit()
           .ValidateItemsAsync<List<int>, int>(
                static (itemCheck, cancellationToken) =>
                {
                    _ = itemCheck.Value;
                    cancellationToken.ThrowIfCancellationRequested();
                    return ValueTask.FromResult(ValidatedValue.Success(0));
                },
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        AsyncValidator<string> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(new List<string> { "A" })
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenDelegateIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<int> quantities = new List<int> { 1, 2 };

        Func<Task> act = async () => await context
           .Check(quantities)
           .ValidateItemsAsync<IReadOnlyList<int>, int>(
                null!,
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("validateItem");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldHandleFailures_WhenUsingReadOnlyLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> readOnlyTags = new List<string> { " ", " Bob " };

        var result = await context
           .Check(readOnlyTags, target: "request.Tags", displayName: "Tags")
           .ValidateItemsAsync(
                new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<IReadOnlyList<string>>.NoValue);
        context.Errors.Should().Contain(error => error.Target == "tags[0]" && error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldHandleFailures_WhenUsingImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = await context
           .Check(immutableTags, target: "request.ImmutableTags", displayName: "Immutable tags")
           .ValidateItemsAsync(
                new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<ImmutableArray<string>>.NoValue);
        context.Errors.Should().Contain(
            error => error.Target == "immutableTags[0]" && error.Code == "NotNullOrWhiteSpace"
        );
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldShortCircuit_WhenUsingReadOnlyLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> readOnlyTags = new List<string> { " ", " Bob " };

        var result = await context
           .Check(readOnlyTags, target: "request.ShortCircuitedTags")
           .ShortCircuit()
           .ValidateItemsAsync(
                new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<IReadOnlyList<string>>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldShortCircuit_WhenUsingImmutableArraysForReadOnlyValidation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = await context
           .Check(immutableTags, target: "request.ShortCircuitedImmutableTags")
           .ShortCircuit()
           .ValidateItemsAsync(
                new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<ImmutableArray<string>>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenReadOnlyListValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> readOnlyTags = new List<string> { " ", " Bob " };
        AsyncValidator<string> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(readOnlyTags, target: "request.NullValidatorTags")
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenImmutableArrayValidatorIsNullForReadOnlyValidation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");
        AsyncValidator<string> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(immutableTags, target: "request.NullValidatorImmutableTags")
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenReadOnlyListIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        IReadOnlyList<string> nullableReadOnlyTags = null!;

        Func<Task> act = async () => await context
           .Check(
                nullableReadOnlyTags,
                NoOpValueNormalizer.Instance,
                target: "request.NullableTags",
                displayName: "Nullable tags"
            )
           .ValidateItemsAsync(
                new AsyncTrimmedRequiredTextValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldHandleTransformingValidatorFailures_WhenUsingArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };

        var result = await context
           .Check(arrayTags, target: "request.ArrayTags", displayName: "Array tags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<int[]>.NoValue);
        context.Errors.Should().Contain(error => error.Target == "arrayTags[0]" && error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldHandleTransformingValidatorFailures_WhenUsingLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };

        var result = await context
           .Check(listTags, target: "request.ListTags", displayName: "List tags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
        context.Errors.Should().Contain(error => error.Target == "listTags[0]" && error.Code == "NotNullOrWhiteSpace");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldHandleTransformingValidatorFailures_WhenUsingImmutableArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = await context
           .Check(immutableTags, target: "request.ImmutableTags", displayName: "Immutable tags")
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<ImmutableArray<int>>.NoValue);
        context.Errors.Should().Contain(
            error => error.Target == "immutableTags[0]" && error.Code == "NotNullOrWhiteSpace"
        );
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldShortCircuit_WhenUsingArrays()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };

        var result = await context
           .Check(arrayTags, target: "request.ShortCircuitedArrayTags")
           .ShortCircuit()
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<int[]>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldShortCircuit_WhenUsingLists()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };

        var result = await context
           .Check(listTags, target: "request.ShortCircuitedListTags")
           .ShortCircuit()
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<List<int>>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldShortCircuit_WhenUsingImmutableArraysForTransformingValidation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");

        var result = await context
           .Check(immutableTags, target: "request.ShortCircuitedImmutableTags")
           .ShortCircuit()
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        result.Should().Be(ValidatedValue<ImmutableArray<int>>.NoValue);
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenArrayValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var arrayTags = new[] { " ", " Bob " };
        AsyncValidator<string, int> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(arrayTags, target: "request.NullValidatorArrayTags")
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenListValidatorIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var listTags = new List<string> { " ", " Bob " };
        AsyncValidator<string, int> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(listTags, target: "request.NullValidatorListTags")
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenImmutableArrayValidatorIsNullForTransformingValidation()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var immutableTags = ImmutableArray.Create(" ", " Bob ");
        AsyncValidator<string, int> nullValidator = null!;

        Func<Task> act = async () => await context
           .Check(immutableTags, target: "request.NullValidatorImmutableTags")
           .ValidateItemsAsync(nullValidator, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("itemValidator");
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenArrayIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        string[] nullableArrayTags = null!;

        Func<Task> act = async () => await context
           .Check(
                nullableArrayTags,
                NoOpValueNormalizer.Instance,
                target: "request.NullableArrayTags",
                displayName: "Nullable array tags"
            )
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ValidateItemsAsync_ShouldThrow_WhenListIsNull()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        List<string> nullableListTags = null!;

        Func<Task> act = async () => await context
           .Check(
                nullableListTags,
                NoOpValueNormalizer.Instance,
                target: "request.NullableListTags",
                displayName: "Nullable list tags"
            )
           .ValidateItemsAsync(
                new AsyncStringLengthValidator(ValidationWorkflowTestData.ValidationContextFactory),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
