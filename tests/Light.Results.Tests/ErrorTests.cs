using System;
using FluentAssertions;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests;

public sealed class ErrorTests
{
    [Fact]
    public void IsDefaultInstance_ShouldReturnTrue_WhenDefaultInstanceIsSupplied()
    {
        var error = default(Error);

        error.IsDefaultInstance.Should().BeTrue();
    }

    [Fact]
    public void IsDefaultInstance_ShouldReturnFalse_WhenRegularInstanceIsSupplied()
    {
        var error = new Error { Message = "Some error occurred" };

        error.IsDefaultInstance.Should().BeFalse();
    }

    [Fact]
    public void Error_WithoutMetadata_ShouldHaveNullMetadata()
    {
        var error = new Error
        {
            Message = "Email is malformed",
            Category = ErrorCategory.Validation,
            Code = "LR300",
            Target = "email"
        };

        error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Message_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        var act = () => new Error { Message = null! };

        act.Should().Throw<ArgumentNullException>().Where(x => x.ParamName == nameof(Error.Message));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Message_ShouldThrowArgumentException_WhenValueIsEmptyOrContainsOnlyWhiteSpace(string invalidMessage)
    {
        var act = () => new Error { Message = invalidMessage };

        act.Should().Throw<ArgumentException>().Where(x => x.ParamName == nameof(Error.Message));
    }

    [Fact]
    public void Error_WithMetadata_ShouldStoreMetadata()
    {
        var metadata = MetadataObject.Create(("correlationId", "abc-123"));
        var error = new Error { Message = "Something went wrong", Metadata = metadata };

        error.Metadata.Should().NotBeNull();
        error.Metadata!.Value.TryGetString("correlationId", out var id).Should().BeTrue();
        id.Should().Be("abc-123");
    }
}
