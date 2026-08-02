using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Validation.Messaging;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

public sealed class DateTimeKindValidationTests
{
    private static readonly DateTime SampleDateTime = new (2026, 8, 2, 10, 0, 0, DateTimeKind.Unspecified);

    private static readonly KindAssertion[] KindAssertions =
    [
        new (
            "IsUtc",
            DateTimeKind.Utc,
            "Utc",
            "Timestamp must be represented in UTC",
            static (check, shortCircuitOnError) => check.IsUtc(shortCircuitOnError),
            static (check, overrides, shortCircuitOnError) => check.IsUtc(overrides, shortCircuitOnError),
            static (templates, template) => templates with { Utc = template }
        ),
        new (
            "IsLocal",
            DateTimeKind.Local,
            "Local",
            "Timestamp must be a local date and time",
            static (check, shortCircuitOnError) => check.IsLocal(shortCircuitOnError),
            static (check, overrides, shortCircuitOnError) => check.IsLocal(overrides, shortCircuitOnError),
            static (templates, template) => templates with { Local = template }
        ),
        new (
            "IsUnspecified",
            DateTimeKind.Unspecified,
            "Unspecified",
            "Timestamp must not specify a time zone",
            static (check, shortCircuitOnError) => check.IsUnspecified(shortCircuitOnError),
            static (check, overrides, shortCircuitOnError) => check.IsUnspecified(overrides, shortCircuitOnError),
            static (templates, template) => templates with { Unspecified = template }
        )
    ];

    // The central assertion test: every rule must accept its own kind and reject both others, so that the three
    // rules together partition DateTimeKind without overlap.
    [Fact]
    public void KindAssertions_ShouldAcceptExactlyTheirOwnKind()
    {
        foreach (var assertion in KindAssertions)
        {
            foreach (var kind in Enum.GetValues<DateTimeKind>())
            {
                var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
                var value = DateTime.SpecifyKind(SampleDateTime, kind);

                assertion.Apply(
                    context.Check(value, target: "timestamp", displayName: "Timestamp"),
                    false
                );

                if (kind == assertion.AcceptedKind)
                {
                    context.Errors.Should().BeEmpty("{0} must accept {1}", assertion.Name, kind);
                }
                else
                {
                    context.Errors.Should().ContainSingle(
                        error =>
                            error.Target == "timestamp" &&
                            error.Code == assertion.ErrorCode &&
                            error.Message == assertion.ExpectedMessage,
                        "{0} must reject {1}",
                        assertion.Name,
                        kind
                    );
                }
            }
        }
    }

    // Keeps the partition claim above honest: if DateTimeKind ever gains a member, no assertion covers it and
    // the matrix silently stops being exhaustive.
    [Fact]
    public void KindAssertions_ShouldCoverEveryDateTimeKind() =>
        KindAssertions.Select(static assertion => assertion.AcceptedKind)
           .Should()
           .BeEquivalentTo(Enum.GetValues<DateTimeKind>());

