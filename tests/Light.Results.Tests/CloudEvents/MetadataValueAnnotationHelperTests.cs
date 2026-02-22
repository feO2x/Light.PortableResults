using FluentAssertions;
using Light.Results.CloudEvents;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents;

public sealed class MetadataValueAnnotationHelperTests
{
    [Fact]
    public void WithAnnotation_ShouldRewriteNullValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromNull(MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten.Kind.Should().Be(MetadataKind.Null);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteBooleanValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromBoolean(true, MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
        );

        rewritten.TryGetBoolean(out var value).Should().BeTrue();
        value.Should().BeTrue();
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteInt64Value()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromInt64(42, MetadataValueAnnotation.SerializeInHttpHeader),
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten.TryGetInt64(out var value).Should().BeTrue();
        value.Should().Be(42);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteDoubleValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromDouble(12.5, MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten.TryGetDouble(out var value).Should().BeTrue();
        value.Should().Be(12.5);
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void WithAnnotation_ShouldRewriteStringValue()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataValue.FromString("abc", MetadataValueAnnotation.SerializeInHttpResponseBody),
            MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute
        );

        rewritten.TryGetString(out var value).Should().BeTrue();
        value.Should().Be("abc");
        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeAsCloudEventExtensionAttribute);
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
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten.TryGetArray(out var rewrittenArray).Should().BeTrue();
        rewrittenArray.Count.Should().Be(3);
        rewrittenArray[0].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);

        rewrittenArray[1].TryGetObject(out var rewrittenObject).Should().BeTrue();
        rewrittenObject["inner"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);

        rewrittenArray[2].TryGetArray(out var nestedArray).Should().BeTrue();
        nestedArray[0].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);

        rewritten.Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
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
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten["flag"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
        rewritten["count"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
        rewritten["nested"].TryGetObject(out var nested).Should().BeTrue();
        nested["name"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventData);
    }

    [Fact]
    public void WithAnnotation_ForEmptyMetadataObject_ShouldReturnEmpty()
    {
        var rewritten = MetadataValueAnnotationHelper.WithAnnotation(
            MetadataObject.Empty,
            MetadataValueAnnotation.SerializeInCloudEventData
        );

        rewritten.Should().Equal(MetadataObject.Empty);
    }
}
