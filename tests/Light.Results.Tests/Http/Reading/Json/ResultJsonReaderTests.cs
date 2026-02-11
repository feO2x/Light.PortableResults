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
    public void ReadSuccessPayload_ShouldReadNullMetadata()
    {
        var reader = CreateReader("""{"metadata":null}""");

        var payload = ResultJsonReader.ReadSuccessPayload(ref reader);

        payload.Metadata.Should().BeNull();
    }

    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenMetadataIsMissing()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("{}");
                ResultJsonReader.ReadSuccessPayload(ref reader);
            }
        );
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
    public void ReadSuccessPayload_ShouldThrow_WhenJsonEndsWhileReadingMetadata()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"metadata":""");
                ResultJsonReader.ReadSuccessPayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenJsonEndsWhileSkippingUnknownProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"extra":""");
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
    public void ReadBareSuccessPayload_ShouldThrow_WhenValueIsNull()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("null");
                ResultJsonReader.ReadBareSuccessPayload<string>(ref reader, new JsonSerializerOptions());
            }
        );
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
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenValueIsNull()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"value":null}""");
                ResultJsonReader.ReadWrappedSuccessPayload<string>(ref reader, new JsonSerializerOptions());
            }
        );
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
    public void ReadAutoSuccessPayload_ShouldTreatObjectWithAdditionalPropertiesAsBareValue()
    {
        var reader = CreateReader("""{"count":42}""");

        var payload = ResultJsonReader.ReadAutoSuccessPayload<JsonElement>(ref reader, new JsonSerializerOptions());

        payload.Metadata.Should().BeNull();
        payload.Value.GetProperty("count").GetInt32().Should().Be(42);
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
    public void ReadAutoSuccessPayload_ShouldThrow_WhenJsonEndsDuringWrapperInspectionAfterProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"value":""");
                ResultJsonReader.ReadAutoSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadAutoSuccessPayload_ShouldThrow_WhenJsonEndsDuringWrapperInspectionBeforeProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("{");
                ResultJsonReader.ReadAutoSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenJsonEndsWhileReadingValue()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"value":""");
                ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenJsonEndsWhileReadingMetadata()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"value":1,"metadata":""");
                ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());
            }
        );
    }

    [Fact]
    public void ReadWrappedSuccessPayload_ShouldThrow_WhenJsonEndsWhileSkippingUnexpectedProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"unknown":""");
                ResultJsonReader.ReadWrappedSuccessPayload<int>(ref reader, new JsonSerializerOptions());
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
    public void ReadFailurePayload_ShouldIgnoreInstanceProperty()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "instance": null
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
    }

    [Fact]
    public void ReadFailurePayload_ShouldIgnoreUnknownProperties()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "ignored": {
                "nested": "value"
              }
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenPayloadIsNotObject()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenStatusIsMissing()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": "https://example.org/problems/validation",
                      "title": "Validation failed"
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenTypeIsNotAString()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": 5,
                      "title": "Validation failed",
                      "status": 400
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenStatusIsNotAnInteger()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader(
                    """
                    {
                      "type": "https://example.org/problems/validation",
                      "title": "Validation failed",
                      "status": "400"
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenDetailIsNotStringOrNull()
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
                      "detail": 5
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorsHaveUnsupportedShape()
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
                      "errors": 5
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailsIsNotAnArray()
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
                      "errorDetails": {}
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenRichErrorEntryIsNotAnObject()
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
                      "errors": [ 1 ]
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
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
    public void ReadFailurePayload_ShouldApplyErrorDetails_WhenTheyAppearBeforeAspNetErrors()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "errorDetails": [
                {
                  "target": "name",
                  "index": 0,
                  "code": "Required",
                  "category": "Validation"
                }
              ],
              "errors": {
                "name": [
                  "Name required"
                ]
              }
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Code.Should().Be("Required");
        payload.Errors[0].Category.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void ReadFailurePayload_ShouldReadRichErrorMetadata()
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
                  "metadata": { "source": "rich" }
                }
              ]
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("rich"))));
    }

    [Fact]
    public void ReadFailurePayload_ShouldIgnoreUnknownPropertiesInRichErrors()
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
                  "ignored": 5
                }
              ]
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Message.Should().Be("Name required");
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingRichErrorMetadata()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors":[
                        {
                          "message":"Name required",
                          "metadata":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileSkippingUnknownRichErrorProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors":[
                        {
                          "message":"Name required",
                          "ignored":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenRichErrorMessageIsMissing()
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
                          "code": "MissingMessage"
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
    public void ReadFailurePayload_ShouldThrow_WhenAspNetErrorsContainNonStringMessage()
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
                        "name": [5]
                      }
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenAspNetErrorsContainEmptyMessage()
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
                        "name": [""]
                      }
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenAspNetErrorsValueIsNotArray()
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
                        "name": "Name required"
                      }
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingAspNetErrorList()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors":{
                        "name":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
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
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailsContainNonObjectEntry()
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
                      "errorDetails": [ 1 ]
                    }
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailIndexIsNotAnInteger()
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
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index": "0"
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
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingErrorDetailIndex()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors": {
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingType()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader("""{"type":""");
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingStatus()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingDetail()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "detail":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailCategoryIsUnknown()
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
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index": 0,
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
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingErrorDetailMetadata()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors": {
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index": 0,
                          "metadata":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileSkippingErrorDetailProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors": {
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name",
                          "index": 0,
                          "unknown":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldIgnoreUnknownPropertiesInErrorDetailObjects()
    {
        var reader = CreateReader(
            """
            {
              "type": "https://example.org/problems/validation",
              "title": "Validation failed",
              "status": 400,
              "errors": {
                "name": [ "Name required" ]
              },
              "errorDetails": [
                {
                  "target": "name",
                  "index": 0,
                  "ignored": 5
                }
              ]
            }
            """
        );

        var payload = ResultJsonReader.ReadFailurePayload(ref reader);

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Target.Should().Be("name");
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailsObjectDoesNotHaveTarget()
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
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
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

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenErrorDetailsObjectDoesNotHaveIndex()
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
                        "name": [ "Name required" ]
                      },
                      "errorDetails": [
                        {
                          "target": "name"
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
    public void ReadFailurePayload_ShouldThrow_WhenInputIsEmpty()
    {
        Assert.ThrowsAny<JsonException>(
            () =>
            {
                var reader = new Utf8JsonReader([]);
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenInputIsEmpty()
    {
        Assert.ThrowsAny<JsonException>(
            () =>
            {
                var reader = new Utf8JsonReader([]);
                ResultJsonReader.ReadSuccessPayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadSuccessPayload_ShouldThrow_WhenInputIsIncompleteAndNoTokenIsAvailable()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(string.Empty);
                ResultJsonReader.ReadSuccessPayload(ref reader);
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

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingMetadata()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "metadata":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingErrors()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errors":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileReadingErrorDetails()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "errorDetails":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    [Fact]
    public void ReadFailurePayload_ShouldThrow_WhenJsonEndsWhileSkippingUnknownProperty()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreatePartialReader(
                    """
                    {
                      "type":"https://example.org/problems/validation",
                      "title":"Validation failed",
                      "status":400,
                      "unknown":
                    """
                );
                ResultJsonReader.ReadFailurePayload(ref reader);
            }
        );
    }

    private static Utf8JsonReader CreateReader(string json) => new (Encoding.UTF8.GetBytes(json));

    private static Utf8JsonReader CreatePartialReader(string json) =>
        new (Encoding.UTF8.GetBytes(json), isFinalBlock: false, state: default);
}
