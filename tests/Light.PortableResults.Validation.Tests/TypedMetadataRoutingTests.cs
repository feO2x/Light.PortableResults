using System;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.Validation.Definitions;
using Light.PortableResults.Validation.Messaging;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class TypedMetadataRoutingTests
{
    [Fact]
    public void BuiltInDefinitionsShouldRouteTheTypedMetadataVocabulary()
    {
        AssertKind(ulong.MaxValue, MetadataKind.UInt64);
        AssertKind(0.1f, MetadataKind.Single);
        AssertKind('x', MetadataKind.Char);
        AssertKind(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc), MetadataKind.DateTime);
        // DateTimeKind.Unspecified is what every `new DateTime(...)` literal produces, so it is the common case
        // for a validation boundary rather than an edge case.
        AssertKind(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified), MetadataKind.DateTime);
        AssertKind(new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Local), MetadataKind.DateTime);
        AssertKind(
            new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2)),
            MetadataKind.DateTimeOffset
        );
        AssertKind(new DateOnly(2026, 7, 26), MetadataKind.DateOnly);
        AssertKind(new TimeOnly(13, 45, 30), MetadataKind.TimeOnly);
        AssertKind(TimeSpan.FromSeconds(5), MetadataKind.TimeSpan);
        AssertKind(new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), MetadataKind.Guid);
        AssertKind(new Uri("https://example.com/items/42"), MetadataKind.Uri);
    }

    [Theory]
    [InlineData(0.1f, "0.1")]
    [InlineData(5f, "5.0")]
    public void ValidationMessagesShouldUseCanonicalSingleEncoding(float value, string expected)
    {
        var context = new ReadOnlyValidationContext(
            new ValidationState(new ValidationContextOptions()),
            string.Empty
        );

        ValidationErrorMessageFormatting.FormatParameter(value, context).Should().Be(expected);
    }

    [Fact]
    public void ValidationMessagesShouldUseCanonicalEncodingForTheNewStringShapedKinds()
    {
        var context = new ReadOnlyValidationContext(
            new ValidationState(new ValidationContextOptions()),
            string.Empty
        );
        var guid = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        ValidationErrorMessageFormatting
           .FormatParameter(ulong.MaxValue, context)
           .Should()
           .Be("18446744073709551615");
        ValidationErrorMessageFormatting.FormatParameter('x', context).Should().Be("x");
        ValidationErrorMessageFormatting.FormatParameter(
            new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Utc),
            context
        ).Should().Be("2026-07-26T13:45:30Z");
        ValidationErrorMessageFormatting.FormatParameter(
            new DateTimeOffset(2026, 7, 26, 13, 45, 30, TimeSpan.FromHours(2)),
            context
        ).Should().Be("2026-07-26T13:45:30+02:00");
        ValidationErrorMessageFormatting.FormatParameter(new DateOnly(2026, 7, 26), context)
           .Should()
           .Be("2026-07-26");
        ValidationErrorMessageFormatting.FormatParameter(new TimeOnly(13, 45, 30), context)
           .Should()
           .Be("13:45:30");
        ValidationErrorMessageFormatting.FormatParameter(TimeSpan.FromSeconds(5), context)
           .Should()
           .Be("PT5S");
        ValidationErrorMessageFormatting.FormatParameter(guid, context)
           .Should()
           .Be(guid.ToString("D"));
        ValidationErrorMessageFormatting.FormatParameter(
            new Uri("https://example.com/items/42"),
            context
        ).Should().Be("https://example.com/items/42");
    }

    [Fact]
    public void ComparisonAgainstAnUnspecifiedDateTimeShouldProduceAnErrorRatherThanThrow()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context
           .Check(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), target: "when")
           .IsGreaterThan(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

        var error = context.Errors.Should().ContainSingle().Subject;
        error.Message.Should().Be("when must be greater than 2026-01-01T00:00:00");
        error.Metadata!.Value[ValidationErrorMetadataKeys.ComparativeValue]
           .ToCanonicalString()
           .Should()
           .Be("2026-01-01T00:00:00");
    }

    [Fact]
    public void ValidationMessagesShouldNotInventAZoneForUnspecifiedDateTimes()
    {
        var context = new ReadOnlyValidationContext(
            new ValidationState(new ValidationContextOptions()),
            string.Empty
        );

        ValidationErrorMessageFormatting
           .FormatParameter(
                new DateTime(2026, 7, 26, 13, 45, 30, DateTimeKind.Unspecified),
                context
            )
           .Should().Be("2026-07-26T13:45:30");
    }

    private static void AssertKind<T>(T value, MetadataKind expectedKind)
    {
        var definition = BuiltInValidationErrorDefinitions.EqualTo(value);

        definition.Metadata!.Value[ValidationErrorMetadataKeys.ComparativeValue].Kind.Should().Be(expectedKind);
    }
}
