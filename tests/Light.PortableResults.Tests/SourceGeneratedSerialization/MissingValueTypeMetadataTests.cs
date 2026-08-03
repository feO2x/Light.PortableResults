using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Http.Writing;
using Light.PortableResults.Http.Writing.Json;
using Light.PortableResults.Metadata;
using Light.PortableResults.SharedJsonSerialization;
using Light.PortableResults.SharedJsonSerialization.Writing;
using Xunit;

namespace Light.PortableResults.Tests.SourceGeneratedSerialization;

/// <summary>
/// Verifies that every entry point which resolves a consumer-owned type reports the unresolved type and the remedy
/// instead of failing with a System.Text.Json error about missing metadata.
/// </summary>
public sealed class MissingValueTypeMetadataTests
{
    private const string CloudEventJson =
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

    private const string HttpBodyJson = """{"value":{"title":"Blade Runner","year":1982}}""";

    private static string ExpectedMessage => PortableResultsJsonContracts.CreateMissingTypeInfoMessage(typeof(MovieDto));

    [Theory]
    [InlineData(PreferSuccessPayload.Auto)]
    [InlineData(PreferSuccessPayload.BareValue)]
    [InlineData(PreferSuccessPayload.WrappedValue)]
    public void CloudEventsReadShouldNameTheUnresolvedValueType(PreferSuccessPayload preferSuccessPayload)
    {
        var options = SourceGeneratedOptions.CreateCloudEventsReadOptions(NoContractsJsonTypeInfoResolver.Instance)
           with
        {
            PreferSuccessPayload = preferSuccessPayload
        };
        var cloudEvent = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(CloudEventJson));

        var act = () => cloudEvent.ReadResult<MovieDto>(options);

        act.Should().Throw<InvalidOperationException>().WithMessage(ExpectedMessage);
    }

    [Theory]
    [InlineData(PreferSuccessPayload.Auto)]
    [InlineData(PreferSuccessPayload.BareValue)]
    [InlineData(PreferSuccessPayload.WrappedValue)]
    public async Task HttpReadShouldNameTheUnresolvedValueType(PreferSuccessPayload preferSuccessPayload)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = SourceGeneratedOptions.CreateHttpReadOptions(NoContractsJsonTypeInfoResolver.Instance) with
        {
            PreferSuccessPayload = preferSuccessPayload
        };
        using var response = SourceGeneratedOptions.CreateResponse(Encoding.UTF8.GetBytes(HttpBodyJson));

        // ReSharper disable once AccessToDisposedClosure -- act is called before disposal
        var act = async () => await response.ReadResultAsync<MovieDto>(options, cancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(ExpectedMessage);
    }

    [Fact]
    public void CloudEventsWriteShouldNameTheUnresolvedValueType()
    {
        var result = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982));
        var options = SourceGeneratedOptions.CreateCloudEventsWriteOptions(NoContractsJsonTypeInfoResolver.Instance);

        var act = () => result.ToCloudEvent(successType: "movie.created", id: "evt-1", options: options);

        act.Should().Throw<InvalidOperationException>().WithMessage(ExpectedMessage);
    }

    [Fact]
    public void HttpWriteShouldNameTheUnresolvedValueType()
    {
        var result = Result<MovieDto>.Ok(new MovieDto("Blade Runner", 1982));
        var options = SourceGeneratedOptions.CreateHttpWriteOptions(NoContractsJsonTypeInfoResolver.Instance);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var act = () => new HttpResultForWritingJsonConverter<MovieDto>().Write(
            writer,
            result.ToHttpResultForWriting(new PortableResultsHttpWriteOptions()),
            options
        );

        act.Should().Throw<InvalidOperationException>().WithMessage(ExpectedMessage);
    }

    [Fact]
    public void WriteGenericValueShouldNameTheUnresolvedValueType()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var act = () => writer.WriteGenericValue(new MovieDto("Blade Runner", 1982), options);

        act.Should().Throw<InvalidOperationException>().WithMessage(ExpectedMessage);
    }

    [Fact]
    public void WriteRichErrorsShouldNameTheUnresolvedMetadataContract()
    {
        var options = SourceGeneratedOptions.CreateSerializerOptions(NoContractsJsonTypeInfoResolver.Instance);
        var errors = new Errors(
            new Error
            {
                Message = "The movie was rejected.",
                Metadata = MetadataObject.Create(("attempt", MetadataValue.FromString("1")))
            }
        );
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();

        var act = () => writer.WriteRichErrors(errors, isValidationResponse: false, options);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(PortableResultsJsonContracts.CreateMissingLibraryContractMessage(typeof(MetadataObject)));
    }
}
