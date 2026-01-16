using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataArrayBuilderAdditionalTests
{
    [Fact]
    public void AddRange_AfterBuild_ShouldThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Build();

        var act = () => builder.AddRange(new MetadataValue[] { 1, 2 });

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Create_WithZeroCapacity_ShouldUseDefaultCapacity()
    {
        using var builder = MetadataArrayBuilder.Create(capacity: 0);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Create_WithCapacity_ShouldCreateBuilder()
    {
        using var builder = MetadataArrayBuilder.Create(capacity: 10);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void AddRange_WithEmptySpan_ShouldNotAddValues()
    {
        using var builder = MetadataArrayBuilder.Create();
        builder.AddRange(ReadOnlySpan<MetadataValue>.Empty);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void AddRange_ShouldExpandCapacity()
    {
        using var builder = MetadataArrayBuilder.Create(capacity: 2);
        builder.AddRange(new MetadataValue[] { 1, 2, 3, 4, 5 });

        builder.Count.Should().Be(5);
    }

    [Fact]
    public void Dispose_WithoutBuild_ShouldNotThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Add(1);

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }
}
