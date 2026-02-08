using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FluentAssertions;
using Light.Results.Http.Writing.Json;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests.Serialization;

public sealed class SerializerExtensionsTests
{
    [Fact]
    public void WriteGenericValue_ShouldSerializeRuntimeType_WhenGenericTypeIsObject()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        object value = new Dto { Name = "Test" };

        writer.WriteStartObject();
        writer.WritePropertyName("value");
        writer.WriteGenericValue(value, options);
        writer.WriteEndObject();
        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Be("{\"value\":{\"name\":\"Test\"}}");
    }

    private sealed class Dto
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local -- required for serialization
        public string Name { get; init; } = string.Empty;
    }
}
