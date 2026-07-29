using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.Metadata;

// Exhaustive kind switches intentionally have no fallback arm. Undeclared values therefore surface as
// SwitchExpressionException instead of being silently interpreted as another kind.
public sealed class UndeclaredMetadataKindFallbackTests
{
    [Fact]
    public void Equals_ShouldThrow_ForUndeclaredKind()
    {
        var first = MetadataValueTestFactory.CreateWithUndeclaredKind();
        var second = MetadataValueTestFactory.CreateWithUndeclaredKind();

        var act = () => first.Equals(second);

        act.Should().Throw<SwitchExpressionException>();
    }

    [Fact]
    public void GetHashCode_ShouldThrow_ForUndeclaredKind()
    {
        var first = MetadataValueTestFactory.CreateWithUndeclaredKind();
        var act = () => first.GetHashCode();

        act.Should().Throw<SwitchExpressionException>();
    }

    [Fact]
    public void ToString_ShouldThrow_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        var act = () => value.ToString();

        act.Should().Throw<SwitchExpressionException>();
    }

    [Fact]
    public void WriteMetadataValue_ShouldThrow_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        var act = () =>
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
            writer.Flush();
        };

        act.Should().Throw<SwitchExpressionException>();
    }
}
