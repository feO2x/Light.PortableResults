using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.Results.CloudEvents.Reading;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.CloudEvents.Reading;

public sealed class ReadOnlyMemoryCloudEventsExtensionsTests
{
    [Fact]
    public void ReadResultOfT_ShouldReadSuccessfulBareDataPayload()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-1",
                "time": "2026-02-14T12:00:00Z",
                "lroutcome": "success",
                "datacontenttype": "application/json",
                "data": 42
            }
            """
        );

        var result = cloudEvent.ReadResult<int>();

        result.Should().Be(Result<int>.Ok(42));
    }

    [Fact]
    public void ReadResultOfT_ShouldReadFailurePayload()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.failure",
                "source": "urn:test:source",
                "id": "evt-2",
                "lroutcome": "failure",
                "data": {
                    "errors": [
                        {
                            "message": "failed",
                            "code": "FAIL",
                            "target": "field",
                            "category": "Validation"
                        }
                    ],
                    "metadata": {
                        "traceId": "abc"
                    }
                }
            }
            """
        );

        var result = cloudEvent.ReadResult<int>();

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Message.Should().Be("failed");
        result.Metadata.Should().Be(MetadataObject.Create(("traceId", MetadataValue.FromString("abc"))));
    }

    [Fact]
    public void ReadResult_ShouldReturnOk_WhenSuccessEventHasNoData()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-3",
                "lroutcome": "success"
            }
            """
        );

        var result = cloudEvent.ReadResult();

        result.Should().Be(Result.Ok());
    }

    [Fact]
    public void ReadResult_ShouldReadMetadata_WhenSuccessDataContainsMetadataObject()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-3a",
                "lroutcome": "success",
                "data": {
                    "metadata": {
                        "traceid": "abc"
                    }
                }
            }
            """
        );

        var result = cloudEvent.ReadResult();

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceid", out var traceId).Should().BeTrue();
        traceId.Should().Be("abc");
        result.Metadata.Value["traceid"].Annotation.Should().Be(MetadataValueAnnotation.SerializeInCloudEventsData);
    }

    [Fact]
    public void ReadResult_ShouldThrow_WhenSuccessDataIsNotMetadataWrapperObject()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-3b",
                "lroutcome": "success",
                "data": 42
            }
            """
        );

        var act = () => cloudEvent.ReadResult();

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ReadResult_ShouldThrow_WhenFailureEventHasNoData()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.failure",
                "source": "urn:test:source",
                "id": "evt-4",
                "lroutcome": "failure"
            }
            """
        );

        var act = () => cloudEvent.ReadResult();

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenDataIsMissing()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-5",
                "lroutcome": "success"
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenSpecVersionIsInvalid()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.1",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-6",
                "lroutcome": "success",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*specversion*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenDataContentTypeIsNotJsonCompatible()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-7",
                "lroutcome": "success",
                "datacontenttype": "text/plain",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*datacontenttype*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenDataBase64IsPresent()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-8",
                "lroutcome": "success",
                "data_base64": "AQ==",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*data_base64*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenLrOutcomeIsInvalid()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-9",
                "lroutcome": "maybe",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*lroutcome*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenLrOutcomeIsNotAString()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-9a",
                "lroutcome": 123,
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*lroutcome*");
    }

    [Fact]
    public void ReadResultOfT_ShouldUseIsFailureTypeFallback_WhenLrOutcomeIsMissing()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.failure",
                "source": "urn:test:source",
                "id": "evt-10",
                "data": {
                    "errors": [
                        {
                            "message": "failed"
                        }
                    ]
                }
            }
            """
        );

        var options = new LightResultsCloudEventsReadOptions
        {
            IsFailureType = type => type == "app.failure"
        };

        var result = cloudEvent.ReadResult<int>(options);

        result.IsValid.Should().BeFalse();
        result.Errors[0].Message.Should().Be("failed");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenOutcomeCannotBeClassified()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.unknown",
                "source": "urn:test:source",
                "id": "evt-11",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenSourceIsInvalid()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "http://[invalid",
                "id": "evt-12",
                "lroutcome": "success",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*source*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenRequiredStringAttributeHasWrongType()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": 123,
                "id": "evt-12a",
                "lroutcome": "success",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*source*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenOptionalStringAttributeHasWrongType()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "subject": 123,
                "id": "evt-12b",
                "lroutcome": "success",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*subject*");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenDataSchemaIsNotAbsoluteUri()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-13",
                "dataschema": "relative/path",
                "lroutcome": "success",
                "data": 1
            }
            """
        );

        var act = () => cloudEvent.ReadResult<int>();

        act.Should().Throw<JsonException>().WithMessage("*dataschema*");
    }

    [Fact]
    public void ReadResultWithCloudEventsEnvelopeOfT_ShouldExposeEnvelopeAndExtensionAttributes()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "subject": "subject-1",
                "id": "evt-14",
                "time": "2026-02-14T12:00:00Z",
                "dataschema": "https://example.org/schema",
                "lroutcome": "success",
                "traceid": "abc",
                "data": 5
            }
            """
        );

        var envelope = cloudEvent.ReadResultWithCloudEventsEnvelope<int>();

        envelope.Type.Should().Be("app.success");
        envelope.Source.Should().Be("urn:test:source");
        envelope.Subject.Should().Be("subject-1");
        envelope.Id.Should().Be("evt-14");
        envelope.DataSchema.Should().Be("https://example.org/schema");
        envelope.Data.Should().Be(Result<int>.Ok(5));
        envelope.ExtensionAttributes.Should().NotBeNull();
        envelope.ExtensionAttributes!.Value.TryGetString("traceid", out var traceId).Should().BeTrue();
        traceId.Should().Be("abc");
    }

    [Fact]
    public void ReadResultOfT_ShouldMergeParsedExtensionMetadata_WhenParsingServiceIsConfigured()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-15",
                "lroutcome": "success",
                "traceid": "from-envelope",
                "data": {
                    "value": 9,
                    "metadata": {
                        "traceid": "from-payload"
                    }
                }
            }
            """
        );

        var options = new LightResultsCloudEventsReadOptions
        {
            ParsingService = new DefaultCloudEventsAttributeParsingService(),
            PreferSuccessPayload = PreferSuccessPayload.WrappedValue,
            MergeStrategy = MetadataMergeStrategy.AddOrReplace
        };

        var result = cloudEvent.ReadResult<int>(options);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(9);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("traceid", out var traceId).Should().BeTrue();
        traceId.Should().Be("from-payload");
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenExtensionAttributeIsComplex()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-16",
                "lroutcome": "success",
                "context": {
                    "nested": "x"
                },
                "data": 9
            }
            """
        );

        var options = new LightResultsCloudEventsReadOptions
        {
            ParsingService = new DefaultCloudEventsAttributeParsingService(),
            PreferSuccessPayload = PreferSuccessPayload.BareValue,
            MergeStrategy = MetadataMergeStrategy.AddOrReplace
        };

        var act = () => cloudEvent.ReadResult<int>(options);

        act.Should().Throw<JsonException>()
           .WithMessage("*extension attributes*primitive*");
    }

    [Fact]
    public void ReadResult_ShouldIgnoreLrOutcomeWhenMergingExtensionMetadata()
    {
        var cloudEvent = CreateUtf8(
            """
            {
                "specversion": "1.0",
                "type": "app.success",
                "source": "urn:test:source",
                "id": "evt-17",
                "lroutcome": "success",
                "data": {
                    "metadata": null
                }
            }
            """
        );

        var options = new LightResultsCloudEventsReadOptions
        {
            ParsingService = new DefaultCloudEventsAttributeParsingService(),
            MergeStrategy = MetadataMergeStrategy.AddOrReplace
        };

        var result = cloudEvent.ReadResult(options);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().BeNull();
    }

    private static ReadOnlyMemory<byte> CreateUtf8(string json) => Encoding.UTF8.GetBytes(json);
}
