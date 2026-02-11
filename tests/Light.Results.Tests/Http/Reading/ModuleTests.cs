using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Light.Results.Tests.Http.Reading;

public sealed class ModuleTests
{
    [Fact]
    public void AddDefaultLightResultsHttpReadJsonConverters_ShouldThrow_WhenSerializerOptionsAreNull()
    {
        var act = () => Module.AddDefaultLightResultsHttpReadJsonConverters(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddDefaultLightResultsHttpReadJsonConverters_ShouldAlwaysAddConverters()
    {
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        serializerOptions.AddDefaultLightResultsHttpReadJsonConverters();
        serializerOptions.AddDefaultLightResultsHttpReadJsonConverters();

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
    public void CreateDefaultLightResultsHttpReadJsonSerializerOptions_ShouldDeserializeGenericAutoPayload()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();

        var payload =
            JsonSerializer.Deserialize<HttpReadAutoSuccessResultPayload<int>>("{\"value\":42}", serializerOptions);

        payload.Value.Should().Be(42);
        payload.Metadata.Should().BeNull();
    }

    [Fact]
    public void CreateDefaultLightResultsHttpReadJsonSerializerOptions_ShouldDeserializeNonGenericSuccessPayload()
    {
        var serializerOptions = Module.CreateDefaultSerializerOptions();

        var payload = JsonSerializer.Deserialize<HttpReadSuccessResultPayload>(
            "{\"metadata\":{\"source\":\"module\"}}",
            serializerOptions
        );

        payload.Metadata.Should().Be(MetadataObject.Create(("source", MetadataValue.FromString("module"))));
    }

    [Fact]
    public void CreateDefaultLightResultsHttpReadJsonSerializerOptions_ShouldDeserializeFailurePayload()
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
    public void AddLightResultsHttpReadOptions_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLightResultsHttpReadOptions();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LightResultsHttpReadOptions>();

        options.Should().NotBeNull();
        options.SerializerOptions.Should().BeSameAs(Module.DefaultSerializerOptions);
    }
}
