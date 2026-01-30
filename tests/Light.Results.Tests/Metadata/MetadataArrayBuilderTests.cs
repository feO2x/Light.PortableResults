using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataArrayBuilderTests
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

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
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

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var builder = MetadataArrayBuilder.Create();
        builder.Dispose();

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Add_ManyItems_ShouldExpandCapacity()
    {
        using var builder = MetadataArrayBuilder.Create(capacity: 2);

        for (var i = 0; i < 100; i++)
        {
            builder.Add(i);
        }

        builder.Count.Should().Be(100);

        var array = builder.Build();
        array.Should().HaveCount(100);
    }

    [Fact]
    public void AddRange_LargerThanDoubleCapacity_ShouldExpandToFit()
    {
        using var builder = MetadataArrayBuilder.Create(capacity: 4);

        var values = new MetadataValue[20];
        for (var i = 0; i < 20; i++)
        {
            values[i] = i;
        }

        builder.AddRange(values);

        builder.Count.Should().Be(20);
    }

    [Fact]
    public void Add_OnDefaultBuilder_ShouldAllocateBuffer()
    {
        // Using a default builder (not created via Create) exercises the null buffer path in EnsureCapacity
        using var builder = default(MetadataArrayBuilder);
        builder.Add(1);
        builder.Add(2);

        builder.Count.Should().Be(2);
    }

    [Fact]
    public void AddRange_OnDefaultBuilder_ShouldAllocateBuffer()
    {
        // Using a default builder exercises the null buffer path in EnsureCapacity
        using var builder = default(MetadataArrayBuilder);
        builder.AddRange(new MetadataValue[] { 1, 2, 3 });

        builder.Count.Should().Be(3);
    }
}
