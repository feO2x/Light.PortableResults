using System;
using System.Reflection;
using FluentAssertions;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents;

public sealed class MetadataValueAnnotationHelperTests
{
    [Fact]
    public void WithAnnotation_ShouldRewriteNullValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromNull(MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten.Kind.Should().Be(MetadataKind.Null);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteBooleanValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromBoolean(true, MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
        );

        rewritten.TryGetBoolean(out var value).Should().BeTrue();
        value.Should().BeTrue();
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteInt64Value()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromInt64(42, MetadataValueAnnotation.SerializeInHttpHeader),
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten.TryGetInt64(out var value).Should().BeTrue();
        value.Should().Be(42);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteDoubleValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromDouble(12.5, MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten.TryGetDouble(out var value).Should().BeTrue();
        value.Should().Be(12.5);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteStringValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes
        );

        rewritten.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsExtensionAttributes);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteNestedArrayAndObjectValues()
    {
        var nestedObject = MetadataObject.Create(("inner", MetadataValue.FromString("value")));
        var array = MetadataArray.Create(
            MetadataValue.FromBoolean(true),
            MetadataValue.FromObject(nestedObject),
            MetadataValue.FromArray(MetadataArray.Create(MetadataValue.FromInt64(7)))
        );

        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromArray(array),
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten.TryGetArray(out var rewrittenArray).Should().BeTrue();
        rewrittenArray.Count.Should().Be(3);
        rewrittenArray[0].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);

        rewrittenArray[1].TryGetObject(out var rewrittenObject).Should().BeTrue();
        rewrittenObject["inner"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);

        rewrittenArray[2].TryGetArray(out var nestedArray).Should().BeTrue();
        nestedArray[0].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);

        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void WithAnnotation_ForMetadataObject_ShouldRewriteAllEntries()
    {
        var metadataObject = MetadataObject.Create(
            ("flag", MetadataValue.FromBoolean(true)),
            ("count", MetadataValue.FromInt64(5)),
            (
                "nested",
                MetadataValue.FromObject(MetadataObject.Create(("name", MetadataValue.FromString("foo"))))
            )
        );

        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            metadataObject,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten["flag"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
        rewritten["count"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
        rewritten["nested"].TryGetObject(out var nested).Should().BeTrue();
        nested["name"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void WithAnnotation_ForEmptyMetadataObject_ShouldReturnEmpty()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataObject.Empty,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        rewritten.Should().Equal(MetadataObject.Empty);
    }

    [Fact]
    public void WithAnnotation_WithUnsupportedMetadataKind_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidValue = CreateMetadataValueWithInvalidKind();

        var act = () => MetadataValueAnnotationHelper.WithAnnotation(
            invalidValue,
            MetadataValueAnnotation.SerializeInCloudEventsData
        );

        act.Should().Throw<ArgumentOutOfRangeException>()
           .Which.ParamName.Should().Be("value");
    }

    private static MetadataValue CreateMetadataValueWithInvalidKind()
    {
        var metadataValueType = typeof(MetadataValue);
        var metadataPayloadType = metadataValueType.Assembly.GetType(
            "Light.PortableResults.Metadata.MetadataPayload",
            throwOnError: true
        )!;

        var constructor = metadataValueType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(MetadataKind), metadataPayloadType, typeof(MetadataValueAnnotation)],
            modifiers: null
        )!;

        var payload = Activator.CreateInstance(metadataPayloadType)!;

        return (MetadataValue) constructor.Invoke(
            [(MetadataKind) byte.MaxValue, payload, MetadataValue.DefaultAnnotation]
        );
    }
}
