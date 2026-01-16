using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

public sealed class MetadataArrayAdditionalTests
{
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
    public void Indexer_OutOfRange_ShouldThrow()
    {
        var array = MetadataArray.Create(1, 2);

        var act = () => array[5];

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
}
