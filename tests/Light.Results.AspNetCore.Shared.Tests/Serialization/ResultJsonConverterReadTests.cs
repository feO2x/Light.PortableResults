using System.Text.Json;
using FluentAssertions;
using Light.Results.Metadata;
using Light.Results.Serialization;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests.Serialization;

public sealed class ResultJsonConverterReadTests
{
    [Fact]
    public void ReadResult_ShouldDeserializeWrappedSuccess()
    {
        var options = CreateOptions();
        const string json = "{\"value\":\"ok\",\"metadata\":{\"note\":\"hi\"}}";

        var result = JsonSerializer.Deserialize<Result<string>>(json, options);

        var expectedMetadata = MetadataObject.Create(("note", MetadataValue.FromString("hi")));
        var expectedResult = Result<string>.Ok("ok", expectedMetadata);
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ReadResult_ShouldDeserializeProblemDetails()
    {
        var options = CreateOptions();
        const string json =
            """
            {
                "type": "https://example.org/problems/not-found",
                "title": "Not Found",
                "status": 404,
                "errors": [
                    {
                        "message": "Missing",
                        "category": "NotFound"
                    }
                ]
            }
            """;

        var result = JsonSerializer.Deserialize<Result<string>>(json, options);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(1);
        result.FirstError.Message.Should().Be("Missing");
        result.FirstError.Category.Should().Be(ErrorCategory.NotFound);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var lightResultOptions = new LightResultOptions();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MetadataObjectJsonConverter());
        options.Converters.Add(new MetadataValueJsonConverter());
        options.Converters.Add(new DefaultResultJsonConverter(lightResultOptions));
        options.Converters.Add(new DefaultResultJsonConverterFactory(lightResultOptions));
        return options;
    }
}
