using FluentAssertions;
using Light.PortableResults.CloudEvents;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents;

public sealed class CloudEventsAttributeTextTests
{
    [Theory]
    [InlineData("a\u0000b", 1)]
    [InlineData("a\u001Fb", 1)]
    [InlineData("a\u007Fb", 1)]
    [InlineData("a\u009Fb", 1)]
    [InlineData("a\uFDD0b", 1)]
    public void IndexOfDisallowedCharacterShouldReturnFirstInvalidUtf16Index(string text, int expectedIndex)
    {
        CloudEventsAttributeText.IndexOfDisallowedCharacter(text).Should().Be(expectedIndex);
    }

    [Fact]
    public void IndexOfDisallowedCharacterShouldRejectLoneHighAndLowSurrogates()
    {
        var loneHighSurrogate = new string(['a', '\uD800', 'b']);
        var loneLowSurrogate = new string(['a', '\uDC00', 'b']);

        CloudEventsAttributeText.IndexOfDisallowedCharacter(loneHighSurrogate).Should().Be(1);
        CloudEventsAttributeText.IndexOfDisallowedCharacter(loneLowSurrogate).Should().Be(1);
    }

    [Fact]
    public void IndexOfDisallowedCharacterShouldRejectLastNoncharactersInEveryPlane()
    {
        var basicPlaneNoncharacter = new string(['a', '\uFFFE', 'b']);
        var supplementaryPlaneNoncharacter = new string(['a', '\uD83F', '\uDFFE', 'b']);

        CloudEventsAttributeText.IndexOfDisallowedCharacter(basicPlaneNoncharacter).Should().Be(1);
        CloudEventsAttributeText.IndexOfDisallowedCharacter(supplementaryPlaneNoncharacter).Should().Be(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain ASCII")]
    [InlineData("Grüße 日本語 😀")]
    [InlineData("\uD83D\uDE00")]
    public void IndexOfDisallowedCharacterShouldAcceptConformingText(string text)
    {
        CloudEventsAttributeText.IndexOfDisallowedCharacter(text).Should().Be(-1);
    }
}
