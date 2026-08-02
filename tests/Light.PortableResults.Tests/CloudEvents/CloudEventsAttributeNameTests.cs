using System;
using FluentAssertions;
using Light.PortableResults.CloudEvents;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents;

public sealed class CloudEventsAttributeNameTests
{
    [Fact]
    public void IsValidExtensionAttributeNameShouldThrowWhenAttributeNameIsNull()
    {
        Action act = () => CloudEventsAttributeName.IsValidExtensionAttributeName(null!);

        act.Should().Throw<ArgumentNullException>()
           .Where(exception => exception.ParamName == "attributeName");
    }
}
