using FluentAssertions;
using Light.Results.Metadata;

namespace Light.Results.Tests;

public sealed class ErrorMetadataTests
{
    [Fact]
    public void Error_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var error = new Error("Something went wrong");

        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Error_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("correlationId", "abc-123"));
        var error = new Error("Something went wrong", Metadata: metadata);

        error.Metadata.Should().NotBeNull();
        error.Metadata!.Value.TryGetString("correlationId", out var id).Should().BeTrue();
        id.Should().Be("abc-123");
    }

    [Fact]
    public void WithMetadata_Properties_ShouldAddMetadata()
    {
        var error = new Error("Error occurred")
           .WithMetadata(("key1", "value1"), ("key2", 42));

        error.Metadata.Should().NotBeNull();
        error.Metadata!.Value.Should().HaveCount(2);
        error.Metadata.Value.TryGetString("key1", out var v1).Should().BeTrue();
        v1.Should().Be("value1");
    }

    [Fact]
    public void WithMetadata_Object_ShouldReplaceMetadata()
    {
        var original = new Error("Error", Metadata: MetadataObject.Create(("old", 1)));
        var newMetadata = MetadataObject.Create(("new", 2));

        var result = original.WithMetadata(newMetadata);

        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.Should().ContainSingle();
        result.Metadata.Value.TryGetInt64("new", out var value).Should().BeTrue();
        value.Should().Be(2L);
    }

    [Fact]
    public void WithMetadata_ShouldAccumulateOnExisting()
    {
        var error = new Error("Error")
           .WithMetadata(("a", 1))
           .WithMetadata(("b", 2));

        error.Metadata.Should().NotBeNull();
        error.Metadata!.Value.Should().HaveCount(2);
    }

    [Fact]
    public void Error_WithAllProperties_ShouldStoreCorrectly()
    {
        var metadata = MetadataObject.Create(("detail", "extra info"));
        var error = new Error(
            Message: "Validation failed",
            Code: "VALIDATION_ERROR",
            Target: "email",
            Metadata: metadata
        );

        error.Message.Should().Be("Validation failed");
        error.Code.Should().Be("VALIDATION_ERROR");
        error.Target.Should().Be("email");
        error.Metadata.Should().NotBeNull();
    }
}