    // Guards the JSON premise the documentation rests on. This deliberately overlaps the matrix: it is a
    // regression detector for third-party behavior. If System.Text.Json ever changes how it maps these wire
    // formats, the assertions keep working while their documented meaning shifts, and this test is what says so.
    // Only the kind is host-independent — the two Local values depend on the server's time zone, so the
    // wall-clock value is deliberately not asserted.
    [Theory]
    [InlineData("2026-08-02T10:00:00Z", DateTimeKind.Utc)]
    [InlineData("2026-08-02T10:00:00+00:00", DateTimeKind.Local)]
    [InlineData("2026-08-02T10:00:00+02:00", DateTimeKind.Local)]
    [InlineData("2026-08-02T10:00:00", DateTimeKind.Unspecified)]
    public void DefaultJsonConverter_ShouldProduceTheDocumentedKind(string wireValue, DateTimeKind expectedKind)
    {
        var dto = JsonSerializer.Deserialize<TimestampDto>($$"""{"OccurredAt":"{{wireValue}}"}""");

        dto!.OccurredAt.Kind.Should().Be(expectedKind);

        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

            assertion.Apply(
                context.Check(dto.OccurredAt, target: "timestamp", displayName: "Timestamp"),
                false
            );

            if (assertion.AcceptedKind == expectedKind)
            {
                context.Errors.Should().BeEmpty("{0} must accept {1}", assertion.Name, wireValue);
            }
            else
            {
                context.Errors.Should().ContainSingle(
                    error => error.Code == assertion.ErrorCode,
                    "{0} must reject {1}",
                    assertion.Name,
                    wireValue
                );
            }
        }
    }

    // IsUtc tests the value's kind, not its provenance: UtcNow never passed through a JSON converter.
    [Fact]
    public void IsUtc_ShouldAcceptUtcNow()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(DateTime.UtcNow, target: "timestamp", displayName: "Timestamp").IsUtc();

        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void IsLocal_ShouldAcceptNow()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(DateTime.Now, target: "timestamp", displayName: "Timestamp").IsLocal();

        context.Errors.Should().BeEmpty();
    }

    // Documents the default-value trap: an absent DateTime property is Unspecified and therefore passes.
    [Fact]
    public void IsUnspecified_ShouldAcceptTheDefaultDateTime()
    {
        var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

        context.Check(default(DateTime), target: "timestamp", displayName: "Timestamp").IsUnspecified();

        context.Errors.Should().BeEmpty();
    }

    [Fact]
    public void KindAssertions_ShouldApplyOverrides_WhenTheKindIsWrong()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

            assertion.ApplyWithOverrides(
                context.Check(RejectedValueFor(assertion), target: "timestamp", displayName: "Timestamp"),
                new ErrorOverrides { Code = "TimestampZoneMismatch" },
                false
            );

            context.Errors.Should().ContainSingle(
                error => error.Target == "timestamp" && error.Code == "TimestampZoneMismatch",
                "{0} must honor overrides",
                assertion.Name
            );
        }
    }

    [Fact]
    public void KindAssertions_ShouldNotAddError_WhenOverridesAreUsedAndTheKindIsCorrect()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();

            assertion.ApplyWithOverrides(
                context.Check(
                    DateTime.SpecifyKind(SampleDateTime, assertion.AcceptedKind),
                    target: "timestamp",
                    displayName: "Timestamp"
                ),
                new ErrorOverrides { Code = "Unused" },
                false
            );

            context.Errors.Should().BeEmpty("{0} must accept its own kind", assertion.Name);
        }
    }

    [Fact]
    public void KindAssertions_ShouldThrow_WhenOverridesAreEmpty()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(
                DateTime.SpecifyKind(SampleDateTime, assertion.AcceptedKind),
                target: "timestamp"
            );

            var act = () => assertion.ApplyWithOverrides(check, new ErrorOverrides(), false);

            act.Should().Throw<ArgumentException>("{0} must reject empty overrides", assertion.Name);
        }
    }

    [Fact]
    public void KindAssertions_ShouldRespectAnAlreadyShortCircuitedCheck()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(RejectedValueFor(assertion), target: "timestamp").ShortCircuit();

            assertion.Apply(check, false).IsShortCircuited.Should().BeTrue();
            context.Errors.Should().BeEmpty("{0} must skip a short-circuited check", assertion.Name);
        }
    }

    [Fact]
    public void KindAssertions_ShouldRespectAnAlreadyShortCircuitedCheck_WhenOverridesAreUsed()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(RejectedValueFor(assertion), target: "timestamp").ShortCircuit();

            assertion.ApplyWithOverrides(check, new ErrorOverrides { Code = "Unused" }, false)
               .IsShortCircuited.Should()
               .BeTrue();
            context.Errors.Should().BeEmpty("{0} must skip a short-circuited check", assertion.Name);
        }
    }

    [Fact]
    public void KindAssertions_ShouldShortCircuit_WhenRequested()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(
                RejectedValueFor(assertion),
                target: "timestamp",
                displayName: "Timestamp"
            );

            assertion.Apply(check, true).IsShortCircuited.Should().BeTrue();
            context.Errors.Should().ContainSingle(
                error => error.Target == "timestamp" && error.Code == assertion.ErrorCode,
                "{0} must still report the failure",
                assertion.Name
            );
        }
    }

    [Fact]
    public void KindAssertions_ShouldShortCircuit_WhenOverridesAreUsedAndRequested()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(
                RejectedValueFor(assertion),
                target: "timestamp",
                displayName: "Timestamp"
            );

            assertion
               .ApplyWithOverrides(check, new ErrorOverrides { Message = "Timestamp has the wrong zone" }, true)
               .IsShortCircuited.Should()
               .BeTrue();
            context.Errors.Should().ContainSingle(
                error => error.Target == "timestamp" && error.Message == "Timestamp has the wrong zone",
                "{0} must honor the override message",
                assertion.Name
            );
        }
    }

    [Fact]
    public void KindAssertions_ShouldNotShortCircuit_WhenNotRequested()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = ValidationWorkflowTestData.ValidationContextFactory.CreateValidationContext();
            var check = context.Check(
                RejectedValueFor(assertion),
                target: "timestamp",
                displayName: "Timestamp"
            );

            assertion.Apply(check, false).IsShortCircuited.Should().BeFalse("{0}", assertion.Name);
        }
    }

    [Fact]
    public void KindAssertions_ShouldUseTheCustomizedTemplate()
    {
        foreach (var assertion in KindAssertions)
        {
            var context = CreateContextWithTemplates(
                assertion.WithTemplate(
                    ValidationErrorTemplates.Default,
                    new ValidationErrorTemplates.Constant("The timestamp zone is not accepted")
                )
            );

            assertion.Apply(
                context.Check(RejectedValueFor(assertion), target: "timestamp", displayName: "Timestamp"),
                false
            );

            context.Errors.Should().ContainSingle(
                error =>
                    error.Code == assertion.ErrorCode &&
                    error.Message == "The timestamp zone is not accepted",
                "{0} must use its customized template",
                assertion.Name
            );
        }
    }

    [Fact]
    public void KindAssertions_ShouldKeepTheCustomizedTemplate_WhenTemplatesAreCopiedAgain()
    {
        foreach (var assertion in KindAssertions)
        {
            var customizedTemplates = assertion.WithTemplate(
                ValidationErrorTemplates.Default,
                new ValidationErrorTemplates.Constant("The timestamp zone is not accepted")
            );
            var context = CreateContextWithTemplates(
                customizedTemplates with { NotNull = new ValidationErrorTemplates.Constant("Value is required") }
            );

            assertion.Apply(
                context.Check(RejectedValueFor(assertion), target: "timestamp", displayName: "Timestamp"),
                false
            );

            context.Errors.Should().ContainSingle(
                error =>
                    error.Code == assertion.ErrorCode &&
                    error.Message == "The timestamp zone is not accepted",
                "{0} must survive a subsequent with-expression copy",
                assertion.Name
            );
        }
    }

    private static DateTime RejectedValueFor(KindAssertion assertion) =>
        DateTime.SpecifyKind(
            SampleDateTime,
            assertion.AcceptedKind == DateTimeKind.Utc ? DateTimeKind.Unspecified : DateTimeKind.Utc
        );

    private static ValidationContext CreateContextWithTemplates(ValidationErrorTemplates templates)
    {
        var options = new ValidationContextOptions() with { ErrorTemplates = templates };
        return new DefaultValidationContextFactory(options).CreateValidationContext();
    }

    private sealed record KindAssertion(
        string Name,
        DateTimeKind AcceptedKind,
        string ErrorCode,
        string ExpectedMessage,
        Func<Check<DateTime>, bool, Check<DateTime>> Apply,
        Func<Check<DateTime>, ErrorOverrides, bool, Check<DateTime>> ApplyWithOverrides,
        Func<ValidationErrorTemplates, IValidationErrorMessageTemplate, ValidationErrorTemplates> WithTemplate
    );
}

public sealed class TimestampDto
{
    public DateTime OccurredAt { get; init; }
}
