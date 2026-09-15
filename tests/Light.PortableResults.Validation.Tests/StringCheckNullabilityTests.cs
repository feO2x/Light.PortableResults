using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.Tests;

// These tests guard the nullable annotations of the string assertions: they must compile without
// CS8620/CS8601 warnings (treated as errors in Release builds) when chained after IsNotNullOrWhiteSpace.
public sealed class StringCheckNullabilityTests
{
    [Fact]
    public void HasMaxLength_ShouldChainWithOtherStringAssertions()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var dto = new Dto("Title");

        string title = context.Check(dto.Title).IsNotNullOrWhiteSpace().HasMinLength(1).HasMaxLength(200);
        string overriddenTitle = context
           .Check(dto.Title)
           .IsNotNullOrWhiteSpace()
           .HasMaxLength(200, "Title is too long")
           .HasLengthIn(1, 200);

        title.Should().Be("Title");
        overriddenTitle.Should().Be("Title");
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void StringCountAssertions_ShouldChainWithOtherStringAssertions()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var dto = new Dto("Title");

        string title = context
           .Check(dto.Title)
           .IsNotNullOrWhiteSpace()
           .HasCount(5)
           .HasMinCount(1)
           .HasMaxCount(10)
           .HasCount(5, "Unused")
           .HasMinCount(1, "Unused")
           .HasMaxCount(10, "Unused");

        title.Should().Be("Title");
        context.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void IsEnumName_ShouldChainWithOtherStringAssertions()
    {
        var context = DefaultValidationContextFactory.Create().CreateValidationContext();
        var dto = new Dto("Approved");

        string status = context
           .Check(dto.Title)
           .IsNotNullOrWhiteSpace()
           .IsEnumName<Status>()
           .IsEnumName<Status>("Unused")
           .HasMaxLength(20);

        status.Should().Be("Approved");
        context.HasErrors.Should().BeFalse();
    }

    private sealed record Dto(string Title);

    private enum Status
    {
        Approved
    }
}
