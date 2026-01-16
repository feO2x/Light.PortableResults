using System;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataObjectTests
{
    [Fact]
    public void Empty_ShouldHaveZeroCount()
    {
        var obj = MetadataObject.Empty;

        obj.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithProperties_ShouldStoreAll()
    {
        var obj = MetadataObject.Create(
            ("name", "John"),
            ("age", 30),
            ("active", true)
        );

        obj.Should().HaveCount(3);
        obj.TryGetString("name", out var name).Should().BeTrue();
        name.Should().Be("John");
        obj.TryGetInt64("age", out var age).Should().BeTrue();
        age.Should().Be(30L);
        obj.TryGetBoolean("active", out var active).Should().BeTrue();
        active.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSortKeysDeterministically()
    {
        var obj = MetadataObject.Create(
            ("zebra", 1),
            ("apple", 2),
            ("mango", 3)
        );

        var keys = new List<string>();
        foreach (var kvp in obj)
        {
            keys.Add(kvp.Key);
        }

        keys.Should().Equal("apple", "mango", "zebra");
    }

    [Fact]
    public void Create_WithDuplicateKeys_ShouldThrow()
    {
        var act = () => MetadataObject.Create(("key", 1), ("key", 2));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Indexer_ExistingKey_ShouldReturnValue()
    {
        var obj = MetadataObject.Create(("foo", "bar"));

        var value = obj["foo"];

        value.Kind.Should().Be(MetadataKind.String);
        value.TryGetString(out var str).Should().BeTrue();
        str.Should().Be("bar");
    }

    [Fact]
    public void Indexer_MissingKey_ShouldThrow()
    {
        var obj = MetadataObject.Create(("foo", "bar"));

        var act = () => obj["missing"];

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ContainsKey_ShouldReturnCorrectResult()
    {
        var obj = MetadataObject.Create(("exists", 1));

        obj.ContainsKey("exists").Should().BeTrue();
        obj.ContainsKey("missing").Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_ExistingKey_ShouldReturnTrue()
    {
        var obj = MetadataObject.Create(("key", 42));

        obj.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(42L);
    }

    [Fact]
    public void TryGetValue_MissingKey_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("key", 42));

        obj.TryGetValue("missing", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetDecimal_ShouldParseDecimalString()
    {
        var obj = MetadataObject.Create(("price", MetadataValue.FromDecimal(19.99m)));

        obj.TryGetDecimal("price", out var price).Should().BeTrue();
        price.Should().Be(19.99m);
    }

    [Fact]
    public void TryGetArray_ShouldReturnNestedArray()
    {
        var nested = MetadataArray.Create(1, 2, 3);
        var obj = MetadataObject.Create(("items", nested));

        obj.TryGetArray("items", out var items).Should().BeTrue();
        items.Should().HaveCount(3);
    }

    [Fact]
    public void TryGetObject_ShouldReturnNestedObject()
    {
        var nested = MetadataObject.Create(("inner", "value"));
        var obj = MetadataObject.Create(("nested", nested));

        obj.TryGetObject("nested", out var result).Should().BeTrue();
        result.TryGetString("inner", out var inner).Should().BeTrue();
        inner.Should().Be("value");
    }

    [Fact]
    public void ImplicitConversion_ToMetadataValue_ShouldWork()
    {
        var obj = MetadataObject.Create(("key", "value"));
        MetadataValue value = obj;

        value.Kind.Should().Be(MetadataKind.Object);
        value.TryGetObject(out var result).Should().BeTrue();
        result.Should().ContainSingle();
    }

    [Fact]
    public void Enumeration_ShouldIterateAllProperties()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2), ("c", 3));
        var count = 0;

        foreach (var kvp in obj)
        {
            count++;
            kvp.Key.Should().NotBeNull();
        }

        count.Should().Be(3);
    }

    [Fact]
    public void Equals_WithIdenticalObjects_ShouldReturnTrue()
    {
        var left = MetadataObject.Create(("name", "Alice"), ("age", 30));
        var right = MetadataObject.Create(("name", "Alice"), ("age", 30));

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
        left.Equals(right).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        var left = MetadataObject.Create(("name", "Alice"), ("age", 30));
        var right = MetadataObject.Create(("name", "Bob"), ("age", 30));

        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
        left.Equals(right).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentKeys_ShouldReturnFalse()
    {
        var left = MetadataObject.Create(("name", "Alice"));
        var right = MetadataObject.Create(("username", "Alice"));

        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCounts_ShouldReturnFalse()
    {
        var left = MetadataObject.Create(("name", "Alice"));
        var right = MetadataObject.Create(("name", "Alice"), ("age", 30));

        (left == right).Should().BeFalse();
        (left != right).Should().BeTrue();
    }

    [Fact]
    public void Equals_BothEmpty_ShouldReturnTrue()
    {
        var left = MetadataObject.Empty;
        var right = MetadataObject.Empty;

        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
        left.Equals(right).Should().BeTrue();
    }

    [Fact]
    public void Equals_EmptyVsNonEmpty_ShouldReturnFalse()
    {
        var empty = MetadataObject.Empty;
        var nonEmpty = MetadataObject.Create(("key", "value"));

        (empty == nonEmpty).Should().BeFalse();
        (empty != nonEmpty).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNestedObjects_ShouldCompareByValue()
    {
        var nested1 = MetadataObject.Create(("inner", "value"));
        var nested2 = MetadataObject.Create(("inner", "value"));

        var left = MetadataObject.Create(("nested", nested1));
        var right = MetadataObject.Create(("nested", nested2));

        (left == right).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithIdenticalObjects_ShouldReturnSameHash()
    {
        var left = MetadataObject.Create(("name", "Alice"), ("age", 30));
        var right = MetadataObject.Create(("name", "Alice"), ("age", 30));

        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithObject_ShouldUseOverride()
    {
        var obj1 = MetadataObject.Create(("key", "value"));
        var obj2 = MetadataObject.Create(("key", "value"));
        object boxedObj2 = obj2;

        obj1.Equals(boxedObj2).Should().BeTrue();
    }
}
