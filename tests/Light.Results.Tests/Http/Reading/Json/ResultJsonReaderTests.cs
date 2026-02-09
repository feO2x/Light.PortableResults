using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Json;

public sealed class ResultJsonReaderTests
{
    [Fact]
    public void ReadResult_ShouldThrow_WhenPayloadIsNotObject()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadResult(ref reader);
            }
        );
    }

    [Fact]
    public void ReadSuccessResult_ShouldReadMetadataOnlyPayload()
    {
        var reader = CreateReader("""{"metadata":{"traceId":"abc"}}""");

        var result = ResultJsonReader.ReadSuccessResult(ref reader);

        var expectedResult = Result.Ok(MetadataObject.Create(("traceId", MetadataValue.FromString("abc"))));
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadSuccessResult_ShouldThrow_WhenUnexpectedPropertyIsPresent()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"metadata":{"traceId":"abc"},"extra":1}""");
                ResultJsonReader.ReadSuccessResult(ref reader);
            }
        );
    }

    [Fact]
    public void ReadResultOfT_ShouldThrow_WhenSerializerOptionsAreNull()
    {
        Assert.Throws<ArgumentNullException>(
            () =>
            {
                var reader = CreateReader("42");
                ResultJsonReader.ReadResult<int>(ref reader, null!);
            }
        );
    }

    [Fact]
    public void ReadSuccessResultOfT_ShouldReadBareValue_WhenBareValuePreferenceIsUsed()
    {
        var reader = CreateReader("42");

        var result = ResultJsonReader.ReadSuccessResult<int>(
            ref reader,
            new JsonSerializerOptions(),
            PreferSuccessPayload.BareValue
        );

        var expectedResult = Result<int>.Ok(42);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadSuccessResultOfT_ShouldThrow_WhenWrappedPreferenceIsUsedForNonWrapperObject()
    {
        Assert.Throws<JsonException>(
            () =>
            {
                var reader = CreateReader("""{"count":42}""");
                ResultJsonReader.ReadSuccessResult<int>(
                    ref reader,
                    new JsonSerializerOptions(),
                    PreferSuccessPayload.WrappedValue
                );
            }
        );
    }

    [Fact]
    public void ReadSuccessResultOfT_ShouldReadWrappedPayload_WithMetadata()
    {
        var reader = CreateReader("""{"value":42,"metadata":{"source":"wrapped"}}""");

        var result = ResultJsonReader.ReadSuccessResult<int>(
            ref reader,
            new JsonSerializerOptions(),
            PreferSuccessPayload.Auto
        );

        var expectedResult = Result<int>.Ok(42, MetadataObject.Create(("source", MetadataValue.FromString("wrapped"))));
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadGenericFailureResult_ShouldCreateFallbackError_WhenErrorsAreMissing()
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

        var result = ResultJsonReader.ReadGenericFailureResult<int>(ref reader);

        var expectedError = new Error { Message = "Missing required field", Category = ErrorCategory.Validation };
        var expectedMetadata = MetadataObject.Create(("trace", MetadataValue.FromString("abc")));
        var expectedResult = Result<int>.Fail(expectedError, expectedMetadata);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadGenericFailureResult_ShouldApplyAspNetErrorDetails()
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

        var result = ResultJsonReader.ReadGenericFailureResult<int>(ref reader);

        var expectedErrorMetadata = MetadataObject.Create(("source", MetadataValue.FromString("detail")));
        var expectedError1 = new Error
        {
            Message = "Name required",
            Category = ErrorCategory.Validation,
            Target = "name"
        };
        var expectedError2 = new Error
        {
            Message = "Name too short",
            Target = "name",
            Code = "MinLength",
            Category = ErrorCategory.Validation,
            Metadata = expectedErrorMetadata
        };
        var expectedErrors = new Errors(new[] { expectedError1, expectedError2 });
        var expectedResult = Result<int>.Fail(expectedErrors);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadGenericFailureResult_ShouldThrow_WhenErrorDetailIndexIsOutOfRange()
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
                ResultJsonReader.ReadGenericFailureResult<int>(ref reader);
            }
        );
    }

    [Fact]
    public void ReadGenericFailureResult_ShouldThrow_WhenRichErrorCategoryIsUnknown()
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
                ResultJsonReader.ReadGenericFailureResult<int>(ref reader);
            }
        );
    }

    [Fact]
    public void ReadGenericFailureResult_ShouldThrow_WhenErrorDetailsReferenceUnknownTarget()
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
                ResultJsonReader.ReadGenericFailureResult<int>(ref reader);
            }
        );
    }

    [Fact]
    public void ReadResult_ShouldParseProblemDetailsPayload()
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
                  "target": "name",
                  "category": "Validation"
                }
              ],
              "metadata": {
                "source": "reader"
              }
            }
            """
        );

        var result = ResultJsonReader.ReadResult(ref reader);

        var expectedError = new Error
        {
            Message = "Name required",
            Target = "name",
            Category = ErrorCategory.Validation
        };
        var expectedMetadata = MetadataObject.Create(("source", MetadataValue.FromString("reader")));
        var expectedResult = Result.Fail(expectedError, expectedMetadata);
        result.Should().Be(expectedResult);
    }

    private static Utf8JsonReader CreateReader(string json) => new (Encoding.UTF8.GetBytes(json));
}
