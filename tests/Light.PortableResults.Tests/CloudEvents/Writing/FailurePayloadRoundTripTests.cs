using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Writing;

/// <summary>
/// Pins the round-trip property that this library never writes a failure payload its own reader rejects.
/// </summary>
public sealed class FailurePayloadRoundTripTests
{
    private static readonly PortableResultsCloudEventsWriteOptions WriteOptions = new ()
    {
        Source = "urn:test:source",
        SuccessType = "app.success",
        FailureType = "app.failure"
    };

    public static TheoryData<Result> NonGenericFailures =>
        new ()
        {
            Result.Fail(new Error { Message = "Something went wrong" }),
            Result.Fail(
                new[]
                {
                    new Error { Message = "First", Category = ErrorCategory.Conflict },
                    new Error { Message = "Second", Category = ErrorCategory.Conflict }
                }
            ),
            Result.Fail(
                new Error { Message = "With metadata", Category = ErrorCategory.NotFound },
                MetadataObject.Create(
                    (
                        "traceId",
                        MetadataValue.FromString("trace-42", MetadataValueAnnotation.SerializeInCloudEventsData)
                    )
                )
            )
        };

    [Theory]
    [MemberData(nameof(NonGenericFailures))]
    public void EveryFailurePayloadTheLibraryWritesCanBeReadBackByItsOwnReader(Result failure)
    {
        var cloudEvent = failure.ToCloudEvent(options: WriteOptions);

        var roundTripped = ((ReadOnlyMemory<byte>) cloudEvent).ReadResult();

        roundTripped.IsValid.Should().BeFalse();
        roundTripped.Errors.Count.Should().Be(failure.Errors.Count);
        roundTripped.FirstError.Message.Should().Be(failure.FirstError.Message);
    }

    [Fact]
    public void GenericFailurePayloadCanBeReadBackByItsOwnReader()
    {
        var failure = Result<int>.Fail(new Error { Message = "Not found", Category = ErrorCategory.NotFound });

        var cloudEvent = failure.ToCloudEvent(options: WriteOptions);
        var roundTripped = ((ReadOnlyMemory<byte>) cloudEvent).ReadResult<int>();

        roundTripped.IsValid.Should().BeFalse();
        roundTripped.FirstError.Message.Should().Be("Not found");
    }

    [Fact]
    public void TheOnlyResultThatWouldProduceAnUnreadablePayloadIsRejectedBeforeAnyByteIsWritten()
    {
        // The default result is invalid while carrying no errors. Writing it would emit an empty errors array,
        // which the reader used above rejects, so the write boundary must refuse it instead.
        Action act = () => default(Result<string>).ToCloudEvent(options: WriteOptions);

        act.Should().Throw<ArgumentException>().WithParameterName("result");
    }

    [Fact]
    public void AnEmptyErrorsArrayIsIndeedRejectedByTheReader()
    {
        var cloudEventWithEmptyErrors = Encoding.UTF8.GetBytes(
            """
            {
              "specversion": "1.0",
              "type": "app.failure",
              "source": "urn:test:source",
              "id": "evt-1",
              "lproutcome": "failure",
              "datacontenttype": "application/json",
              "data": { "errors": [] }
            }
            """
        );

        Action act = () => ((ReadOnlyMemory<byte>) cloudEventWithEmptyErrors).ReadResult<string>();

        act.Should().Throw<JsonException>();
    }
}
