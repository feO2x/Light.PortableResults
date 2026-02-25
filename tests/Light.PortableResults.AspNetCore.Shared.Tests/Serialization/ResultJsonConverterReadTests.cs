using System;
using System.Text.Json;
using FluentAssertions;
using Light.PortableResults.Http.Writing;
using Xunit;

namespace Light.PortableResults.AspNetCore.Shared.Tests.Serialization;

public sealed class ResultJsonConverterReadTests
{
    [Fact]
    public void ReadHttpResultForWriting_ShouldThrow_ForNonGenericWriteConverter()
    {
        var options = CreateOptions();
        const string json = "{\"metadata\":{\"note\":\"hi\"}}";

        Action act = () => JsonSerializer.Deserialize<HttpResultForWriting>(json, options);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ReadHttpResultForWriting_ShouldThrow_ForGenericWriteConverter()
    {
        var options = CreateOptions();
        const string json = "{\"value\":\"ok\"}";

        Action act = () => JsonSerializer.Deserialize<HttpResultForWriting<string>>(json, options);

        act.Should().Throw<NotSupportedException>();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.AddDefaultPortableResultsHttpWriteJsonConverters();
        return options;
    }
}
