using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Metadata;

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
    public void FromDecimal_ShouldStoreAsString()
    {
        var input = 123.456m;
        var value = MetadataValue.FromDecimal(input);

        value.Kind.Should().Be(MetadataKind.String);
        value.TryGetString(out var str).Should().BeTrue();
        str.Should().Be("123.456");
    }

    [Fact]
    public void TryGetDecimal_ShouldParseDecimalString()
    {
        var value = MetadataValue.FromDecimal(99.99m);

        value.TryGetDecimal(out var result).Should().BeTrue();
        result.Should().Be(99.99m);
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
}
