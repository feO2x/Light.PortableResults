using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.PortableResults.Validation.Messaging;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class UuidV7ValidationTests
{
    // The example UUIDv7 from RFC 9562. The version nibble sits at index 14 and the variant nibble at index 19.
    private const string RfcExampleUuid = "017f22e2-79b0-7cc3-98c4-dc0c0c07398f";
    private const int VersionNibbleIndex = 14;
    private const int VariantNibbleIndex = 19;
    private const string HexDigits = "0123456789abcdef";

    private static readonly Guid AcceptedUuidV7 = Guid.Parse(RfcExampleUuid);
    private static readonly Guid RejectedUuid = Guid.Parse("017f22e2-79b0-4cc3-98c4-dc0c0c07398f");

    // The BCL computes Version as (_c >> 12) & 0xF and Variant as _d >> 4, which are the same two expressions
    // the predicate evaluates over its own field overlay. This oracle therefore pins the overlay's field
    // offsets — the load-bearing assumption — but cannot pin the masks, because a wrong mask would have to be
    // wrong in the BCL too. The masks are pinned by the RFC-derived accepted set in the test below.
    [Fact]
    public void IsUuidV7_ShouldAgreeWithVersionAndVariantOracle_AcrossTheFullNibbleMatrix()
    {
        for (var versionNibble = 0; versionNibble < 16; versionNibble++)
        {
            for (var variantNibble = 0; variantNibble < 16; variantNibble++)
            {
                var guid = CreateGuidWithNibbles(versionNibble, variantNibble);

                guid.IsUuidV7()
                   .Should()
                   .Be(
                        guid.Version == 7 && (guid.Variant & 0xC) == 0x8,
                        "the version nibble is {0:x} and the variant nibble is {1:x}",
                        versionNibble,
                        variantNibble
                    );
            }
        }
    }

    // Not redundant with the matrix above: the expected set is read off RFC 9562 rather than derived from
    // Guid.Version and Guid.Variant, so this is the only test that constrains the version and variant masks
    // independently of the BCL. Keep it even though both tests walk the same 256 inputs.
    [Fact]
    public void IsUuidV7_ShouldAcceptOnlyVersion7CrossedWithTheRfcVariantNibbles()
    {
        var acceptedNibbles = new List<(int VersionNibble, int VariantNibble)>();

        for (var versionNibble = 0; versionNibble < 16; versionNibble++)
        {
            for (var variantNibble = 0; variantNibble < 16; variantNibble++)
            {
                if (CreateGuidWithNibbles(versionNibble, variantNibble).IsUuidV7())
                {
                    acceptedNibbles.Add((versionNibble, variantNibble));
                }
            }
        }

        acceptedNibbles.Should().BeEquivalentTo(
            [(7, 0x8), (7, 0x9), (7, 0xA), (7, 0xB)]
        );
    }

    [Fact]
    public void IsUuidV7_ShouldRejectEmptyGuid() => Guid.Empty.IsUuidV7().Should().BeFalse();

    [Fact]
    public void IsUuidV7_ShouldRejectNewGuid() => Guid.NewGuid().IsUuidV7().Should().BeFalse();

    [Fact]
    public void IsUuidV7_ShouldAcceptCreateVersion7() => Guid.CreateVersion7().IsUuidV7().Should().BeTrue();

    [Fact]
    public void IsUuidV7_ShouldAddError_WhenGuidIsNotAVersion7Uuid()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(RejectedUuid, target: "orderId", displayName: "Order ID").IsUuidV7();

        context.Errors.Should().ContainSingle(
            error =>
                error.Target == "orderId" &&
                error.Code == "UuidV7" &&
                error.Message == "Order ID must be a version 7 UUID"
        );
    }

    [Fact]
    public void IsUuidV7_ShouldNotAddError_WhenGuidIsAVersion7Uuid()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(AcceptedUuidV7, target: "orderId", displayName: "Order ID").IsUuidV7();

        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsUuidV7_ShouldAddError_WhenOverridesAreUsed()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(RejectedUuid, target: "orderId", displayName: "Order ID")
           .IsUuidV7(new ErrorOverrides { Code = "OrderIdNotSortable" });

        context.Errors.Should().ContainSingle(
            error => error.Target == "orderId" && error.Code == "OrderIdNotSortable"
        );
    }

    [Fact]
    public void IsUuidV7_ShouldNotAddError_WhenOverridesAreUsedAndGuidIsAVersion7Uuid()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(AcceptedUuidV7, target: "orderId", displayName: "Order ID")
           .IsUuidV7(new ErrorOverrides { Code = "Unused" });

        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsUuidV7_ShouldThrow_WhenOverridesAreEmpty()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(AcceptedUuidV7, target: "orderId");

        var act = () => check.IsUuidV7(new ErrorOverrides());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsUuidV7_ShouldRespectShortCircuit()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(RejectedUuid, target: "orderId").ShortCircuit();

        check.IsUuidV7().IsShortCircuited.Should().BeTrue();
        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsUuidV7_ShouldRespectShortCircuit_WhenOverridesAreUsed()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(RejectedUuid, target: "orderId").ShortCircuit();

        check.IsUuidV7(new ErrorOverrides { Code = "Unused" }).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsUuidV7_ShouldShortCircuit_WhenRequested()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(RejectedUuid, target: "orderId", displayName: "Order ID");

        check.IsUuidV7(shortCircuitOnError: true).IsShortCircuited.Should().BeTrue();
        context.Errors.Should().ContainSingle(error => error.Target == "orderId" && error.Code == "UuidV7");
    }

    [Fact]
    public void IsUuidV7_ShouldShortCircuit_WhenOverridesAreUsedAndRequested()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(RejectedUuid, target: "orderId", displayName: "Order ID");

        check
           .IsUuidV7(new ErrorOverrides { Message = "Order ID must be sortable" }, shortCircuitOnError: true)
           .IsShortCircuited.Should()
           .BeTrue();
        context.Errors.Should().ContainSingle(
            error => error.Target == "orderId" && error.Message == "Order ID must be sortable"
        );
    }

    [Fact]
    public void IsUuidV7_ShouldNotShortCircuit_WhenNotRequested()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
        var check = context.Check(RejectedUuid, target: "orderId", displayName: "Order ID");

        check.IsUuidV7().IsShortCircuited.Should().BeFalse();
    }

    [Fact]
    public void IsUuidV7_ShouldUseTheCustomizedTemplate()
    {
        var context = CreateContextWithTemplates(
            ValidationErrorTemplates.Default with
            {
                UuidV7 = new ValidationErrorTemplates.Constant("Only time-ordered identifiers are accepted")
            }
        );

        context.Check(RejectedUuid, target: "orderId", displayName: "Order ID").IsUuidV7();

        context.Errors.Should().ContainSingle(
            error =>
                error.Code == "UuidV7" &&
                error.Message == "Only time-ordered identifiers are accepted"
        );
    }

    [Fact]
    public void IsUuidV7_ShouldKeepTheCustomizedTemplate_WhenTemplatesAreCopiedAgain()
    {
        var customizedTemplates = ValidationErrorTemplates.Default with
        {
            UuidV7 = new ValidationErrorTemplates.Constant("Only time-ordered identifiers are accepted")
        };
        var context = CreateContextWithTemplates(
            customizedTemplates with { NotNull = new ValidationErrorTemplates.Constant("Value is required") }
        );

        context.Check(RejectedUuid, target: "orderId", displayName: "Order ID").IsUuidV7();

        context.Errors.Should().ContainSingle(
            error =>
                error.Code == "UuidV7" &&
                error.Message == "Only time-ordered identifiers are accepted"
        );
    }

    private static ValidationContext CreateContextWithTemplates(ValidationErrorTemplates templates)
    {
        var options = new ValidationContextOptions() with { ErrorTemplates = templates };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private static Guid CreateGuidWithNibbles(int versionNibble, int variantNibble)
    {
        var characters = RfcExampleUuid.ToCharArray();
        characters[VersionNibbleIndex] = HexDigits[versionNibble];
        characters[VariantNibbleIndex] = HexDigits[variantNibble];
        return Guid.Parse(new string(characters));
    }
}
