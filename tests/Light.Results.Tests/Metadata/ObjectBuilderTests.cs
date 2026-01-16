using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class ObjectBuilderTests
{
    [Fact]
    public void Build_Empty_ShouldReturnEmptyObject()
    {
        using var builder = MetadataObjectBuilder.Create();
        var obj = builder.Build();

        obj.Should().BeEmpty();
    }

    [Fact]
    public void Add_ShouldAccumulateProperties()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("name", "Alice");
        builder.Add("age", 25);

        var obj = builder.Build();

        obj.Should().HaveCount(2);
        obj.TryGetString("name", out var name).Should().BeTrue();
        name.Should().Be("Alice");
    }

    [Fact]
    public void Add_DuplicateKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Add("key", 2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddOrReplace_ShouldUpdateExistingKey()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);
        builder.AddOrReplace("key", 2);

        var obj = builder.Build();

        obj.TryGetInt64("key", out var value).Should().BeTrue();
        value.Should().Be(2L);
    }

    [Fact]
    public void Replace_MissingKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Replace("missing", 1);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetValue_ShouldFindExistingKey()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 42);

        builder.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(42L);
    }

    [Fact]
    public void ContainsKey_ShouldReturnCorrectResult()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("exists", 1);

        builder.ContainsKey("exists").Should().BeTrue();
        builder.ContainsKey("missing").Should().BeFalse();
    }

    [Fact]
    public void From_ShouldCopyExistingObject()
    {
        var original = MetadataObject.Create(("a", 1), ("b", 2));

        using var builder = MetadataObjectBuilder.From(original);
        builder.Add("c", 3);

        var result = builder.Build();

        result.Should().HaveCount(3);
        result.ContainsKey("a").Should().BeTrue();
        result.ContainsKey("b").Should().BeTrue();
        result.ContainsKey("c").Should().BeTrue();
    }

    [Fact]
    public void Build_ShouldSortKeys()
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
    public void Build_CalledTwice_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }
}
