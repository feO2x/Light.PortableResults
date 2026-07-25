using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

public sealed class MetadataValueTests
{
    [Fact]
    public void Null_ShouldHaveNullKind()
    {
        var value = MetadataValue.Null;

        value.Kind.Should().Be(MetadataKind.Null);
        value.IsNull.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromBoolean_ShouldStoreValue(bool input)
    {
        var value = MetadataValue.FromBoolean(input);

        value.Kind.Should().Be(MetadataKind.Boolean);
        value.TryGetBoolean(out var result).Should().BeTrue();
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(-100L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void FromInt64_ShouldStoreValue(long input)
    {
        var value = MetadataValue.FromInt64(input);

        value.Kind.Should().Be(MetadataKind.Int64);
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.14159)]
    [InlineData(-273.15)]
    public void FromDouble_ShouldStoreValue(double input)
    {
        var value = MetadataValue.FromDouble(input);

        value.Kind.Should().Be(MetadataKind.Double);
        value.TryGetDouble(out var result).Should().BeTrue();
        result.Should().Be(input);
    }

    [Fact]
    public void FromDouble_ShouldRejectNaN()
    {
        var act = () => MetadataValue.FromDouble(double.NaN);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromDouble_ShouldRejectPositiveInfinity()
    {
        var act = () => MetadataValue.FromDouble(double.PositiveInfinity);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromDouble_ShouldRejectNegativeInfinity()
    {
        var act = () => MetadataValue.FromDouble(double.NegativeInfinity);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("unicode: 日本語")]
    public void FromString_ShouldStoreValue(string input)
    {
        var value = MetadataValue.FromString(input);

        value.Kind.Should().Be(MetadataKind.String);
        value.TryGetString(out var result).Should().BeTrue();
        result.Should().Be(input);
    }

    [Fact]
    public void FromString_NullShouldReturnNullValue()
    {
        var value = MetadataValue.FromString(null);

        value.Kind.Should().Be(MetadataKind.Null);
        value.IsNull.Should().BeTrue();
    }

    [Fact]
    public void FromDecimal_ShouldStoreAsDecimal()
    {
        var input = 123.456m;
        var value = MetadataValue.FromDecimal(input);

        value.Kind.Should().Be(MetadataKind.Decimal);
        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(input);
    }

    [Fact]
    public void FromDecimal_ShouldPreserveScale()
    {
        var value = MetadataValue.FromDecimal(19.50m);

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.ToString(CultureInfo.InvariantCulture).Should().Be("19.50");
    }

    [Fact]
    public void TryGetString_ShouldReturnFalse_ForDecimal()
    {
        var value = MetadataValue.FromDecimal(99.99m);

        value.TryGetString(out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetDecimal_ShouldReturnStoredValue()
    {
        var value = MetadataValue.FromDecimal(99.99m);

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(99.99m);
    }

    [Fact]
    public void TryGetDecimal_ShouldParseNumericString()
    {
        var value = MetadataValue.FromString("99.99");

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(99.99m);
    }

    [Fact]
    public void TryGetDecimal_ShouldReturnFalse_ForNonNumericString()
    {
        var value = MetadataValue.FromString("not a number");

        value.TryGetDecimal(out var result).Should().BeFalse();
        result.Should().Be(0m);
    }

    [Fact]
    public void TryGetDecimal_ShouldConvertFromInt64()
    {
        var value = MetadataValue.FromInt64(42);

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(42m);
    }

    [Fact]
    public void TryGetDecimal_ShouldConvertFromDouble()
    {
        var value = MetadataValue.FromDouble(3.5);

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(3.5m);
    }

    [Fact]
    public void ImplicitConversion_FromInt_ShouldWork()
    {
        MetadataValue value = 42;

        value.Kind.Should().Be(MetadataKind.Int64);
        value.TryGetInt64(out var result).Should().BeTrue();
        result.Should().Be(42L);
    }

    [Fact]
    public void ImplicitConversion_FromBool_ShouldWork()
    {
        MetadataValue value = true;

        value.Kind.Should().Be(MetadataKind.Boolean);
        value.TryGetBoolean(out var result).Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_FromString_ShouldWork()
    {
        MetadataValue value = "test";

        value.Kind.Should().Be(MetadataKind.String);
        value.TryGetString(out var result).Should().BeTrue();
        result.Should().Be("test");
    }

    [Fact]
    public void ImplicitConversion_FromFloat_ShouldWork()
    {
        MetadataValue value = 3.14f;

        value.Kind.Should().Be(MetadataKind.Double);
        value.TryGetDouble(out var result).Should().BeTrue();
        result.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void ImplicitConversion_FromDouble_ShouldWork()
    {
        MetadataValue value = 3.14159;

        value.Kind.Should().Be(MetadataKind.Double);
        value.TryGetDouble(out var result).Should().BeTrue();
        result.Should().Be(3.14159);
    }

    [Fact]
    public void ImplicitConversion_FromDecimal_ShouldWork()
    {
        MetadataValue value = 123.456m;

        value.Kind.Should().Be(MetadataKind.Decimal);
        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(123.456m);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = MetadataValue.FromInt64(42);
        var b = MetadataValue.FromInt64(42);

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var a = MetadataValue.FromInt64(42);
        var b = MetadataValue.FromInt64(43);

        a.Should().NotBe(b);
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentKinds_ShouldNotBeEqual()
    {
        var a = MetadataValue.FromInt64(42);
        var b = MetadataValue.FromDouble(42.0);

        a.Should().NotBe(b);
    }

    [Fact]
    public void ToString_ShouldReturnReadableRepresentation()
    {
        MetadataValue.Null.ToString().Should().Be("null");
        MetadataValue.FromBoolean(true).ToString().Should().Be("true");
        MetadataValue.FromBoolean(false).ToString().Should().Be("false");
        MetadataValue.FromInt64(42).ToString().Should().Be("42");
        MetadataValue.FromString("hello").ToString().Should().Be("\"hello\"");
    }

    [Fact]
    public void AsArray_WhenArray_ShouldReturnArray()
    {
        var array = MetadataArray.Create(1, 2, 3);
        var value = MetadataValue.FromArray(array);

        var result = value.AsArray();

        result.Count.Should().Be(3);
    }

    [Fact]
    public void AsArray_WhenNotArray_ShouldThrow()
    {
        var value = MetadataValue.FromInt64(42);

        var act = () => value.AsArray();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Int64*Array*");
    }

    [Fact]
    public void AsObject_WhenObject_ShouldReturnObject()
    {
        var obj = MetadataObject.Create(("key", "value"));
        var value = MetadataValue.FromObject(obj);

        var result = value.AsObject();

        result.Count.Should().Be(1);
    }

    [Fact]
    public void AsObject_WhenNotObject_ShouldThrow()
    {
        var value = MetadataValue.FromString("test");

        var act = () => value.AsObject();

        act.Should().Throw<InvalidOperationException>().WithMessage("*String*Object*");
    }

    [Fact]
    public void GetHashCode_Null_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.Null;
        var value2 = MetadataValue.Null;

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Boolean_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.FromBoolean(true);
        var value2 = MetadataValue.FromBoolean(true);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Int64_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.FromInt64(42);
        var value2 = MetadataValue.FromInt64(42);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Double_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.FromDouble(3.14);
        var value2 = MetadataValue.FromDouble(3.14);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_String_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.FromString("test");
        var value2 = MetadataValue.FromString("test");

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Array_ShouldReturnConsistentValue()
    {
        var array = MetadataArray.Create(1, 2);
        var value1 = MetadataValue.FromArray(array);
        var value2 = MetadataValue.FromArray(array);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Object_ShouldReturnConsistentValue()
    {
        var obj = MetadataObject.Create(("k", "v"));
        var value1 = MetadataValue.FromObject(obj);
        var value2 = MetadataValue.FromObject(obj);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void ToString_Null_ShouldReturnNull()
    {
        var value = MetadataValue.Null;

        value.ToString().Should().Be("null");
    }

    [Fact]
    public void ToString_Boolean_True_ShouldReturnTrue()
    {
        var value = MetadataValue.FromBoolean(true);

        value.ToString().Should().Be("true");
    }

    [Fact]
    public void ToString_Boolean_False_ShouldReturnFalse()
    {
        var value = MetadataValue.FromBoolean(false);

        value.ToString().Should().Be("false");
    }

    [Fact]
    public void ToString_Int64_ShouldReturnNumber()
    {
        var value = MetadataValue.FromInt64(42);

        value.ToString().Should().Be("42");
    }

    [Fact]
    public void ToString_Double_ShouldReturnNumber()
    {
        var value = MetadataValue.FromDouble(3.14);

        value.ToString().Should().Be("3.14");
    }

    [Fact]
    public void ToString_String_ShouldReturnQuotedString()
    {
        var value = MetadataValue.FromString("hello");

        value.ToString().Should().Be("\"hello\"");
    }

    [Fact]
    public void ToString_Array_ShouldReturnArrayContents()
    {
        var value = MetadataValue.FromArray(MetadataArray.Create(1, 2));

        value.ToString().Should().Be("[1, 2]");
    }

    [Fact]
    public void ToString_Object_ShouldReturnBraces()
    {
        var value = MetadataValue.FromObject(MetadataObject.Create(("k", "v")));

        value.ToString().Should().Be("{...}");
    }

    [Fact]
    public void Equals_Object_WithMetadataValue_ShouldReturnTrue()
    {
        var value1 = MetadataValue.FromInt64(42);
        object value2 = MetadataValue.FromInt64(42);

        value1.Equals(value2).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_WithNonMetadataValue_ShouldReturnFalse()
    {
        var value = MetadataValue.FromInt64(42);

        value.Equals("not a metadata value").Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_WithNull_ShouldReturnFalse()
    {
        var value = MetadataValue.FromInt64(42);

        value.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void NotEqualOperator_ShouldReturnTrueForDifferentValues()
    {
        var value1 = MetadataValue.FromInt64(1);
        var value2 = MetadataValue.FromInt64(2);

        (value1 != value2).Should().BeTrue();
    }

    [Fact]
    public void TryGetArray_WhenNotArray_ShouldReturnFalse()
    {
        var value = MetadataValue.FromInt64(42);

        var result = value.TryGetArray(out var array);

        result.Should().BeFalse();
        array.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetObject_WhenNotObject_ShouldReturnFalse()
    {
        var value = MetadataValue.FromInt64(42);

        var result = value.TryGetObject(out var obj);

        result.Should().BeFalse();
        obj.Count.Should().Be(0);
    }

    [Fact]
    public void TryGetDecimal_FromDouble_WithOverflow_ShouldReturnFalse()
    {
        var value = MetadataValue.FromDouble(double.MaxValue);

        var result = value.TryGetDecimal(out var dec);

        result.Should().BeFalse();
        dec.Should().Be(0);
    }

    [Fact]
    public void TryGetDecimal_FromInt64_ShouldSucceed()
    {
        var value = MetadataValue.FromInt64(42);

        var result = value.TryGetDecimal(out var dec);

        result.Should().BeTrue();
        dec.Should().Be(42m);
    }

    [Fact]
    public void TryGetDecimal_FromNull_ShouldReturnFalse()
    {
        var value = MetadataValue.Null;

        var result = value.TryGetDecimal(out var dec);

        result.Should().BeFalse();
        dec.Should().Be(0);
    }

    [Fact]
    public void Equals_DifferentKinds_ShouldReturnFalse()
    {
        var int64Value = MetadataValue.FromInt64(1);
        var doubleValue = MetadataValue.FromDouble(1.0);

        int64Value.Equals(doubleValue).Should().BeFalse();
    }

    [Fact]
    public void Equals_Doubles_ShouldCompareCorrectly()
    {
        var value1 = MetadataValue.FromDouble(3.14);
        var value2 = MetadataValue.FromDouble(3.14);
        var value3 = MetadataValue.FromDouble(2.71);

        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeFalse();
    }

    [Fact]
    public void Equals_Strings_ShouldCompareCorrectly()
    {
        var value1 = MetadataValue.FromString("test");
        var value2 = MetadataValue.FromString("test");
        var value3 = MetadataValue.FromString("other");

        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeFalse();
    }

    [Fact]
    public void Equals_Arrays_ShouldCompareByValue()
    {
        var array1 = MetadataArray.Create(1, 2);
        var array2 = MetadataArray.Create(1, 2);
        var value1 = MetadataValue.FromArray(array1);
        var value2 = MetadataValue.FromArray(array1);
        var value3 = MetadataValue.FromArray(array2);

        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeTrue();
    }

    [Fact]
    public void Equals_Objects_ShouldCompareByValue()
    {
        var obj1 = MetadataObject.Create(("k", "v"));
        var obj2 = MetadataObject.Create(("k", "v"));
        var value1 = MetadataValue.FromObject(obj1);
        var value2 = MetadataValue.FromObject(obj1);
        var value3 = MetadataValue.FromObject(obj2);

        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeTrue();
    }

    [Fact]
    public void Equals_Decimals_ShouldCompareByValue()
    {
        var value1 = MetadataValue.FromDecimal(19.99m);
        var value2 = MetadataValue.FromDecimal(19.99m);
        var value3 = MetadataValue.FromDecimal(24.99m);

        value1.Equals(value2).Should().BeTrue();
        value1.Equals(value3).Should().BeFalse();
    }

    [Fact]
    public void Equals_Decimals_ShouldIgnoreScale()
    {
        var value1 = MetadataValue.FromDecimal(19.50m);
        var value2 = MetadataValue.FromDecimal(19.5m);

        value1.Equals(value2).Should().BeTrue();
        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void Equals_DecimalAndDouble_ShouldNotBeEqual()
    {
        var decimalValue = MetadataValue.FromDecimal(19.5m);
        var doubleValue = MetadataValue.FromDouble(19.5);

        decimalValue.Equals(doubleValue).Should().BeFalse();
    }

    // FromDecimal is the only way to create a decimal value, thus a decimal kind without a boxed decimal in the
    // payload is unreachable through the public API. Equals and ToString still handle it, because the dispatch
    // sites are the last safety net for a kind that is only half-implemented.
    [Fact]
    public void Equals_Decimal_ShouldReturnFalse_ForEmptyPayload()
    {
        var wellFormed = MetadataValue.FromDecimal(19.99m);
        var malformed = MetadataValueTestFactory.CreateWithEmptyPayload(MetadataKind.Decimal);

        wellFormed.Equals(malformed).Should().BeFalse();
        malformed.Equals(wellFormed).Should().BeFalse();
        malformed.Equals(malformed).Should().BeFalse();
    }

    [Fact]
    public void ToString_Decimal_ShouldThrow_ForEmptyPayload()
    {
        var malformed = MetadataValueTestFactory.CreateWithEmptyPayload(MetadataKind.Decimal);

        var act = () => malformed.ToString();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetHashCode_Decimal_ShouldReturnConsistentValue()
    {
        var value1 = MetadataValue.FromDecimal(19.99m);
        var value2 = MetadataValue.FromDecimal(19.99m);

        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void ToString_Decimal_ShouldReturnUnquotedNumber()
    {
        var value = MetadataValue.FromDecimal(19.50m);

        value.ToString().Should().Be("19.50");
    }

    [Fact]
    public void ToString_Decimal_ShouldUseInvariantCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            MetadataValue.FromDecimal(1234.56m).ToString().Should().Be("1234.56");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Decimal_ShouldBeClassifiedAsPrimitive()
    {
        MetadataKind.Decimal.IsPrimitive().Should().BeTrue();
    }

    // Decimals are stored boxed in the reference slot of the payload precisely so that MetadataValue does not
    // grow: array and object elements are stored inline, thus every additional byte multiplies. This test pins
    // the size to the value it had before MetadataKind.Decimal was introduced.
    //
    // The pinned value describes a 64-bit process: the payload holds 8 bytes of primitives plus a pointer-sized
    // reference slot. On a 32-bit runtime the reference slot shrinks and the expected value is a different one,
    // so the bitness is asserted first - a failure there means the pin needs a second case, not a new number.
    [Fact]
    public void MetadataValue_ShouldNotExceedItsPinnedSize()
    {
        Environment.Is64BitProcess.Should().BeTrue("the pinned size describes a 64-bit process");

        Unsafe.SizeOf<MetadataValue>().Should().Be(24);
    }
}
