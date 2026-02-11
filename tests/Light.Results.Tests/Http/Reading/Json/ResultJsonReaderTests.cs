using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Json;

public sealed class ResultJsonReaderTests
{
    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenPayloadIsNotObject()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadSuccessPayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadSuccessPayload_ShouldReadMetadataOnlyPayload()
    {
        var reader = CreateReader("""{"metadata":{"traceId":"abc"}}""");

        var payload = ResultJsonReader.ReadSuccessPayload(ref reader);

        payload.Metadata.Should().Be(MetadataObject.Create(("traceId", MetadataValue.FromString("abc"))));
    }

    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenUnexpectedPropertyIsPresent()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"metadata":{"traceId":"abc"},"extra":1}""");
                ResultJsonReader.ReadSuccessPayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadBareSuccessPayload_ShouldThrow_WhenSerializerOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadBareSuccessPayload<int>(ref reader, null!);
            }
        );
    }

    [Fact]
    public void ReadBareSuccessPayload_ShouldReadBareValue()
    {
        var reader = CreateReader("42");

        var payload = ResultJsonReader.ReadBareSuccessPayload<int>(ref reader, new JsonSerializerOptions());

        payload.Value.Should().Be(42);
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenPayloadIsNotObject()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenUnexpectedPropertyIsPresent()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"count":42}""");
                ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldReadWrappedPayload_WithMetadata()
    {
        var reader = CreateReader("""{"value":42,"metadata":{"source":"wrapped"}}""");

        var payload = ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());

        payload.Value.Should().Be(42);
        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("wrapped"))));
    }

    [Fact]
    public void ReadAutoSuccessPayload_ShouldReadBareValue_WhenPayloadIsScalar()
    {
        var reader = CreateReader("42");

        var payload = ResultJsonReader.ReadAutoSuccessPayload<int>(ref reader, new JsonSerializerOptions());

        payload.Value.Should().Be(42);
        payload.Metadata.Should().BeNull();
    }

    [Fact]
    public void ReadAutoSuccessPayload_ShouldReadWrappedPayload_WhenOnlyAllowedPropertiesExist()
    {
        var reader = CreateReader("""{"value":42,"metadata":{"source":"auto"}}""");

        var payload = ResultJsonReader.ReadAutoSuccessPayload<int>(ref reader, new JsonSerializerOptions());

        payload.Value.Should().Be(42);
        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("auto"))));
    }

    [Fact]
    public void ReadAutoSuccessPayload_ShouldThrow_WhenWrapperCandidateIsMissingValue()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"metadata":{"trace":"t-1"}}""");
                ResultJsonReader.ReadAutoSuccessPayload<string>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldCreateFallbackError_WhenErrorsAreMissing()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "detail": "Missing required field",
              "metadata": { "trace": "abc" }
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Message.Should().Be("Missing required field");
        payload.Errors[0].Category.Should().Be(ErrorCategory.Validation);
        payload.Metadata.Should().Be(MetadataObject.Create(("trace", MetadataValue.FromString("abc"))));
    }

    [Fact]
    public void ReadFailurePayload_ShouldApplyAspNetErrorDetails()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "errors": {
                "name": [
                  "Name required",
                  "Name too short"
                ]
              },
              "errorDetails": [
                {
                  "target": "name",
                  "index": 1,
                  "code": "MinLength",
                  "category": "Validation",
                  "metadata": { "source": "detail" }
                }
              ]
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        var expectedErrorMetadata = MetadataObject.Create(("source", MetadataValue.FromString("detail")));
        payload.Errors.Count.Should().Be(2);
        payload.Errors[0].Message.Should().Be("Name required");
        payload.Errors[0].Code.Should().BeNull();
        payload.Errors[0].Category.Should().Be(ErrorCategory.Validation);
        payload.Errors[1].Message.Should().Be("Name too short");
        payload.Errors[1].Target.Should().Be("name");
        payload.Errors[1].Code.Should().Be("MinLength");
        payload.Errors[1].Category.Should().Be(ErrorCategory.Validation);
        payload.Errors[1].Metadata.Should().Be(expectedErrorMetadata);
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailIndexIsOutOfRange()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": "https://example.org/problems/validation",
                      "title": "Validation failed",
                      "status": 400,
                      "errors": {
                        "name": [
                          "Name required"
                        ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index": 5
                        }
                      ]
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenRichErrorCategoryIsUnknown()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": "https://example.org/problems/validation",
                      "title": "Validation failed",
                      "status": 400,
                      "errors": [
                        {
                          "message": "Name required",
                          "category": "NotARealCategory"
                        }
                      ]
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailsReferenceUnknownTarget()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": "https://example.org/problems/validation",
                      "title": "Validation failed",
                      "status": 400,
                      "errors": {
                        "name": [
                          "Name required"
                        ]
                      },
                      "errorDetails": [
                        {
                          "target": "email",
                          "index": 0
                        }
                      ]
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    private static Utf8JsonReader CreateReader(string json) => new (Encoding.UTF8.GetBytes(json));
}
