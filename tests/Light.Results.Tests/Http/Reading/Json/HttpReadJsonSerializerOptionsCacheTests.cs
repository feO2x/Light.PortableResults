using System.Text.Json;
using FluentAssertions;
using Light.Results.Http.Reading;
using Light.Results.Http.Reading.Json;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.Tests.Http.Reading.Json;

public sealed class HttpReadJsonSerializerOptionsCacheTests
{
    [Fact]
    public void GetByPreference_ShouldReturnAutoOptions_ForAutoPreference() =>
        HttpReadJsonSerializerOptionsCache
           .GetByPreference(PreferSuccessPayload.Auto)
           .Should().BeSameAs(HttpReadJsonSerializerOptionsCache.Auto);

    [Fact]
    public void GetByPreference_ShouldReturnBareValueOptions_ForBareValuePreference() =>
        HttpReadJsonSerializerOptionsCache
           .GetByPreference(PreferSuccessPayload.BareValue)
           .Should().BeSameAs(HttpReadJsonSerializerOptionsCache.BareValue);

    [Fact]
    public void GetByPreference_ShouldReturnWrappedValueOptions_ForWrappedValuePreference() =>
        HttpReadJsonSerializerOptionsCache
           .GetByPreference(PreferSuccessPayload.WrappedValue)
           .Should().BeSameAs(HttpReadJsonSerializerOptionsCache.WrappedValue);

    [Fact]
    public void GetByPreference_ShouldFallbackToAuto_ForUnknownEnumValue()
    {
        var serializerOptions = HttpReadJsonSerializerOptionsCache.GetByPreference((PreferSuccessPayload) 123);

        serializerOptions.Should().BeSameAs(HttpReadJsonSerializerOptionsCache.Auto);
    }

    [Fact]
    public void CachedOptions_ShouldDeserializeGenericResult()
    {
        var genericResult = JsonSerializer.Deserialize<Result<int>>(
            "{\"value\":42}",
            HttpReadJsonSerializerOptionsCache.Auto
        );

        var expectedGenericResult = Result<int>.Ok(42);
        genericResult.Should().Be(expectedGenericResult);
    }

    [Fact]
    public void CachedOptions_ShouldDeserializeNonGenericResult()
    {
        var nonGenericResult = JsonSerializer.Deserialize<Result>(
            "{\"metadata\":{\"source\":\"cache\"}}",
            HttpReadJsonSerializerOptionsCache.Auto
        );

        var expectedMetadata = MetadataObject.Create(("source", MetadataValue.FromString("cache")));
        var expectedNonGenericResult = Result.Ok(expectedMetadata);
        nonGenericResult.Should().Be(expectedNonGenericResult);
    }
}
