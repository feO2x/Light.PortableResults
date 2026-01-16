using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataObjectAdditionalTests
{
    [Fact]
    public void Enumerator_Reset_ShouldResetToBeginning()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));
        using var enumerator = obj.GetEnumerator();

        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.Reset();
        enumerator.MoveNext();

        enumerator.Current.Key.Should().Be("a");
    }

    [Fact]
    public void Enumerator_Current_ViaIEnumerator_ShouldReturnBoxedValue()
    {
        var obj = MetadataObject.Create(("key", 42));
        IEnumerator enumerator = obj.GetEnumerator();

        enumerator.MoveNext();

        var current = (KeyValuePair<string, MetadataValue>) enumerator.Current;
        current.Key.Should().Be("key");
        (enumerator as IDisposable)?.Dispose();
    }

    [Fact]
    public void Enumerator_Dispose_ShouldNotThrow()
    {
        var obj = MetadataObject.Create(("k", "v"));
        var enumerator = obj.GetEnumerator();

        var act = () => enumerator.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Enumerator_OnEmptyObject_MoveNext_ShouldReturnFalse()
    {
        var obj = MetadataObject.Empty;
        using var enumerator = obj.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_OnDefaultObject_MoveNext_ShouldReturnFalse()
    {
        var obj = default(MetadataObject);
        using var enumerator = obj.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void IEnumerableGeneric_GetEnumerator_ShouldWork()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));
        IEnumerable<KeyValuePair<string, MetadataValue>> enumerable = obj;

        var list = enumerable.ToList();

        list.Should().HaveCount(2);
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ShouldWork()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));
        IEnumerable enumerable = obj;

        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        count.Should().Be(2);
    }

    [Fact]
    public void Keys_OnDefaultObject_ShouldReturnEmpty()
    {
        var obj = default(MetadataObject);

        obj.Keys.Should().BeEmpty();
    }

    [Fact]
    public void Keys_ShouldReturnAllKeys()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2), ("c", 3));

        obj.Keys.Should().BeEquivalentTo("a", "b", "c");
    }

    [Fact]
    public void Values_OnDefaultObject_ShouldReturnEmpty()
    {
        var obj = default(MetadataObject);

        obj.Values.Should().BeEmpty();
    }

    [Fact]
    public void Values_ShouldReturnAllValues()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));

        obj.Values.Should().HaveCount(2);
    }

    [Fact]
    public void Indexer_OnMissingKey_ShouldThrow()
    {
        var obj = MetadataObject.Create(("a", 1));

        var act = () => obj["missing"];

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ContainsKey_WithNullKey_ShouldThrow()
    {
        var obj = MetadataObject.Create(("a", 1));

        var act = () => obj.ContainsKey(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ContainsKey_OnDefaultObject_ShouldReturnFalse()
    {
        var obj = default(MetadataObject);

        obj.ContainsKey("any").Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_WithNullKey_ShouldThrow()
    {
        var obj = MetadataObject.Create(("a", 1));

        var act = () => obj.TryGetValue(null!, out _);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetValue_OnDefaultObject_ShouldReturnFalse()
    {
        var obj = default(MetadataObject);

        obj.TryGetValue("any", out var value).Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void TryGetBoolean_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.TryGetBoolean("missing", out var value).Should().BeFalse();
        value.Should().BeFalse();
    }

    [Fact]
    public void TryGetBoolean_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", "string"));

        obj.TryGetBoolean("a", out var value).Should().BeFalse();
        value.Should().BeFalse();
    }

    [Fact]
    public void TryGetInt64_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", "string"));

        obj.TryGetInt64("missing", out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetInt64_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", "string"));

        obj.TryGetInt64("a", out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetDouble_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.TryGetDouble("missing", out var value).Should().BeFalse();
        value.Should().Be(0.0);
    }

    [Fact]
    public void TryGetDouble_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", "string"));

        obj.TryGetDouble("a", out var value).Should().BeFalse();
        value.Should().Be(0.0);
    }

    [Fact]
    public void TryGetString_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.TryGetString("missing", out var value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetString_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 42));

        obj.TryGetString("a", out var value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGetDecimal_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", "string"));

        obj.TryGetDecimal("missing", out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetDecimal_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", true));

        obj.TryGetDecimal("a", out var value).Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetArray_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.TryGetArray("missing", out var value).Should().BeFalse();
        value.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetArray_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 42));

        obj.TryGetArray("a", out var value).Should().BeFalse();
        value.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetObject_WhenKeyNotFound_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.TryGetObject("missing", out var value).Should().BeFalse();
        value.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetObject_WhenWrongType_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 42));

        obj.TryGetObject("a", out var value).Should().BeFalse();
        value.Count.Should().Be(0);
    }

    [Fact]
    public void NotEqualOperator_ShouldReturnTrueForDifferentObjects()
    {
        var obj1 = MetadataObject.Create(("a", 1));
        var obj2 = MetadataObject.Create(("b", 2));

        (obj1 != obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithMetadataObject_ShouldReturnTrue()
    {
        var obj = MetadataObject.Create(("a", 1));
        object boxed = obj;

        obj.Equals(boxed).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithNonMetadataObject_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        // ReSharper disable once SuspiciousTypeConversion.Global -- OK in this test scenario
        obj.Equals("not an object").Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WithNull_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(("a", 1));

        obj.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_DefaultObject_ShouldReturnZero()
    {
        var obj = default(MetadataObject);

        obj.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void GetHashCode_SameObject_ShouldReturnConsistentValue()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));

        var hash1 = obj.GetHashCode();
        var hash2 = obj.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Count_OnDefaultObject_ShouldReturnZero()
    {
        var obj = default(MetadataObject);

        obj.Count.Should().Be(0);
    }

    [Fact]
    public void Create_WithNullKey_ShouldThrow()
    {
        var act = () => MetadataObject.Create((null!, 1));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithDuplicateKeys_ShouldThrow()
    {
        var act = () => MetadataObject.Create(("a", 1), ("a", 2));

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Duplicate key*");
    }

    [Fact]
    public void GetHashCode_WithMultipleEntries_ShouldReturnConsistentValue()
    {
        var obj = MetadataObject.Create(("key1", 1), ("key2", 2), ("key3", 3));

        var hash1 = obj.GetHashCode();
        var hash2 = obj.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_EqualObjects_ShouldReturnSameHash()
    {
        var obj1 = MetadataObject.Create(("a", 1), ("b", 2));
        var obj2 = MetadataObject.Create(("a", 1), ("b", 2));

        obj1.GetHashCode().Should().Be(obj2.GetHashCode());
    }

    [Fact]
    public void TryGetValue_WithLargeObject_ShouldUseDictionaryLookup()
    {
        var obj = MetadataObject.Create(
            ("key0", 0),
            ("key1", 1),
            ("key2", 2),
            ("key3", 3),
            ("key4", 4),
            ("key5", 5),
            ("key6", 6),
            ("key7", 7),
            ("key8", 8),
            ("key9", 9)
        );

        var result = obj.TryGetValue("key5", out var value);

        result.Should().BeTrue();
        value.Should().Be(MetadataValue.FromInt64(5));
    }

    [Fact]
    public void TryGetValue_WithLargeObject_MissingKey_ShouldReturnFalse()
    {
        var obj = MetadataObject.Create(
            ("key0", 0),
            ("key1", 1),
            ("key2", 2),
            ("key3", 3),
            ("key4", 4),
            ("key5", 5),
            ("key6", 6),
            ("key7", 7),
            ("key8", 8),
            ("key9", 9)
        );

        var result = obj.TryGetValue("missing", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void ContainsKey_WithLargeObject_ShouldUseDictionaryLookup()
    {
        var obj = MetadataObject.Create(
            ("key0", 0),
            ("key1", 1),
            ("key2", 2),
            ("key3", 3),
            ("key4", 4),
            ("key5", 5),
            ("key6", 6),
            ("key7", 7),
            ("key8", 8),
            ("key9", 9)
        );

        obj.ContainsKey("key7").Should().BeTrue();
        obj.ContainsKey("missing").Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentKeysSameCount_ShouldReturnFalse()
    {
        var obj1 = MetadataObject.Create(("a", 1), ("b", 2));
        var obj2 = MetadataObject.Create(("c", 1), ("d", 2));

        obj1.Equals(obj2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentValuesSameKeys_ShouldReturnFalse()
    {
        var obj1 = MetadataObject.Create(("a", 1), ("b", 2));
        var obj2 = MetadataObject.Create(("a", 1), ("b", 99));

        obj1.Equals(obj2).Should().BeFalse();
    }
}
