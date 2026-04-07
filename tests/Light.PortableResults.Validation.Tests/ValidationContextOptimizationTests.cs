using System;
using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using Light.PortableResults.Validation.Targeting;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class ValidationContextOptimizationTests
{
    private static readonly DefaultValidationContextFactory ValidationContextFactory = new ();

    private static readonly FieldInfo ManyErrorsField = typeof(Errors).GetField(
        "_manyErrors",
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    [Fact]
    public void ScopeApis_ShouldComposeRootMemberAndIndexTargets()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var container = new AddressContainer { Address = new AddressDto() };

        var addressContext = context.For(container.Address);
        var addressesContext = context.ForMember("addresses", isNormalized: true);
        var indexedContext = addressesContext.ForIndex(0);
        var zipCodeContext = indexedContext.ForMember("zipCode", isNormalized: true);

        addressContext.TargetPrefix.Should().Be("address");
        indexedContext.TargetPrefix.Should().Be("addresses[0]");
        zipCodeContext.TargetPrefix.Should().Be("addresses[0].zipCode");
    }

    [Fact]
    public void ErrorsFromMultipleScopes_ShouldAccumulateInFlatOrder()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var addressContext = context.ForMember("address", isNormalized: true);
        var addressesContext = context.ForMember("addresses", isNormalized: true);

        context.AddError("firstName must not be empty", "NotEmpty", ValidationTarget.Relative("firstName", true));
        addressContext.AddError("zipCode must not be empty", "NotEmpty", ValidationTarget.Relative("zipCode", true));
        addressesContext
           .ForIndex(0)
           .AddError("zipCode must not be empty", "NotEmpty", ValidationTarget.Relative("zipCode", true));

        context.Errors.Should().Equal(
            new Errors(
                new[]
                {
                    CreateValidationError("firstName must not be empty", "NotEmpty", "firstName"),
                    CreateValidationError("zipCode must not be empty", "NotEmpty", "address.zipCode"),
                    CreateValidationError("zipCode must not be empty", "NotEmpty", "addresses[0].zipCode")
                }
            )
        );
    }

    [Fact]
    public void ToErrors_ShouldMaterializeSingleErrorInline()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("firstName must not be empty", "NotEmpty", ValidationTarget.Relative("firstName", true));

        var errors = context.Errors;
        var manyErrors = GetManyErrorsMemory(errors);

        errors.Should().Equal(
            new Errors(CreateValidationError("firstName must not be empty", "NotEmpty", "firstName"))
        );
        manyErrors.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ToErrors_ShouldWrapOwnedArrayWithoutCopyForTwoErrors()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        context.AddError("firstName must not be empty", "NotEmpty", ValidationTarget.Relative("firstName", true));
        context.AddError("age must be at least 18", "Adult", ValidationTarget.Relative("age", true));

        var errors = context.Errors;
        var segment = GetBackingSegment(errors);

        errors.Should().Equal(
            new Errors(
                new[]
                {
                    CreateValidationError("firstName must not be empty", "NotEmpty", "firstName"),
                    CreateValidationError("age must be at least 18", "Adult", "age")
                }
            )
        );
        segment.Array.Should().NotBeNull();
        segment.Array!.Length.Should().Be(10);
        segment.Offset.Should().Be(0);
        segment.Count.Should().Be(2);
    }

    [Fact]
    public void ToErrors_ShouldPreserveOrderAndGrowBufferAfterTenErrors()
    {
        var context = ValidationContextFactory.CreateValidationContext();
        var expectedErrors = new Error[11];
        for (var i = 0; i < expectedErrors.Length; i++)
        {
            var error = CreateValidationError($"message {i}", $"Code{i}", $"items[{i}]");
            expectedErrors[i] = error;
            context.AddError(error);
        }

        var errors = context.Errors;
        var segment = GetBackingSegment(errors);

        errors.Should().Equal(new Errors(expectedErrors));
        segment.Array.Should().NotBeNull();
        segment.Array!.Length.Should().Be(20);
        segment.Offset.Should().Be(0);
        segment.Count.Should().Be(11);
    }

    private static Error CreateValidationError(string message, string code, string target) =>
        new ()
        {
            Message = message,
            Code = code,
            Target = target,
            Category = ErrorCategory.Validation
        };

    private static ReadOnlyMemory<Error> GetManyErrorsMemory(Errors errors) =>
        (ReadOnlyMemory<Error>) ManyErrorsField.GetValue(errors)!;

    private static ArraySegment<Error> GetBackingSegment(Errors errors)
    {
        var manyErrors = GetManyErrorsMemory(errors);
        MemoryMarshal.TryGetArray(manyErrors, out var segment).Should().BeTrue();
        return segment;
    }

    private sealed class AddressContainer
    {
        public AddressDto? Address { get; init; }
    }

    private sealed class AddressDto;
}
