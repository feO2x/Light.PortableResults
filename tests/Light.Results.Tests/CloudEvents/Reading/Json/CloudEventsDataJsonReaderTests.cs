using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.Results.CloudEvents.Reading.Json;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading.Json;

public sealed class CloudEventsDataJsonReaderTests
{
    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenTokenTypeIsNotStartObject()
    {
        Action act = () =>
        {
            var reader = CreateReader("123");
            CloudEventsDataJsonReader.ReadFailurePayload(ref reader);
        };

        act.Should().Throw<JsonException>().WithMessage("*must be a JSON object*");
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorsAreMissing()
    {
        Action act = () =>
        {
            var reader = CreateReader("{" + "\"metadata\":null" + "}");
            CloudEventsDataJsonReader.ReadFailurePayload(ref reader);
        };

        act.Should().Throw<JsonException>().WithMessage("*errors array*");
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorsArrayIsEmpty()
    {
        Action act = () =>
        {
            var reader = CreateReader("{" + "\"errors\":[]" + "}");
            CloudEventsDataJsonReader.ReadFailurePayload(ref reader);
        };

        act.Should().Throw<JsonException>().WithMessage("*errors array*");
    }

    [Fact]
    public void ReadFailurePayload_ShouldReadErrors_AndSkipUnknownProperties()
    {
        var reader = CreateReader(
            """
            {
                "errors": [
                    {
                        "message": "failed",
                        "code": "FAIL",
                        "target": "name",
                        "category": "Validation"
                    }
                ],
                "metadata": null,
                "unknown": {
                    "nested": true
                }
            }
            """
        );

        var payload = CloudEventsDataJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Message.Should().Be("failed");
        payload.Metadata.Should().BeNull();
    }

    private static Utf8JsonReader CreateReader(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return reader;
    }
}
