using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class ArrayBuilderTests
{
    [Fact]
    public void Build_Empty_ShouldReturnEmptyArray()
    {
        using var builder = MetadataArrayBuilder.Create();
        var array = builder.Build();

        array.Should().BeEmpty();
    }

    [Fact]
    public void Add_ShouldAccumulateValues()
    {
        using var builder = MetadataArrayBuilder.Create();
        builder.Add(1);
        builder.Add(2);
        builder.Add(3);

        var array = builder.Build();

        array.Should().HaveCount(3);
        array[0].TryGetInt64(out var v0).Should().BeTrue();
        v0.Should().Be(1);
        array[1].TryGetInt64(out var v1).Should().BeTrue();
        v1.Should().Be(2);
        array[2].TryGetInt64(out var v2).Should().BeTrue();
        v2.Should().Be(3);
    }

    [Fact]
    public void AddRange_ShouldAddMultipleValues()
    {
        using var builder = MetadataArrayBuilder.Create();
        builder.AddRange(new MetadataValue[] { 10, 20, 30 });

        var array = builder.Build();

        array.Should().HaveCount(3);
    }

    [Fact]
    public void Build_CalledTwice_ShouldThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Build();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_AfterBuild_ShouldThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Build();

        var act = () => builder.Add(1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Add(1);

        var act = () =>
        {
            builder.Dispose();
            builder.Dispose(); // Double dispose should be safe
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void LargeArray_ShouldGrowBuffer()
    {
        using var builder = MetadataArrayBuilder.Create(2);
        for (var i = 0; i < 100; i++)
        {
            builder.Add(i);
        }

        var array = builder.Build();

        array.Should().HaveCount(100);
    }
}
