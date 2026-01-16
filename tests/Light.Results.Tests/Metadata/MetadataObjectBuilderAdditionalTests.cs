using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataObjectBuilderAdditionalTests
{
    [Fact]
    public void Add_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        var act = () => builder.Add(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void TryGetValue_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        var act = () => builder.TryGetValue(null!, out _);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void ContainsKey_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        var act = () => builder.ContainsKey(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Replace_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        var act = () => builder.Replace(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Replace_ExistingKey_ShouldUpdateValue()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        builder.Replace("key", 2);

        builder.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var intValue).Should().BeTrue();
        intValue.Should().Be(2L);
    }

    [Fact]
    public void AddOrReplace_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        var act = () => builder.AddOrReplace(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void AddOrReplace_NewKey_ShouldAddEntry()
    {
        using var builder = MetadataObjectBuilder.Create();

        builder.AddOrReplace("newKey", 42);

        builder.ContainsKey("newKey").Should().BeTrue();
    }

    [Fact]
    public void Dispose_WithoutBuild_ShouldNotThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Dispose();

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Add_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.Add("key", 1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Replace_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);
        builder.Build();

        var act = () => builder.Replace("key", 2);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void AddOrReplace_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.AddOrReplace("key", 1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void From_WithEmptyObject_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.From(MetadataObject.Empty);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void From_WithDefaultObject_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.From(default);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_MissingKey_ShouldReturnFalse()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        var result = builder.TryGetValue("missing", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void Create_WithCapacity_ShouldCreateBuilder()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 10);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Create_WithZeroCapacity_ShouldUseDefaultCapacity()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 0);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Add_ManyEntries_ShouldExpandCapacity()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 2);

        for (var i = 0; i < 10; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.Count.Should().Be(10);
    }

    [Fact]
    public void Add_ShouldMaintainSortedOrder()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("z", 1);
        builder.Add("a", 2);
        builder.Add("m", 3);

        var obj = builder.Build();

        var keys = new List<string>();
        foreach (var kvp in obj)
        {
            keys.Add(kvp.Key);
        }

        keys.Should().Equal("a", "m", "z");
    }

    [Fact]
    public void AddOrReplace_InsertInMiddle_ShouldMaintainOrder()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("a", 1);
        builder.Add("z", 3);

        builder.AddOrReplace("m", 2);

        var obj = builder.Build();

        var keys = new List<string>();
        foreach (var kvp in obj)
        {
            keys.Add(kvp.Key);
        }

        keys.Should().Equal("a", "m", "z");
    }
}
