using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataArrayTests
{
    [Fact]
    public void Empty_ShouldHaveZeroCount()
    {
        var array = MetadataArray.Empty;

        array.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValues_ShouldStoreAll()
    {
        var array = MetadataArray.Create(1, 2, 3);

        array.Should().HaveCount(3);
        array[0].Should().Be(MetadataValue.FromInt64(1));
        array[1].Should().Be(MetadataValue.FromInt64(2));
        array[2].Should().Be(MetadataValue.FromInt64(3));
    }

    [Fact]
    public void Create_WithNull_ShouldReturnEmpty()
    {
        var array = MetadataArray.Create(null!);

        array.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyArray_ShouldReturnEmpty()
    {
        var array = MetadataArray.Create();

        array.Should().BeEmpty();
    }

    [Fact]
    public void Indexer_OutOfRange_ShouldThrow()
    {
        var array = MetadataArray.Create(1, 2);

        var act1 = () => array[2];
        var act2 = () => array[-1];

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Enumeration_ShouldIterateAllValues()
    {
        var array = MetadataArray.Create("a", "b", "c");
        var count = 0;

        foreach (var item in array)
        {
            count++;
            item.Kind.Should().Be(MetadataKind.String);
        }

        count.Should().Be(3);
    }

    [Fact]
    public void AsSpan_ShouldReturnAllValues()
    {
        var array = MetadataArray.Create(10, 20, 30);
        var span = array.AsSpan();

        span.Length.Should().Be(3);
        span[0].TryGetInt64(out var v0).Should().BeTrue();
        v0.Should().Be(10);
        span[1].TryGetInt64(out var v1).Should().BeTrue();
        v1.Should().Be(20);
        span[2].TryGetInt64(out var v2).Should().BeTrue();
        v2.Should().Be(30);
    }

    [Fact]
    public void ImplicitConversion_ToMetadataValue_ShouldWork()
    {
        var array = MetadataArray.Create(1, 2, 3);
        MetadataValue value = array;

        value.Kind.Should().Be(MetadataKind.Array);
        value.TryGetArray(out var result).Should().BeTrue();
        result.Should().HaveCount(3);
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_WithOnlyPrimitives_ShouldReturnTrue()
    {
        var array = MetadataArray.Create(1, "string", true, 3.14);

        array.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_WithNestedArray_ShouldReturnFalse()
    {
        var nested = MetadataArray.Create(1, 2);
        var array = MetadataArray.Create(1, nested);

        array.HasOnlyPrimitiveChildren.Should().BeFalse();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_WithNestedObject_ShouldReturnFalse()
    {
        var nested = MetadataObject.Create(("key", "value"));
        var array = MetadataArray.Create(1, nested);

        array.HasOnlyPrimitiveChildren.Should().BeFalse();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_OnEmptyArray_ShouldReturnTrue()
    {
        var array = MetadataArray.Empty;

        array.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_OnDefaultArray_ShouldReturnTrue()
    {
        var array = default(MetadataArray);

        array.HasOnlyPrimitiveChildren.Should().BeTrue();
    }

    [Fact]
    public void HasOnlyPrimitiveChildren_IsCached()
    {
        var array = MetadataArray.Create(1, 2, 3);

        // Call twice to exercise caching
        var result1 = array.HasOnlyPrimitiveChildren;
        var result2 = array.HasOnlyPrimitiveChildren;

        result1.Should().Be(result2).And.BeTrue();
    }

    [Fact]
    public void ToString_WithZeroElements_ShouldReturnEmptyArray()
    {
        var array = MetadataArray.Empty;

        array.ToString().Should().Be("[]");
    }

    [Fact]
    public void ToString_WithOneElement_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(42);

        array.ToString().Should().Be("[42]");
    }

    [Fact]
    public void ToString_WithTwoElements_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(1, 2);

        array.ToString().Should().Be("[1, 2]");
    }

    [Fact]
    public void ToString_WithThreeElements_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(1, 2, 3);

        array.ToString().Should().Be("[1, 2, 3]");
    }

    [Fact]
    public void ToString_WithFourElements_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(1, 2, 3, 4);

        array.ToString().Should().Be("[1, 2, 3, 4]");
    }

    [Fact]
    public void ToString_WithFiveElements_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(1, 2, 3, 4, 5);

        array.ToString().Should().Be("[1, 2, 3, 4, 5]");
    }

    [Fact]
    public void ToString_WithSixOrMoreElements_ShouldUseJoin()
    {
        var array = MetadataArray.Create(1, 2, 3, 4, 5, 6);

        array.ToString().Should().Be("[1, 2, 3, 4, 5, 6]");
    }

    [Fact]
    public void ToString_WithManyElements_ShouldFormatCorrectly()
    {
        var array = MetadataArray.Create(1, 2, 3, 4, 5, 6, 7, 8);

        var result = array.ToString();

        result.Should().StartWith("[1");
        result.Should().EndWith("8]");
    }

    [Fact]
    public void ToString_IsCached()
    {
        var array = MetadataArray.Create(1, 2, 3);

        var result1 = array.ToString();
        var result2 = array.ToString();

        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public void ToString_OnDefaultArray_ShouldReturnEmptyArray()
    {
        var array = default(MetadataArray);

        array.ToString().Should().Be("[]");
    }

    [Fact]
    public void Enumerator_Reset_ShouldResetToBeginning()
    {
        var array = MetadataArray.Create(1, 2, 3);
        using var enumerator = array.GetEnumerator();

        enumerator.MoveNext();
        enumerator.MoveNext();
        enumerator.Reset();
        enumerator.MoveNext();

        enumerator.Current.Should().Be(MetadataValue.FromInt64(1));
    }

    [Fact]
    public void Enumerator_Current_ViaIEnumerator_ShouldReturnBoxedValue()
    {
        var array = MetadataArray.Create(42);
        IEnumerator enumerator = array.GetEnumerator();

        enumerator.MoveNext();

        enumerator.Current.Should().Be(MetadataValue.FromInt64(42));
        (enumerator as IDisposable)?.Dispose();
    }

    [Fact]
    public void Enumerator_Dispose_ShouldNotThrow()
    {
        var array = MetadataArray.Create(1, 2);
        var enumerator = array.GetEnumerator();

        var act = () => enumerator.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Enumerator_OnEmptyArray_MoveNext_ShouldReturnFalse()
    {
        var array = MetadataArray.Empty;
        using var enumerator = array.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void Enumerator_OnDefaultArray_MoveNext_ShouldReturnFalse()
    {
        var array = default(MetadataArray);
        using var enumerator = array.GetEnumerator();

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void IEnumerableGeneric_GetEnumerator_ShouldWork()
    {
        var array = MetadataArray.Create(1, 2, 3);
        IEnumerable<MetadataValue> enumerable = array;

        var list = new List<MetadataValue>();
        foreach (var item in enumerable)
        {
            list.Add(item);
        }

        list.Should().HaveCount(3);
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ShouldWork()
    {
        var array = MetadataArray.Create(1, 2);
        IEnumerable enumerable = array;

        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        count.Should().Be(2);
    }

    [Fact]
    public void Indexer_OnDefaultArray_ShouldThrow()
    {
        var array = default(MetadataArray);

        var act = () => array[0];

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NotEqualOperator_ShouldReturnTrueForDifferentArrays()
    {
        var array1 = MetadataArray.Create(1);
        var array2 = MetadataArray.Create(2);

        (array1 != array2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithMetadataArray_ShouldReturnTrue()
    {
        var array = MetadataArray.Create(1, 2);
        object boxed = array;

        array.Equals(boxed).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithNonMetadataArray_ShouldReturnFalse()
    {
        var array = MetadataArray.Create(1, 2);

        // ReSharper disable once SuspiciousTypeConversion.Global -- OK in this test scenario
        array.Equals("not an array").Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WithNull_ShouldReturnFalse()
    {
        var array = MetadataArray.Create(1, 2);

        array.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_DefaultArray_ShouldReturnZero()
    {
        var array = default(MetadataArray);

        array.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void GetHashCode_SameArray_ShouldReturnConsistentValue()
    {
        var array = MetadataArray.Create(1, 2, 3);

        var hash1 = array.GetHashCode();
        var hash2 = array.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Count_OnDefaultArray_ShouldReturnZero()
    {
        var array = default(MetadataArray);

        array.Count.Should().Be(0);
    }

    [Fact]
    public void AsSpan_OnDefaultArray_ShouldReturnEmptySpan()
    {
        var array = default(MetadataArray);

        array.AsSpan().Length.Should().Be(0);
    }

    [Fact]
    public void AsSpan_ShouldReturnCorrectValues()
    {
        var array = MetadataArray.Create(1, 2, 3);

        var span = array.AsSpan();

        span.Length.Should().Be(3);
        span[0].Should().Be(MetadataValue.FromInt64(1));
        span[1].Should().Be(MetadataValue.FromInt64(2));
        span[2].Should().Be(MetadataValue.FromInt64(3));
    }

    [Fact]
    public void GetHashCode_WithMultipleValues_ShouldReturnConsistentValue()
    {
        var array = MetadataArray.Create(1, 2, 3, 4, 5);

        var hash1 = array.GetHashCode();
        var hash2 = array.GetHashCode();

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GetHashCode_EqualArrays_ShouldReturnSameHash()
    {
        var array1 = MetadataArray.Create(1, 2, 3);
        var array2 = MetadataArray.Create(1, 2, 3);

        array1.GetHashCode().Should().Be(array2.GetHashCode());
    }

    [Fact]
    public void Indexer_WithNegativeIndex_ShouldThrow()
    {
        var array = MetadataArray.Create(1, 2, 3);

        var act = () => array[-1];

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("index");
    }

    [Fact]
    public void Equals_WithDifferentLengths_ShouldReturnFalse()
    {
        var array1 = MetadataArray.Create(1, 2);
        var array2 = MetadataArray.Create(1, 2, 3);

        array1.Equals(array2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        var array1 = MetadataArray.Create(1, 2, 3);
        var array2 = MetadataArray.Create(1, 2, 99);

        array1.Equals(array2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NonEmptyWithDefault_ShouldReturnFalse()
    {
        var array1 = MetadataArray.Create(1, 2, 3);
        var array2 = default(MetadataArray);

        array1.Equals(array2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DefaultWithNonEmpty_ShouldReturnFalse()
    {
        var array1 = default(MetadataArray);
        var array2 = MetadataArray.Create(1, 2, 3);

        array1.Equals(array2).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothDefault_ShouldReturnTrue()
    {
        var array1 = default(MetadataArray);
        var array2 = default(MetadataArray);

        array1.Equals(array2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_NonEmptyWithDefault_ShouldReturnFalse()
    {
        var array1 = MetadataArray.Create(1);
        var array2 = default(MetadataArray);

        (array1 == array2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothDefault_ShouldReturnTrue()
    {
        var array1 = default(MetadataArray);
        var array2 = default(MetadataArray);

        (array1 == array2).Should().BeTrue();
    }
}
