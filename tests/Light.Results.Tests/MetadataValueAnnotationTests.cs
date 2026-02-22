using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class MetadataValueAnnotationTests
{
    [Fact]
    public void FromBoolean_WithAnnotation_SetsAnnotation()
    {
        var value = MetadataValue.FromBoolean(true, MetadataValueAnnotation.SerializeInHttpHeader);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
    }

    [Fact]
    public void FromInt64_WithAnnotation_SetsAnnotation()
    {
        var value = MetadataValue.FromInt64(42, MetadataValueAnnotation.None);

        value.Annotation.Should().Be(MetadataValueAnnotation.None);
    }

    [Fact]
    public void FromDouble_WithAnnotation_SetsAnnotation()
    {
        var value = MetadataValue.FromDouble(3.14, MetadataValueAnnotation.SerializeInHttpHeaderAndBody);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeaderAndBody);
    }

    [Fact]
    public void FromString_WithAnnotation_SetsAnnotation()
    {
        var value = MetadataValue.FromString("test", MetadataValueAnnotation.SerializeInHttpHeader);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
    }

    [Fact]
    public void FromDecimal_WithAnnotation_SetsAnnotation()
    {
        var value = MetadataValue.FromDecimal(123.45m);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInBodies);
    }

    [Fact]
    public void FromArray_WithPrimitives_AllowsHeaderAnnotation()
    {
        var array = MetadataArray.Create(1, 2, 3);
        var value = MetadataValue.FromArray(array, MetadataValueAnnotation.SerializeInHttpHeader);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInHttpHeader);
    }

    [Fact]
    public void FromArray_WithNestedArray_ThrowsOnHeaderAnnotation()
    {
        var innerArray = MetadataArray.Create(1, 2);
        var outerArray = MetadataArray.Create(innerArray);

        var act = () => MetadataValue.FromArray(outerArray, MetadataValueAnnotation.SerializeInHttpHeader);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as HTTP headers*");
    }

    [Fact]
    public void FromArray_WithObject_ThrowsOnHeaderAnnotation()
    {
        var obj = MetadataObject.Create(("key", "value"));
        var array = MetadataArray.Create(obj);

        var act = () => MetadataValue.FromArray(array, MetadataValueAnnotation.SerializeInHttpHeader);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as HTTP headers*");
    }

    [Fact]
    public void FromArray_WithNestedArray_ThrowsOnCloudEventsExtensionAnnotation()
    {
        var innerArray = MetadataArray.Create(1, 2);
        var outerArray = MetadataArray.Create(innerArray);

        var act = () =>
            MetadataValue.FromArray(
                outerArray,
                MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
            );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as CloudEvents extension attributes*");
    }

    [Fact]
    public void FromArray_WithPrimitiveValues_ThrowsOnCloudEventsExtensionAnnotation()
    {
        var array = MetadataArray.Create(1, 2, 3);

        var act = () =>
            MetadataValue.FromArray(
                array,
                MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
            );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as CloudEvents extension attributes*");
    }

    [Fact]
    public void FromArray_WithNestedArray_AllowsCloudEventsDataAnnotation()
    {
        var innerArray = MetadataArray.Create(1, 2);
        var outerArray = MetadataArray.Create(innerArray);

        var value = MetadataValue.FromArray(
            outerArray,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void FromObject_ThrowsOnHeaderAnnotation()
    {
        var obj = MetadataObject.Create(("key", "value"));

        var act = () => MetadataValue.FromObject(obj, MetadataValueAnnotation.SerializeInHttpHeader);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as HTTP headers*");
    }

    [Fact]
    public void FromObject_ThrowsOnCloudEventsExtensionAnnotation()
    {
        var obj = MetadataObject.Create(("key", "value"));

        var act = () =>
            MetadataValue.FromObject(
                obj,
                MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
            );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be serialized as CloudEvents extension attributes*");
    }

    [Fact]
    public void FromObject_AllowsBodyAnnotation()
    {
        var obj = MetadataObject.Create(("key", "value"));
        var value = MetadataValue.FromObject(obj);

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInBodies);
    }

    [Fact]
    public void ImplicitConversion_SetsAnnotationToSerializeInHttpResponseBody()
    {
        MetadataValue value = "test";

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInBodies);
    }

    [Fact]
    public void DefaultAnnotation_IsSerializeInHttpResponseBody()
    {
        var value = MetadataValue.FromString("test");

        value.Annotation.Should().Be(MetadataValueAnnotation.SerializeInBodies);
    }

    [Theory]
    [InlineData(MetadataValueAnnotation.None, false, false)]
    [InlineData(MetadataValueAnnotation.SerializeInHttpResponseBody, true, false)]
    [InlineData(MetadataValueAnnotation.SerializeInHttpHeader, false, true)]
    [InlineData(MetadataValueAnnotation.SerializeInHttpHeaderAndBody, true, true)]
    public void AnnotationFlags_WorkCorrectly(
        MetadataValueAnnotation annotation,
        bool expectBody,
        bool expectHeader
    )
    {
        var hasBody = (annotation & MetadataValueAnnotation.SerializeInHttpResponseBody) != 0;
        var hasHeader = (annotation & MetadataValueAnnotation.SerializeInHttpHeader) != 0;

        hasBody.Should().Be(expectBody);
        hasHeader.Should().Be(expectHeader);
    }
}
