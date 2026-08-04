using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.CloudEvents.Writing.Json;
using Light.PortableResults.SharedJsonSerialization;
using Xunit;

namespace Light.PortableResults.Tests.CloudEvents.Writing;

/// <summary>
/// <para>
/// Covers the CloudEvents write boundaries for the default result instance.
/// </para>
/// <para>
/// Only <c>Result&lt;T></c> with a reference type or a nullable value type can take that shape.
/// <c>default(Result)</c> and <c>default(Result&lt;int>)</c> are ordinary successes, because their encapsulated
/// value can never be null.
/// </para>
/// </summary>
public sealed class DefaultResultCloudEventsGuardTests
{
    private static readonly PortableResultsCloudEventsWriteOptions WriteOptions = new ()
    {
        Source = "urn:test:source",
        SuccessType = "app.success",
        FailureType = "app.failure"
    };

    [Fact]
    public void ToCloudEventShouldRejectDefaultResult()
    {
        Action act = () => default(Result<string>).ToCloudEvent(options: WriteOptions);

        AssertDefaultInstanceRejected(act);
    }

    [Fact]
    public void ToCloudEventPooledShouldRejectDefaultResult()
    {
        Action act = () => default(Result<string>).ToCloudEventPooled(options: WriteOptions);

        AssertDefaultInstanceRejected(act);
    }

    [Fact]
    public void WriteCloudEventShouldRejectDefaultResultWithoutWritingBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        Action act = () => default(Result<string>).WriteCloudEvent(writer, options: WriteOptions);

        AssertNothingWasWritten(act, writer, stream);
    }

    [Fact]
    public void ToCloudEventsEnvelopeForWritingShouldRejectDefaultResult()
    {
        Action act = () => default(Result<string>).ToCloudEventsEnvelopeForWriting(options: WriteOptions);

        AssertDefaultInstanceRejected(act);
    }

    [Fact]
    public void WriteCloudEventsShouldRejectHandConstructedEnvelopeWithoutWritingBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var envelope = new CloudEventsEnvelopeForWriting<string>(
            "app.failure",
            "urn:test:source",
            "evt-1",
            default,
            new ResolvedCloudEventsWriteOptions(MetadataSerializationMode.Always)
        );

        Action act = () => writer.WriteCloudEvents(envelope, WriteOptions.SerializerOptions);

        AssertNothingWasWritten(act, writer, stream, "envelope");
    }

    [Fact]
    public void ToCloudEventShouldAcceptDefaultNonGenericResultAsASuccess()
    {
        var json = default(Result).ToCloudEvent(options: WriteOptions);

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("lproutcome").GetString().Should().Be("success");
    }

    private static void AssertDefaultInstanceRejected(Action act, string parameterName = "result") =>
        act.Should()
           .Throw<ArgumentException>()
           .WithParameterName(parameterName)
           .WithMessage("*default instance*Result.Ok or Result.Fail*");

    private static void AssertNothingWasWritten(
        Action act,
        Utf8JsonWriter writer,
        MemoryStream stream,
        string parameterName = "result"
    )
    {
        AssertDefaultInstanceRejected(act, parameterName);

        writer.BytesPending.Should().Be(0);
        writer.BytesCommitted.Should().Be(0);
        stream.Length.Should().Be(0);
    }
}
