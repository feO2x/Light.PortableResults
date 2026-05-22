using System;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Validation.OpenApi.Tests;

// These marker attributes are never instantiated at runtime - they are consumed by the source generator at
// compile time. The unit tests below exercise their guard clauses and property assignments so that the
// hand-written guard logic stays covered and correct.
public sealed class ValidationOpenApiAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GeneratePortableValidationOpenApi_ShouldExposeAllowUnknownErrorCodes(string? _)
    {
        var attribute = new GeneratePortableValidationOpenApiAttribute { AllowUnknownErrorCodes = true };

        attribute.AllowUnknownErrorCodes.Should().BeTrue();
    }

    [Fact]
    public void ErrorHint_ShouldCaptureCodeOnly()
    {
        var attribute = new PortableValidationOpenApiErrorHintAttribute("NotEmpty");

        (attribute.Code, attribute.MetadataType).Should().Be(("NotEmpty", null));
    }

    [Fact]
    public void ErrorHint_ShouldCaptureCodeAndMetadataType()
    {
        var attribute = new PortableValidationOpenApiErrorHintAttribute("NotEmpty", typeof(MetadataSample));

        (attribute.Code, attribute.MetadataType).Should().Be(("NotEmpty", typeof(MetadataSample)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ErrorHint_ShouldRejectMissingCode(string? code)
    {
        var act = () => new PortableValidationOpenApiErrorHintAttribute(code!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ErrorHint_ShouldRejectNullMetadataType()
    {
        var act = () => new PortableValidationOpenApiErrorHintAttribute("NotEmpty", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExampleHint_ShouldCaptureCodeTargetAndMessage()
    {
        var attribute = new PortableValidationOpenApiExampleHintAttribute("NotEmpty")
        {
            Target = "name",
            Message = "name must not be empty"
        };

        (attribute.Code, attribute.Target, attribute.Message)
           .Should()
           .Be(("NotEmpty", "name", "name must not be empty"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExampleHint_ShouldRejectMissingCode(string? code)
    {
        var act = () => new PortableValidationOpenApiExampleHintAttribute(code!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ErrorMetadataProperty_ShouldCaptureCodeKeyAndType()
    {
        var attribute = new PortableValidationOpenApiErrorMetadataPropertyAttribute(
            "InRange",
            "lowerBoundary",
            typeof(int)
        );

        (attribute.Code, attribute.Key, attribute.Type)
           .Should()
           .Be(("InRange", "lowerBoundary", typeof(int)));
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("", "key")]
    [InlineData("   ", "key")]
    [InlineData("code", null)]
    [InlineData("code", "")]
    [InlineData("code", "   ")]
    public void ErrorMetadataProperty_ShouldRejectMissingCodeOrKey(string? code, string? key)
    {
        var act = () => new PortableValidationOpenApiErrorMetadataPropertyAttribute(code!, key!, typeof(int));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ErrorMetadataProperty_ShouldRejectNullType()
    {
        var act = () => new PortableValidationOpenApiErrorMetadataPropertyAttribute("InRange", "lowerBoundary", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureStringValue()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("Pattern", "pattern", "[a-z]+");

        (attribute.Code, attribute.Key, attribute.StringValue)
           .Should()
           .Be(("Pattern", "pattern", "[a-z]+"));
    }

    [Fact]
    public void ExampleMetadata_ShouldAllowNullStringValue()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("Pattern", "pattern", (string?) null);

        attribute.StringValue.Should().BeNull();
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureIntValueAsInt64()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("InRange", "lowerBoundary", 1);

        attribute.Int64Value.Should().Be(1L);
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureLongValue()
    {
        var attribute =
            new PortableValidationOpenApiExampleMetadataAttribute("InRange", "upperBoundary", 9_000_000_000L);

        attribute.Int64Value.Should().Be(9_000_000_000L);
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureBooleanValue()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("EnumName", "ignoreCase", true);

        attribute.BooleanValue.Should().BeTrue();
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureDoubleValue()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("InRange", "lowerBoundary", 1.5);

        attribute.DoubleValue.Should().Be(1.5);
    }

    [Fact]
    public void ExampleMetadata_ShouldCaptureTypeValue()
    {
        var attribute = new PortableValidationOpenApiExampleMetadataAttribute("Enum", "enumType", typeof(DayOfWeek));

        attribute.TypeValue.Should().Be(typeof(DayOfWeek));
    }

    [Fact]
    public void ExampleMetadata_ShouldRejectNullTypeValue()
    {
        var act = () => new PortableValidationOpenApiExampleMetadataAttribute("Enum", "enumType", (Type) null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, "key")]
    [InlineData("", "key")]
    [InlineData("   ", "key")]
    [InlineData("code", null)]
    [InlineData("code", "")]
    [InlineData("code", "   ")]
    public void ExampleMetadata_ShouldRejectMissingCodeOrKey(string? code, string? key)
    {
        var act = () => new PortableValidationOpenApiExampleMetadataAttribute(code!, key!, 1);

        act.Should().Throw<ArgumentException>();
    }

    // ReSharper disable once ClassNeverInstantiated.Local -- only used as a metadata type argument
    private sealed class MetadataSample;
}
