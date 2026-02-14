using System;
using FluentAssertions;
using Light.Results.CloudEvents;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents;

public sealed class CloudEventEnvelopeTests
{
    [Fact]
    public void CloudEventEnvelope_ShouldExposeAllConfiguredValues()
    {
        var time = new DateTimeOffset(2026, 2, 14, 15, 20, 0, TimeSpan.Zero);
        var extensionAttributes = MetadataObject.Create(("traceid", MetadataValue.FromString("abc")));
        var data = Result.Fail(new Error { Message = "failure" });

        var envelope = new CloudEventEnvelope(
            Type: "app.failure",
            Source: "urn:test:source",
            Id: "evt-1",
            Data: data,
            Subject: "contacts",
            Time: time,
            DataContentType: "application/json",
            DataSchema: "https://example.org/schema",
            ExtensionAttributes: extensionAttributes
        );

        envelope.Type.Should().Be("app.failure");
        envelope.Source.Should().Be("urn:test:source");
        envelope.Id.Should().Be("evt-1");
        envelope.Data.Should().Be(data);
        envelope.Subject.Should().Be("contacts");
        envelope.Time.Should().Be(time);
        envelope.DataContentType.Should().Be("application/json");
        envelope.DataSchema.Should().Be("https://example.org/schema");
        envelope.ExtensionAttributes.Should().Be(extensionAttributes);
        CloudEventEnvelope.SpecVersion.Should().Be(CloudEventConstants.SpecVersion);
    }

    [Fact]
    public void CloudEventEnvelopeOfT_ShouldExposeAllConfiguredValues()
    {
        var time = new DateTimeOffset(2026, 2, 14, 15, 25, 0, TimeSpan.Zero);
        var extensionAttributes = MetadataObject.Create(("attempt", MetadataValue.FromInt64(2)));
        var data = Result<int>.Ok(42);

        var envelope = new CloudEventEnvelope<int>(
            Type: "app.success",
            Source: "urn:test:source",
            Id: "evt-2",
            Data: data,
            Subject: "contacts",
            Time: time,
            DataContentType: "application/json",
            DataSchema: "https://example.org/schema",
            ExtensionAttributes: extensionAttributes
        );

        envelope.Type.Should().Be("app.success");
        envelope.Source.Should().Be("urn:test:source");
        envelope.Id.Should().Be("evt-2");
        envelope.Data.Should().Be(data);
        envelope.Subject.Should().Be("contacts");
        envelope.Time.Should().Be(time);
        envelope.DataContentType.Should().Be("application/json");
        envelope.DataSchema.Should().Be("https://example.org/schema");
        envelope.ExtensionAttributes.Should().Be(extensionAttributes);
        CloudEventEnvelope<int>.SpecVersion.Should().Be(CloudEventConstants.SpecVersion);
    }
}
