using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Json;
using Light.PortableResults.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.PortableResults.Tests.Http.Reading;

public sealed class ModuleTests
{
    [Fact]
    public void AddDefaultPortableResultsHttpReadJsonConverters_ShouldThrow_WhenSerializerOptionsAreNull()
    {
        var act = () => Module.AddDefaultPortableResultsHttpReadJsonConverters(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDefaultPortableResultsHttpReadJsonConverters_ShouldAlwaysAddConverters()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        serializerOptions.AddDefaultPortableResultsHttpReadJsonConverters();
        serializerOptions.AddDefaultPortableResultsHttpReadJsonConverters();

        serializerOptions.Converters.Should().HaveCount(10);
        serializerOptions
           .Converters.Count(converter => converter is HttpReadMetadataObjectJsonConverter).Should().Be(2);
        serializerOptions
           .Converters.Count(converter => converter is HttpReadMetadataValueJsonConverter).Should().Be(2);
        serializerOptions
           .Converters.Count(converter => converter is HttpReadFailureResultPayloadJsonConverter).Should().Be(2);
        serializerOptions
           .Converters.Count(converter => converter is HttpReadSuccessResultPayloadJsonConverter).Should().Be(2);
        serializerOptions
           .Converters.Count(converter => converter is HttpReadSuccessResultPayloadJsonConverterFactory).Should().Be(2);
    }

    [Fact]
    public void CreateDefaultPortableResultsHttpReadJsonSerializerOptions_ShouldDeserializeGenericAutoPayload()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();

        var payload =
            JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<int>>("{\"value\":42}", serializerOptions);

        payload.Value.Should().Be(42);
        payload.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateDefaultPortableResultsHttpReadJsonSerializerOptions_ShouldDeserializeNonGenericSuccessPayload()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();

        var payload = JsonSerializer.Deserialize<HttpReadSuccessResultPayload>(
            "{\"metadata\":{\"source\":\"module\"}}",
            serializerOptions
        );

        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("module"))));
    }

    [Fact]
    public void CreateDefaultPortableResultsHttpReadJsonSerializerOptions_ShouldDeserializeFailurePayload()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();

        var payload = JsonSerializer.Deserialize<HttpReadFailureResultPayload>(
            """
            {
                "type": "https://example.org/problems/validation",
                "title": "Validation failed",
                "status": 400,
                "errors": [
                    {
                        "message": "Name is required",
                        "code": "NameRequired",
                        "target": "name",
                        "category": "Validation"
                    }
                ]
            }
            """,
            serializerOptions
        );

        payload.Errors.Count.Should().Be(1);
        payload.Errors[0].Message.Should().Be("Name is required");
    }

    [Fact]
    public void AddPortableResultsHttpReadOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddPortableResultsHttpReadOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PortableResultsHttpReadOptions>();

        options.Should().NotBeNull();
        options.SerializerOptions.Should().BeSameAs(Module.DefaultSerializerOptions);
    }
}
