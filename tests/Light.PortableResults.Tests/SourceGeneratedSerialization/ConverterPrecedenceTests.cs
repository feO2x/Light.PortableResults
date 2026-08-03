using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Reading.Json;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Json;
using Xunit;

namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// Verifies that converters registered before the Light.PortableResults defaults keep their precedence, both with a
/// reflection-backed resolver and with a source-generated resolver that cannot supply the wrapper contract.
/// </summary>
public sealed class ConverterPrecedenceTests
{
    private const string SuccessCloudEvent =
        """
        {
            "specversion": "1.0",
            "type": "movie.created",
            "source": "urn:test:source",
            "id": "evt-1",
            "lproutcome": "success",
            "datacontenttype": "application/json",
            "data": { "value": { "title": "Blade Runner", "year": 1982 } }
        }
        """;

    private const string SuccessHttpBody = """{"value":{"title":"Blade Runner","year":1982}}""";
    private static readonly MovieDto ReplacementMovie = new ("Replaced by converter", 2026);

    [Fact]
    public void CloudEventsReadShouldUseTheConverterRegisteredBeforeTheDefaults()
    {
        var options = SourceGeneratedOptions.CreateCloudEventsReadOptions(
            configureBeforeDefaults: serializerOptions =>
                serializerOptions.Converters.Add(new ReplacingCloudEventsAutoSuccessPayloadConverter())
        );
        var cloudEvent = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(SuccessCloudEvent));

        var result = cloudEvent.ReadResult<MovieDto>(options);

        result.Should().Be(Result<MovieDto>.Ok(ReplacementMovie));
    }

    [Fact]
    public void CloudEventsReadShouldUseTheConverterRegisteredBeforeTheDefaultsWithReflection()
    {
        var serializerOptions = PortableResultsCloudEventsReadingModule.CreateDefaultSerializerOptions();
        serializerOptions.Converters.Insert(0, new ReplacingCloudEventsAutoSuccessPayloadConverter());
        var options = new PortableResultsCloudEventsReadOptions { SerializerOptions = serializerOptions };
        var cloudEvent = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(SuccessCloudEvent));

        var result = cloudEvent.ReadResult<MovieDto>(options);

        result.Should().Be(Result<MovieDto>.Ok(ReplacementMovie));
    }

    [Fact]
    public async Task HttpReadShouldUseTheConverterRegisteredBeforeTheDefaults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = SourceGeneratedOptions.CreateHttpReadOptions(
            configureBeforeDefaults: serializerOptions =>
                serializerOptions.Converters.Add(new ReplacingHttpReadAutoSuccessResultPayloadConverter())
        );
        using var response = SourceGeneratedOptions.CreateResponse(Encoding.UTF8.GetBytes(SuccessHttpBody));

        var result = await response.ReadResultAsync<MovieDto>(options, cancellationToken);

        result.Should().Be(Result<MovieDto>.Ok(ReplacementMovie));
    }

    [Fact]
    public void CloudEventsWriteShouldUseTheConverterRegisteredBeforeTheDefaults()
    {
        var serializerOptions = SourceGeneratedOptions.CreateSerializerOptions();
        serializerOptions.Converters.Add(new SentinelCloudEventsEnvelopeForWritingConverter());
        serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();
        var options = new PortableResultsCloudEventsWriteOptions
        {
            SerializerOptions = serializerOptions,
            Source = "urn:test:source"
        };

        var cloudEvent = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982))
           .ToCloudEvent(successType: "movie.created", id: "evt-1", options: options);

        Encoding.UTF8.GetString(cloudEvent).Should().Be(SentinelCloudEventsEnvelopeForWritingConverter.WrittenJson);
    }

    [Fact]
    public void CloudEventsWriteShouldUseTheConverterRegisteredBeforeTheDefaultsWithReflection()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.Converters.Add(new SentinelCloudEventsEnvelopeForWritingConverter());
        serializerOptions.AddDefaultPortableResultsCloudEventsWriteJsonConverters();
        var options = new PortableResultsCloudEventsWriteOptions
        {
            SerializerOptions = serializerOptions,
            Source = "urn:test:source"
        };

        var cloudEvent = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982))
           .ToCloudEvent(successType: "movie.created", id: "evt-1", options: options);

        Encoding.UTF8.GetString(cloudEvent).Should().Be(SentinelCloudEventsEnvelopeForWritingConverter.WrittenJson);
    }

    private sealed class ReplacingCloudEventsAutoSuccessPayloadConverter
        : JsonConverter<CloudEventsAutoSuccessPayload<MovieDto>>
    {
        public override CloudEventsAutoSuccessPayload<MovieDto> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            reader.Skip();
            return new CloudEventsAutoSuccessPayload<MovieDto>(ReplacementMovie, metadata: null);
        }

        public override void Write(
            Utf8JsonWriter writer,
            CloudEventsAutoSuccessPayload<MovieDto> value,
            JsonSerializerOptions options
        ) =>
            throw new NotSupportedException();
    }

    private sealed class ReplacingHttpReadAutoSuccessResultPayloadConverter
        : JsonConverter<HttpReadAutoSuccessResultPayload<MovieDto>>
    {
        public override HttpReadAutoSuccessResultPayload<MovieDto> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            reader.Skip();
            return new HttpReadAutoSuccessResultPayload<MovieDto>(ReplacementMovie, metadata: null);
        }

        public override void Write(
            Utf8JsonWriter writer,
            HttpReadAutoSuccessResultPayload<MovieDto> value,
            JsonSerializerOptions options
        ) =>
            throw new NotSupportedException();
    }

    private sealed class SentinelCloudEventsEnvelopeForWritingConverter
        : JsonConverter<CloudEventsEnvelopeForWriting<MovieDto>>
    {
        public const string WrittenJson = """{"replacedBy":"custom-converter"}""";

        public override CloudEventsEnvelopeForWriting<MovieDto> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) =>
            throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter writer,
            CloudEventsEnvelopeForWriting<MovieDto> value,
            JsonSerializerOptions options
        )
        {
            writer.WriteStartObject();
            writer.WriteString("replacedBy", "custom-converter");
            writer.WriteEndObject();
        }
    }
}
