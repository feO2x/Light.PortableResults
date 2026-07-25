using System;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

// Every site that dispatches on MetadataKind has a fallback arm, thus adding a kind and forgetting a site breaks
// nothing at compile time. These tests pin what the fallback arms do so that the blast radius of a half-finished
// kind is at least documented.
public sealed class UndeclaredMetadataKindFallbackTests
{
    [Fact]
    public void Equals_ShouldReturnFalse_ForUndeclaredKind()
    {
        var first = MetadataValueTestFactory.CreateWithUndeclaredKind();
        var second = MetadataValueTestFactory.CreateWithUndeclaredKind();

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldOnlyHashTheKind_ForUndeclaredKind()
    {
        var first = MetadataValueTestFactory.CreateWithUndeclaredKind();
        var second = MetadataValueTestFactory.CreateWithUndeclaredKind((MetadataKind) (byte.MaxValue - 1));

        first.GetHashCode().Should().NotBe(second.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldThrow_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        var act = () => value.ToString();

        act.Should().Throw<InvalidOperationException>().WithMessage("*is unknown*");
    }

    [Fact]
    public void WriteMetadataValue_ShouldWriteNull_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
            writer.Flush();
        }

        Encoding.UTF8.GetString(stream.ToArray()).Should().Be("null");
    }
}
