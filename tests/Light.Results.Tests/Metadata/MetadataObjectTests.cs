using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public void Create_ShouldPreserveInsertionOrder()
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

        keys.Should().Equal("zebra", "apple", "mango");
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
    public void Equals_BothDefault_ShouldReturnTrue()
    {
        var left = default(MetadataObject);
        var right = default(MetadataObject);

        left.Equals(right).Should().BeTrue();
        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultVsNonEmpty_ShouldReturnFalse()
    {
        var defaultObj = default(MetadataObject);
        var nonEmpty = MetadataObject.Create(("key", "value"));

        defaultObj.Equals(nonEmpty).Should().BeFalse();
        (defaultObj == nonEmpty).Should().BeFalse();
    }

    [Fact]
    public void Equals_NonEmptyVsDefault_ShouldReturnFalse()
    {
        var nonEmpty = MetadataObject.Create(("key", "value"));
        var defaultObj = default(MetadataObject);

        nonEmpty.Equals(defaultObj).Should().BeFalse();
        (nonEmpty == defaultObj).Should().BeFalse();
    }

    [Fact]
    public void Equals_DefaultVsEmpty_ShouldReturnFalse()
    {
        var defaultObj = default(MetadataObject);
        var empty = MetadataObject.Empty;

        // default(MetadataObject) has null Data, Empty has non-null Data
        defaultObj.Equals(empty).Should().BeFalse();
        (defaultObj == empty).Should().BeFalse();
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

    [Fact]
    public void HasOnlyPrimitiveChildren_WithOnlyPrimitives_ShouldReturnTrue()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", "string"), ("c", true), ("d", 3.14));

        obj.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_WithNestedArray_ShouldReturnFalse()
    {
        var nested = MetadataArray.Create(1, 2, 3);
        var obj = MetadataObject.Create(("a", 1), ("items", nested));

        obj.HasOnlyPrimitiveChildren.Should().BeFalse();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_WithNestedObject_ShouldReturnFalse()
    {
        var nested = MetadataObject.Create(("inner", "value"));
        var obj = MetadataObject.Create(("a", 1), ("nested", nested));

        obj.HasOnlyPrimitiveChildren.Should().BeFalse();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_OnEmptyObject_ShouldReturnTrue()
    {
        var obj = MetadataObject.Empty;

        obj.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_OnDefaultObject_ShouldReturnTrue()
    {
        var obj = default(MetadataObject);

        obj.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_IsCached()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));

        // Call twice to exercise caching
        var result1 = obj.HasOnlyPrimitiveChildren;
        var result2 = obj.HasOnlyPrimitiveChildren;

        result1.Should().Be(result2).And.BeTrue();
    }

    [Fact]
    public void ToString_WithZeroEntries_ShouldReturnEmptyObject()
    {
        var obj = MetadataObject.Empty;

        obj.ToString().Should().Be("{}");
    }

    [Fact]
    public void ToString_WithOneEntry_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(("key", "value"));

        obj.ToString().Should().Be("{\"key\": \"value\"}");
    }

    [Fact]
    public void ToString_WithTwoEntries_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2));

        obj.ToString().Should().Be("{\"a\": 1, \"b\": 2}");
    }

    [Fact]
    public void ToString_WithThreeEntries_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2), ("c", 3));

        obj.ToString().Should().Be("{\"a\": 1, \"b\": 2, \"c\": 3}");
    }

    [Fact]
    public void ToString_WithFourEntries_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2), ("c", 3), ("d", 4));

        obj.ToString().Should().Be("{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4}");
    }

    [Fact]
    public void ToString_WithFiveEntries_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(("a", 1), ("b", 2), ("c", 3), ("d", 4), ("e", 5));

        obj.ToString().Should().Be("{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4, \"e\": 5}");
    }

    [Fact]
    public void ToString_WithSixOrMoreEntries_ShouldUseStringBuilder()
    {
        var obj = MetadataObject.Create(
            ("a", 1),
            ("b", 2),
            ("c", 3),
            ("d", 4),
            ("e", 5),
            ("f", 6)
        );

        obj.ToString().Should().Be("{\"a\": 1, \"b\": 2, \"c\": 3, \"d\": 4, \"e\": 5, \"f\": 6}");
    }

    [Fact]
    public void ToString_WithManyEntries_ShouldFormatCorrectly()
    {
        var obj = MetadataObject.Create(
            ("a", 1),
            ("b", 2),
            ("c", 3),
            ("d", 4),
            ("e", 5),
            ("f", 6),
            ("g", 7),
            ("h", 8)
        );

        var result = obj.ToString();

        result.Should().StartWith("{\"a\": 1");
        result.Should().EndWith("\"h\": 8}");
    }

    [Fact]
    public void ToString_OnDefaultObject_ShouldReturnEmptyObject()
    {
        var obj = default(MetadataObject);

        obj.ToString().Should().Be("{}");
    }

    [Fact]
    public void Create_WithCustomKeyComparer_ShouldUseComparer()
    {
        var obj = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key", "value1")
        );

        obj.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetString(out var str).Should().BeTrue();
        str.Should().Be("value1");
    }

    [Fact]
    public void Create_WithCustomKeyComparer_ContainsKey_ShouldUseCaseInsensitiveComparison()
    {
        var obj = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("MyKey", 42)
        );

        obj.ContainsKey("MYKEY").Should().BeTrue();
        obj.ContainsKey("mykey").Should().BeTrue();
    }

    [Fact]
    public void Create_WithCustomKeyComparer_AndNullProperties_ShouldReturnEmpty()
    {
        var obj = MetadataObject.Create(StringComparer.OrdinalIgnoreCase, null);

        obj.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithCustomKeyComparer_AndEmptyProperties_ShouldReturnEmpty()
    {
        var obj = MetadataObject.Create(StringComparer.OrdinalIgnoreCase);

        obj.Should().BeEmpty();
    }

    [Fact]
    public void Equals_WithCustomKeyComparer_ShouldCompareUsingComparer()
    {
        var obj1 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key", "value")
        );
        var obj2 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key", "value")
        );

        obj1.Equals(obj2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithCustomKeyComparer_DifferentKeys_ShouldReturnFalse()
    {
        var obj1 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key1", "value")
        );
        var obj2 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key2", "value")
        );

        obj1.Equals(obj2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithCustomKeyComparer_DifferentValues_ShouldReturnFalse()
    {
        var obj1 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key", "value1")
        );
        var obj2 = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key", "value2")
        );

        obj1.Equals(obj2).Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_SmallObjectWithKeyComparer_MissingKey_ShouldReturnFalse()
    {
        // Small object (< DictionaryThreshold) with key comparer exercises linear search with comparer
        var obj = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key1", 1),
            ("Key2", 2),
            ("Key3", 3)
        );

        obj.TryGetValue("missing", out var value).Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void TryGetValue_OnEmptyObject_ShouldReturnFalse()
    {
        var obj = MetadataObject.Empty;

        obj.TryGetValue("any", out var value).Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void ContainsKey_OnEmptyObject_ShouldReturnFalse()
    {
        var obj = MetadataObject.Empty;

        obj.ContainsKey("any").Should().BeFalse();
    }


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
    public void TryGetDouble_WhenDoubleType_ShouldReturnTrue()
    {
        var obj = MetadataObject.Create(("pi", MetadataValue.FromDouble(3.14159)));

        obj.TryGetDouble("pi", out var value).Should().BeTrue();
        value.Should().Be(3.14159);
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
    public void Create_WithDuplicateKeys_ShouldUseLastValue()
    {
        var obj = MetadataObject.Create(("a", 1), ("a", 2));

        // With insertion order and linear search, the first occurrence is found
        // Duplicate keys are allowed but only the first value is accessible via lookup
        obj.TryGetInt64("a", out var value).Should().BeTrue();
        value.Should().Be(1L);
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
        // More than DictionaryThreshold (10) entries to trigger dictionary lookup
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
            ("key9", 9),
            ("key10", 10)
        );

        var result = obj.TryGetValue("key5", out var value);

        result.Should().BeTrue();
        value.Should().Be(MetadataValue.FromInt64(5));
    }

    [Fact]
    public void TryGetValue_WithLargeObject_MissingKey_ShouldReturnFalse()
    {
        // More than DictionaryThreshold (10) entries to trigger dictionary lookup
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
            ("key9", 9),
            ("key10", 10)
        );

        var result = obj.TryGetValue("missing", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void ContainsKey_WithLargeObject_ShouldUseDictionaryLookup()
    {
        // More than DictionaryThreshold (10) entries to trigger dictionary lookup
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
            ("key9", 9),
            ("key10", 10)
        );

        obj.ContainsKey("key7").Should().BeTrue();
        obj.ContainsKey("missing").Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_WithLargeObjectAndKeyComparer_ShouldUseDictionaryLookup()
    {
        // More than DictionaryThreshold (10) entries with key comparer to trigger dictionary lookup
        var obj = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key0", 0),
            ("Key1", 1),
            ("Key2", 2),
            ("Key3", 3),
            ("Key4", 4),
            ("Key5", 5),
            ("Key6", 6),
            ("Key7", 7),
            ("Key8", 8),
            ("Key9", 9),
            ("Key10", 10)
        );

        // Should find keys case-insensitively
        obj.TryGetValue("KEY5", out var value).Should().BeTrue();
        value.Should().Be(MetadataValue.FromInt64(5));
        obj.TryGetValue("key10", out var value2).Should().BeTrue();
        value2.Should().Be(MetadataValue.FromInt64(10));
    }

    [Fact]
    public void TryGetValue_WithLargeObjectAndKeyComparer_MissingKey_ShouldReturnFalse()
    {
        // More than DictionaryThreshold (10) entries with key comparer
        var obj = MetadataObject.Create(
            StringComparer.OrdinalIgnoreCase,
            ("Key0", 0),
            ("Key1", 1),
            ("Key2", 2),
            ("Key3", 3),
            ("Key4", 4),
            ("Key5", 5),
            ("Key6", 6),
            ("Key7", 7),
            ("Key8", 8),
            ("Key9", 9),
            ("Key10", 10)
        );

        obj.TryGetValue("missing", out var value).Should().BeFalse();
        value.Should().Be(default(MetadataValue));
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

    [Fact]
    public void Builder_Create_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.Create();
        var obj = builder.Build();

        obj.Should().BeEmpty();
    }

    [Fact]
    public void Builder_Create_WithCapacity_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 10);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Builder_Create_WithZeroCapacity_ShouldThrow()
    {
        var act = () => _ = MetadataObjectBuilder.Create(capacity: 0);

        act.Should().Throw<ArgumentOutOfRangeException>().Where(x => x.ParamName == "capacity");
    }

    [Fact]
    public void Builder_From_ShouldCopyExistingObject()
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
    public void Builder_From_WithEmptyObject_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.From(MetadataObject.Empty);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Builder_From_WithDefaultObject_ShouldCreateEmptyBuilder()
    {
        using var builder = MetadataObjectBuilder.From(default);

        builder.Count.Should().Be(0);
    }

    [Fact]
    public void Builder_Add_ShouldAccumulateProperties()
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
    public void Builder_Add_ShouldPreserveInsertionOrder()
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

        keys.Should().Equal("z", "a", "m");
    }

    [Fact]
    public void Builder_Add_WithDuplicateKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Add("key", 2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Builder_Add_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = () => builder.Add(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Builder_Add_ManyEntries_ShouldExpandCapacity()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 2);

        for (var i = 0; i < 10; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.Count.Should().Be(10);
    }

    [Fact]
    public void Builder_Add_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.Add("key", 1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Builder_Replace_ShouldUpdateExistingKey()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        builder.Replace("key", 2);

        builder.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var intValue).Should().BeTrue();
        intValue.Should().Be(2L);
    }

    [Fact]
    public void Builder_Replace_MissingKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Replace("missing", 1);

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void Builder_Replace_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Replace(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Builder_Replace_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);
        builder.Build();

        var act = () => builder.Replace("key", 2);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Builder_AddOrReplace_ExistingKey_ShouldUpdateValue()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);
        builder.AddOrReplace("key", 2);

        var obj = builder.Build();

        obj.TryGetInt64("key", out var value).Should().BeTrue();
        value.Should().Be(2L);
    }

    [Fact]
    public void Builder_AddOrReplace_NewKey_ShouldAppendEntry()
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

        // New keys are appended at the end, preserving insertion order
        keys.Should().Equal("a", "z", "m");
    }

    [Fact]
    public void Builder_AddOrReplace_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.AddOrReplace(null!, 1);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Builder_AddOrReplace_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.AddOrReplace("key", 1);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Builder_TryGetValue_ShouldFindExistingKey()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 42);

        builder.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(42L);
    }

    [Fact]
    public void Builder_TryGetValue_MissingKey_ShouldReturnFalse()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        var result = builder.TryGetValue("missing", out var value);

        result.Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void Builder_TryGetValue_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.TryGetValue(null!, out _);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Builder_ContainsKey_ShouldReturnCorrectResult()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.Add("exists", 1);

        builder.ContainsKey("exists").Should().BeTrue();
        builder.ContainsKey("missing").Should().BeFalse();
    }

    [Fact]
    public void Builder_ContainsKey_WithNullKey_ShouldThrow()
    {
        using var builder = MetadataObjectBuilder.Create();

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.ContainsKey(null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("key");
    }

    [Fact]
    public void Builder_Build_CalledTwice_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Builder_Dispose_WithoutBuild_ShouldNotThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Add("key", 1);

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Builder_Dispose_CalledTwice_ShouldNotThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Dispose();

        var act = () => builder.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Builder_SetKeyComparer_ShouldAffectKeyLookup()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("Key", "value");

        builder.ContainsKey("KEY").Should().BeTrue();
        builder.ContainsKey("key").Should().BeTrue();
    }

    [Fact]
    public void Builder_SetKeyComparer_AfterBuild_ShouldThrow()
    {
        var builder = MetadataObjectBuilder.Create();
        builder.Build();

        var act = () => builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*already been used*");
    }

    [Fact]
    public void Builder_Add_WithKeyComparer_ShouldDetectDuplicatesCaseInsensitively()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("Key", 1);

        // ReSharper disable once AccessToDisposedClosure -- thats fine, act is called before disposal
        var act = () => builder.Add("KEY", 2);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Builder_TryGetValue_WithKeyComparer_ShouldFindCaseInsensitively()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("MyKey", 42);

        builder.TryGetValue("MYKEY", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(42L);
    }

    [Fact]
    public void Builder_Replace_WithKeyComparer_ShouldFindCaseInsensitively()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("Key", 1);

        builder.Replace("KEY", 2);

        builder.TryGetValue("key", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(2L);
    }

    [Fact]
    public void Builder_AddOrReplace_WithKeyComparer_ShouldReplaceCaseInsensitively()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("Key", 1);

        builder.AddOrReplace("KEY", 2);

        var obj = builder.Build();
        obj.TryGetInt64("key", out var value).Should().BeTrue();
        value.Should().Be(2L);
    }

    [Fact]
    public void Builder_Add_ManyEntriesWithKeyComparer_ShouldUseDictionaryLookup()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);

        // Add more than DictionaryThreshold (10) entries to trigger dictionary lookup
        for (var i = 0; i < 15; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.ContainsKey("KEY10").Should().BeTrue();
        builder.ContainsKey("KEY14").Should().BeTrue();
        builder.ContainsKey("KEY15").Should().BeFalse();
    }

    [Fact]
    public void Builder_TryGetValue_WithManyEntries_ShouldUseDictionaryLookup()
    {
        using var builder = MetadataObjectBuilder.Create();

        for (var i = 0; i < 15; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.TryGetValue("key12", out var value).Should().BeTrue();
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(12L);
    }

    [Fact]
    public void Builder_TryGetValue_WithManyEntries_MissingKey_ShouldReturnFalse()
    {
        using var builder = MetadataObjectBuilder.Create();

        for (var i = 0; i < 15; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.TryGetValue("missing", out var value).Should().BeFalse();
        value.Should().Be(default(MetadataValue));
    }

    [Fact]
    public void Builder_AddOrReplace_WithManyEntries_ShouldUpdateDictionary()
    {
        using var builder = MetadataObjectBuilder.Create();

        for (var i = 0; i < 15; i++)
        {
            builder.Add($"key{i}", i);
        }

        // Force dictionary creation by lookup
        builder.ContainsKey("key10");

        // Now add new key - should update the dictionary
        builder.AddOrReplace("newKey", 100);

        builder.ContainsKey("newKey").Should().BeTrue();
    }

    [Fact]
    public void Builder_EnsureCapacity_WithVeryLargeCapacity_ShouldWork()
    {
        using var builder = MetadataObjectBuilder.Create(capacity: 2);

        for (var i = 0; i < 100; i++)
        {
            builder.Add($"key{i}", i);
        }

        builder.Count.Should().Be(100);

        var obj = builder.Build();
        obj.Should().HaveCount(100);
    }

    [Fact]
    public void Builder_Add_OnDefaultBuilder_ShouldAllocateBuffer()
    {
        // Using a default builder (not created via Create) exercises the null entries path in EnsureCapacity
        using var builder = default(MetadataObjectBuilder);
        builder.Add("key1", 1);
        builder.Add("key2", 2);

        builder.Count.Should().Be(2);
    }

    [Fact]
    public void Builder_Build_WithKeyComparer_ShouldPreserveComparer()
    {
        using var builder = MetadataObjectBuilder.Create();
        builder.SetKeyComparer(StringComparer.OrdinalIgnoreCase);
        builder.Add("KeyName", "value");

        var obj = builder.Build();

        obj.TryGetValue("KEYNAME", out var value).Should().BeTrue();
        value.TryGetString(out var str).Should().BeTrue();
        str.Should().Be("value");
    }
}
