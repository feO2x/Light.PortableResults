using System;
using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Writing;
using Xunit;

namespace Light.Results.AspNetCore.Shared.Tests.Serialization;

public sealed class ResultJsonConverterReadTests
{
    [Fact]
    public void ReadResult_ShouldThrow_ForNonGenericWriteConverter()
    {
        var options = CreateOptions();
        const string json = "{\"metadata\":{\"note\":\"hi\"}}";

        Action act = () => JsonSerializer.Deserialize<Result>(json, options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReadResult_ShouldThrow_ForGenericWriteConverter()
    {
        var options = CreateOptions();
        const string json = "{\"value\":\"ok\"}";

        Action act = () => JsonSerializer.Deserialize<Result<string>>(json, options);

        act.Should().Throw<NotSupportedException>();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var lightResultOptions = new LightResultsHttpWriteOptions();
        var options = new JsonSerializerOptions();
        options.AddDefaultLightResultsHttpWriteJsonConverters(lightResultOptions);
        return options;
    }
}
