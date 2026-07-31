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

        Action act = () => _ = first.Equals(second);

        AssertUndeclaredKindThrows(act);
    }

    [Fact]
    public void GetHashCode_ShouldThrow_ForUndeclaredKind()
    {
        var first = MetadataValueTestFactory.CreateWithUndeclaredKind();
        Action act = () => _ = first.GetHashCode();

        AssertUndeclaredKindThrows(act);
    }

    [Fact]
    public void ToString_ShouldThrow_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        Action act = () => _ = value.ToString();

        AssertUndeclaredKindThrows(act);
    }

    [Fact]
    public void WriteMetadataValue_ShouldThrow_ForUndeclaredKind()
    {
        var value = MetadataValueTestFactory.CreateWithUndeclaredKind();

        Action act = () =>
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            writer.WriteMetadataValue(value, MetadataValueAnnotation.SerializeInHttpResponseBody);
            writer.Flush();
        };

        AssertUndeclaredKindThrows(act);
    }

    private static void AssertUndeclaredKindThrows(Action action)
    {
#if TESTING_NETSTANDARD_ASSET
        // The netstandard2.0 compiler helper cannot reference SwitchExpressionException and emits its
        // InvalidOperationException fallback. The net10.0 asset retains the more specific exception.
        action.Should().Throw<InvalidOperationException>();
#else
        action.Should().Throw<SwitchExpressionException>();
#endif
    }
}
