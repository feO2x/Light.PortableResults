using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Writing;
using Xunit;

namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// Verifies that the public CloudEvents write overloads taking a <see cref="Utf8JsonWriter" /> leave the complete
/// event in the output before the caller flushes or disposes the writer. The envelope contract can come either from
/// the configured resolver or from a library-owned converter, and the caller must not have to know which.
/// </summary>
public sealed class CloudEventsWriteFlushTests
{
    [Fact]
    public void NonGenericWriteShouldFlushWithASourceGeneratedResolver()
    {
        var options = SourceGeneratedOptions.CreateCloudEventsWriteOptions(NoContractsJsonTypeInfoResolver.Instance);

        var written = WriteWithoutFlushing(
            writer => Result.Ok().WriteCloudEvent(writer, successType: "note.created", id: "evt-1", options: options)
        );

        GetId(written).Should().Be("evt-1");
    }

    [Fact]
    public void GenericWriteShouldFlushWithASourceGeneratedResolver()
    {
        var options = SourceGeneratedOptions.CreateCloudEventsWriteOptions();
        var result = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982));

        var written = WriteWithoutFlushing(
            writer => result.WriteCloudEvent(writer, successType: "movie.created", id: "evt-2", options: options)
        );

        GetId(written).Should().Be("evt-2");
    }

    [Fact]
    public void NonGenericWriteShouldFlushWithAReflectionBackedResolver()
    {
        var options = CreateReflectionBackedOptions();

        var written = WriteWithoutFlushing(
            writer => Result.Ok().WriteCloudEvent(writer, successType: "note.created", id: "evt-3", options: options)
        );

        GetId(written).Should().Be("evt-3");
    }

    [Fact]
    public void GenericWriteShouldFlushWithAReflectionBackedResolver()
    {
        var options = CreateReflectionBackedOptions();
        var result = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982));

        var written = WriteWithoutFlushing(
            writer => result.WriteCloudEvent(writer, successType: "movie.created", id: "evt-4", options: options)
        );

        GetId(written).Should().Be("evt-4");
    }

    private static PortableResultsCloudEventsWriteOptions CreateReflectionBackedOptions() =>
        new ()
        {
            SerializerOptions = PortableResultsCloudEventsWritingModule.CreateDefaultSerializerOptions(),
            Source = "urn:test:source"
        };

    // The writer is deliberately neither flushed nor disposed: everything the caller can observe at this point must
    // already have reached the stream.
    private static byte[] WriteWithoutFlushing(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        write(writer);
        return stream.ToArray();
    }

    private static string? GetId(byte[] cloudEvent)
    {
        using var document = JsonDocument.Parse(cloudEvent);
        return document.RootElement.GetProperty("id").GetString();
    }
}
